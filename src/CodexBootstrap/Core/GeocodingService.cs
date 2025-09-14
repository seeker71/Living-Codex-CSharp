using System.Text.Json;
using CodexBootstrap.Core;

namespace CodexBootstrap.Core
{
    [MetaNode(Id = "codex.core.geocoding-service", Name = "GeocodingService", Description = "Service for geocoding locations using external APIs")]
    public class GeocodingService
    {
        private readonly HttpClient _httpClient;
        private readonly ICodexLogger _logger;
        private readonly string _apiKey;

        public GeocodingService(HttpClient httpClient, ICodexLogger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = Environment.GetEnvironmentVariable("OPENCAGE_API_KEY") ?? 
                     Environment.GetEnvironmentVariable("GEOCODING_API_KEY") ?? 
                     "";
        }

        [ResponseType(Id = "codex.core.geocoding-result", Name = "GeocodingResult", Description = "Result of geocoding operation")]
        public record GeocodingResult(
            double Latitude,
            double Longitude,
            string FormattedAddress,
            string Country,
            string City,
            bool IsFromCache,
            string Source
        );

        [ResponseType(Id = "codex.core.geocoding-error", Name = "GeocodingError", Description = "Error from geocoding operation")]
        public record GeocodingError(
            string Message,
            string Location,
            string Source
        );

        public async Task<GeocodingResult?> GeocodeAsync(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                _logger.Warn("Empty location provided for geocoding");
                return null;
            }

            try
            {
                // Try OpenCage first if API key is available
                if (!string.IsNullOrEmpty(_apiKey))
                {
                    var result = await GeocodeWithOpenCageAsync(location);
                    if (result != null)
                        return result;
                }

                // Fallback to other services
                var fallbackResult = await GeocodeWithFallbackAsync(location);
                return fallbackResult;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error geocoding location '{location}': {ex.Message}");
                return null;
            }
        }

        private async Task<GeocodingResult?> GeocodeWithOpenCageAsync(string location)
        {
            try
            {
                var encodedLocation = Uri.EscapeDataString(location);
                var url = $"https://api.opencagedata.com/geocode/v1/json?q={encodedLocation}&key={_apiKey}&limit=1&no_annotations=1";
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                
                if (json.RootElement.TryGetProperty("results", out var results) && results.GetArrayLength() > 0)
                {
                    var result = results[0];
                    var geometry = result.GetProperty("geometry");
                    var components = result.GetProperty("components");
                    
                    var lat = geometry.GetProperty("lat").GetDouble();
                    var lng = geometry.GetProperty("lng").GetDouble();
                    var formatted = result.GetProperty("formatted").GetString() ?? location;
                    
                    var country = components.TryGetProperty("country", out var countryProp) ? countryProp.GetString() : "";
                    var city = components.TryGetProperty("city", out var cityProp) ? cityProp.GetString() : 
                              components.TryGetProperty("town", out var townProp) ? townProp.GetString() : 
                              components.TryGetProperty("village", out var villageProp) ? villageProp.GetString() : "";

                    _logger.Info($"Successfully geocoded '{location}' to {lat}, {lng} using OpenCage");
                    return new GeocodingResult(lat, lng, formatted, country ?? "", city ?? "", false, "OpenCage");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"OpenCage geocoding failed for '{location}': {ex.Message}");
            }

            return null;
        }

