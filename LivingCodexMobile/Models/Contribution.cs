using System.Text.Json.Serialization;

namespace LivingCodexMobile.Models
{
    public class Contribution
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("entityId")]
        public string EntityId { get; set; } = string.Empty;

        [JsonPropertyName("entityType")]
        public string EntityType { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;

        [JsonPropertyName("contributionType")]
        public string ContributionType { get; set; } = string.Empty;

        [JsonPropertyName("energy")]
        public double Energy { get; set; }

        [JsonPropertyName("resonance")]
        public double Resonance { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }
}