        private async Task<GeocodingResult?> GeocodeWithFallbackAsync(string location)
        {
            // Try with a free alternative - Nominatim (OpenStreetMap)
            try
            {
                var encodedLocation = Uri.EscapeDataString(location);
                var url = $"https://nominatim.openstreetmap.org/search?q={encodedLocation}&format=json&limit=1&addressdetails=1";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Living-Codex/1.0");
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                
                if (json.RootElement.GetArrayLength() > 0)
                {
                    var result = json.RootElement[0];
                    var lat = result.GetProperty("lat").GetDouble();
                    var lng = result.GetProperty("lon").GetDouble();
                    var displayName = result.GetProperty("display_name").GetString() ?? location;
                    
                    var address = result.TryGetProperty("address", out var addressProp) ? addressProp : new JsonElement();
                    var country = address.TryGetProperty("country", out var countryProp) ? countryProp.GetString() : "";
                    var city = address.TryGetProperty("city", out var cityProp) ? cityProp.GetString() : 
                              address.TryGetProperty("town", out var townProp) ? townProp.GetString() : 
                              address.TryGetProperty("village", out var villageProp) ? villageProp.GetString() : "";

                    _logger.Info($"Successfully geocoded '{location}' to {lat}, {lng} using Nominatim");
                    return new GeocodingResult(lat, lng, displayName, country ?? "", city ?? "", false, "Nominatim");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Nominatim geocoding failed for '{location}': {ex.Message}");
            }

            // Final fallback to hardcoded major cities
            return GeocodeWithHardcodedCities(location);
        }

        private GeocodingResult? GeocodeWithHardcodedCities(string location)
        {
            var majorCities = new Dictionary<string, (double lat, double lng, string country, string city)>(StringComparer.OrdinalIgnoreCase)
            {
                ["new york"] = (40.7128, -74.0060, "United States", "New York"),
                ["london"] = (51.5074, -0.1278, "United Kingdom", "London"),
                ["paris"] = (48.8566, 2.3522, "France", "Paris"),
                ["tokyo"] = (35.6762, 139.6503, "Japan", "Tokyo"),
                ["san francisco"] = (37.7749, -122.4194, "United States", "San Francisco"),
                ["los angeles"] = (34.0522, -118.2437, "United States", "Los Angeles"),
                ["chicago"] = (41.8781, -87.6298, "United States", "Chicago"),
                ["houston"] = (29.7604, -95.3698, "United States", "Houston"),
                ["phoenix"] = (33.4484, -112.0740, "United States", "Phoenix"),
                ["philadelphia"] = (39.9526, -75.1652, "United States", "Philadelphia"),
                ["berlin"] = (52.5200, 13.4050, "Germany", "Berlin"),
                ["madrid"] = (40.4168, -3.7038, "Spain", "Madrid"),
                ["rome"] = (41.9028, 12.4964, "Italy", "Rome"),
                ["moscow"] = (55.7558, 37.6176, "Russia", "Moscow"),
                ["beijing"] = (39.9042, 116.4074, "China", "Beijing"),
                ["shanghai"] = (31.2304, 121.4737, "China", "Shanghai"),
                ["mumbai"] = (19.0760, 72.8777, "India", "Mumbai"),
                ["delhi"] = (28.7041, 77.1025, "India", "Delhi"),
                ["sydney"] = (-33.8688, 151.2093, "Australia", "Sydney"),
                ["melbourne"] = (-37.8136, 144.9631, "Australia", "Melbourne"),
                ["toronto"] = (43.6532, -79.3832, "Canada", "Toronto"),
                ["vancouver"] = (49.2827, -123.1207, "Canada", "Vancouver"),
                ["mexico city"] = (19.4326, -99.1332, "Mexico", "Mexico City"),
                ["sao paulo"] = (-23.5505, -46.6333, "Brazil", "São Paulo"),
                ["rio de janeiro"] = (-22.9068, -43.1729, "Brazil", "Rio de Janeiro"),
                ["buenos aires"] = (-34.6118, -58.3960, "Argentina", "Buenos Aires"),
                ["cairo"] = (30.0444, 31.2357, "Egypt", "Cairo"),
                ["johannesburg"] = (-26.2041, 28.0473, "South Africa", "Johannesburg"),
                ["nairobi"] = (-1.2921, 36.8219, "Kenya", "Nairobi"),
                ["lagos"] = (6.5244, 3.3792, "Nigeria", "Lagos"),
                ["dubai"] = (25.2048, 55.2708, "United Arab Emirates", "Dubai"),
                ["singapore"] = (1.3521, 103.8198, "Singapore", "Singapore"),
                ["bangkok"] = (13.7563, 100.5018, "Thailand", "Bangkok"),
                ["jakarta"] = (-6.2088, 106.8456, "Indonesia", "Jakarta"),
                ["manila"] = (14.5995, 120.9842, "Philippines", "Manila"),
                ["seoul"] = (37.5665, 126.9780, "South Korea", "Seoul"),
                ["taipei"] = (25.0330, 121.5654, "Taiwan", "Taipei"),
                ["hong kong"] = (22.3193, 114.1694, "Hong Kong", "Hong Kong"),
                ["osaka"] = (34.6937, 135.5023, "Japan", "Osaka"),
                ["kyoto"] = (35.0116, 135.7681, "Japan", "Kyoto"),
                ["yokohama"] = (35.4437, 139.6380, "Japan", "Yokohama"),
                ["nagoya"] = (35.1815, 136.9066, "Japan", "Nagoya"),
                ["sapporo"] = (43.0642, 141.3469, "Japan", "Sapporo"),
                ["fukuoka"] = (33.5904, 130.4017, "Japan", "Fukuoka"),
                ["kobe"] = (34.6901, 135.1956, "Japan", "Kobe"),
                ["kawasaki"] = (35.5307, 139.7029, "Japan", "Kawasaki"),
                ["saitama"] = (35.8617, 139.6455, "Japan", "Saitama"),
                ["hiroshima"] = (34.3853, 132.4553, "Japan", "Hiroshima"),
                ["sendai"] = (38.2682, 140.8694, "Japan", "Sendai"),
                ["kitakyushu"] = (33.8839, 130.8751, "Japan", "Kitakyushu"),
                ["chiba"] = (35.6074, 140.1065, "Japan", "Chiba"),
                ["sakai"] = (34.5733, 135.4821, "Japan", "Sakai"),
                ["niigata"] = (37.9161, 139.0364, "Japan", "Niigata"),
                ["hamamatsu"] = (34.7108, 137.7262, "Japan", "Hamamatsu"),
                ["okayama"] = (34.6618, 133.9344, "Japan", "Okayama"),
                ["kumamoto"] = (32.7898, 130.7417, "Japan", "Kumamoto"),
                ["shizuoka"] = (34.9756, 138.3826, "Japan", "Shizuoka"),
                ["sagamihara"] = (35.5685, 139.3586, "Japan", "Sagamihara"),
                ["nara"] = (34.6851, 135.8050, "Japan", "Nara"),
                ["matsuyama"] = (33.8416, 132.7654, "Japan", "Matsuyama"),
                ["kagoshima"] = (31.5602, 130.5581, "Japan", "Kagoshima"),
                ["niigata"] = (37.9161, 139.0364, "Japan", "Niigata"),
                ["nishinomiya"] = (34.7375, 135.3417, "Japan", "Nishinomiya"),
                ["kanazawa"] = (36.5613, 136.6562, "Japan", "Kanazawa"),
                ["fukushima"] = (37.7509, 140.4676, "Japan", "Fukushima"),
                ["nagano"] = (36.6486, 138.1948, "Japan", "Nagano"),
                ["toyama"] = (36.6953, 137.2113, "Japan", "Toyama"),
                ["gifu"] = (35.4231, 136.7607, "Japan", "Gifu"),
                ["matsumoto"] = (36.2380, 137.9720, "Japan", "Matsumoto"),
                ["takamatsu"] = (34.3401, 134.0464, "Japan", "Takamatsu"),
                ["hachioji"] = (35.6558, 139.3239, "Japan", "Hachioji"),
                ["koriyama"] = (37.4008, 140.3594, "Japan", "Koriyama"),
                ["kawaguchi"] = (35.8078, 139.7244, "Japan", "Kawaguchi"),
                ["yokkaichi"] = (34.9652, 136.6245, "Japan", "Yokkaichi"),
                ["akita"] = (39.7200, 140.1025, "Japan", "Akita"),
                ["utsunomiya"] = (36.5658, 139.8836, "Japan", "Utsunomiya"),
                ["naha"] = (26.2124, 127.6792, "Japan", "Naha"),
                ["kanazawa"] = (36.5613, 136.6562, "Japan", "Kanazawa"),
                ["nagasaki"] = (32.7503, 129.8779, "Japan", "Nagasaki"),
                ["nara"] = (34.6851, 135.8050, "Japan", "Nara"),
                ["matsuyama"] = (33.8416, 132.7654, "Japan", "Matsuyama"),
                ["kumamoto"] = (32.7898, 130.7417, "Japan", "Kumamoto"),
                ["shizuoka"] = (34.9756, 138.3826, "Japan", "Shizuoka"),
                ["sagamihara"] = (35.5685, 139.3586, "Japan", "Sagamihara"),
                ["nara"] = (34.6851, 135.8050, "Japan", "Nara"),
                ["matsuyama"] = (33.8416, 132.7654, "Japan", "Matsuyama"),
                ["kagoshima"] = (31.5602, 130.5581, "Japan", "Kagoshima"),
                ["niigata"] = (37.9161, 139.0364, "Japan", "Niigata"),
                ["nishinomiya"] = (34.7375, 135.3417, "Japan", "Nishinomiya"),
                ["kanazawa"] = (36.5613, 136.6562, "Japan", "Kanazawa"),
                ["fukushima"] = (37.7509, 140.4676, "Japan", "Fukushima"),
                ["nagano"] = (36.6486, 138.1948, "Japan", "Nagano"),
                ["toyama"] = (36.6953, 137.2113, "Japan", "Toyama"),
                ["gifu"] = (35.4231, 136.7607, "Japan", "Gifu"),
                ["matsumoto"] = (36.2380, 137.9720, "Japan", "Matsumoto"),
                ["takamatsu"] = (34.3401, 134.0464, "Japan", "Takamatsu"),
                ["hachioji"] = (35.6558, 139.3239, "Japan", "Hachioji"),
                ["koriyama"] = (37.4008, 140.3594, "Japan", "Koriyama"),
                ["kawaguchi"] = (35.8078, 139.7244, "Japan", "Kawaguchi"),
                ["yokkaichi"] = (34.9652, 136.6245, "Japan", "Yokkaichi"),
                ["akita"] = (39.7200, 140.1025, "Japan", "Akita"),
                ["utsunomiya"] = (36.5658, 139.8836, "Japan", "Utsunomiya"),
                ["naha"] = (26.2124, 127.6792, "Japan", "Naha")
            };

            // Try exact match first
            if (majorCities.TryGetValue(location.Trim(), out var cityData))
            {
                _logger.Info($"Found hardcoded location '{location}' -> {cityData.lat}, {cityData.lng}");
                return new GeocodingResult(cityData.lat, cityData.lng, $"{cityData.city}, {cityData.country}", 
                    cityData.country, cityData.city, true, "Hardcoded");
            }

            // Try partial matches
            var normalizedLocation = location.ToLowerInvariant().Trim();
            foreach (var kvp in majorCities)
            {
                if (kvp.Key.Contains(normalizedLocation) || normalizedLocation.Contains(kvp.Key))
                {
                    _logger.Info($"Found partial match for '{location}' -> {kvp.Key} ({kvp.Value.lat}, {kvp.Value.lng})");
                    return new GeocodingResult(kvp.Value.lat, kvp.Value.lng, $"{kvp.Value.city}, {kvp.Value.country}", 
                        kvp.Value.country, kvp.Value.city, true, "Hardcoded");
                }
            }

            _logger.Warn($"No geocoding result found for location: '{location}'");
            return null;
        }

        public async Task<string> ReverseGeocodeAsync(double latitude, double longitude)
        {
            try
            {
                var url = $"https://nominatim.openstreetmap.org/reverse?lat={latitude}&lon={longitude}&format=json&addressdetails=1";
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Living-Codex/1.0");
                
                var response = await _httpClient.GetStringAsync(url);
                var json = JsonDocument.Parse(response);
                
                if (json.RootElement.TryGetProperty("display_name", out var displayName))
                {
                    return displayName.GetString() ?? $"{latitude}, {longitude}";
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Reverse geocoding failed for {latitude}, {longitude}: {ex.Message}");
            }

            return $"{latitude}, {longitude}";
        }
    }
}
