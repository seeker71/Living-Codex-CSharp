using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// Joy Calculation Data Types

[MetaNode("codex.joy.calculation", "codex.meta/type", "JoyCalculation", "Mathematical calculation of joy amplification")]
public record JoyCalculation(
    string Id,
    string UserId,
    double BaselineJoy,
    double FrequencyResonance,
    double AmplificationFactor,
    double CalculatedJoy,
    double JoyIncrease,
    double JoyIncreasePercentage,
    DateTime CalculatedAt
);

[MetaNode("codex.joy.progression", "codex.meta/type", "JoyProgression", "Track joy progression over time")]
public record JoyProgression(
    string Id,
    string UserId,
    List<JoyCalculation> Calculations,
    double AverageJoyIncrease,
    double TotalJoyIncrease,
    DateTime StartedAt,
    DateTime LastCalculatedAt
);

/// <summary>
/// Joy Calculator Module - Shows mathematical relationship between frequency resonance and joy increase
/// </summary>
public class JoyCalculatorModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;

    public JoyCalculatorModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.joy.calculator",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "Joy Calculator Module",
            Description: "Mathematical calculation of joy amplification through frequency resonance",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.joy.calculator",
                    Name = "Joy Calculator Module",
                    Description = "Shows how frequency resonance increases joy mathematically",
                    Version = "1.0.0",
                    Formula = "CalculatedJoy = BaselineJoy * (1 + FrequencyResonance * AmplificationFactor)",
                    Capabilities = new[] { "JoyCalculation", "JoyProgression", "JoyPrediction", "JoyOptimization" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.joy.calculator",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow,
                ["purpose"] = "Mathematical demonstration of joy amplification"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        registry.Upsert(GetModuleNode());
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attributes
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attributes
    }

    [ApiRoute("POST", "/joy/calculate", "joy-calculate", "Calculate joy amplification from frequency resonance", "codex.joy.calculator")]
    public async Task<object> CalculateJoy([ApiParameter("request", "Joy calculation request", Required = true, Location = "body")] JoyCalculationRequest request)
    {
        try
        {
            // Get frequency data
            var frequency = GetFrequencyData(request.FrequencyId);
            if (frequency == null)
            {
                return new ErrorResponse($"Frequency '{request.FrequencyId}' not found");
            }

            // Calculate joy amplification using the formula:
            // CalculatedJoy = BaselineJoy * (1 + FrequencyResonance * AmplificationFactor)
            var frequencyResonance = CalculateFrequencyResonance(frequency, request.Intensity, request.Duration);
            var amplificationFactor = frequency.Amplification;
            var calculatedJoy = request.BaselineJoy * (1 + frequencyResonance * amplificationFactor);
            var joyIncrease = calculatedJoy - request.BaselineJoy;
            var joyIncreasePercentage = (joyIncrease / request.BaselineJoy) * 100;

            var calculation = new JoyCalculation(
                Id: Guid.NewGuid().ToString(),
                UserId: request.UserId,
                BaselineJoy: request.BaselineJoy,
                FrequencyResonance: frequencyResonance,
                AmplificationFactor: amplificationFactor,
                CalculatedJoy: Math.Round(calculatedJoy, 2),
                JoyIncrease: Math.Round(joyIncrease, 2),
                JoyIncreasePercentage: Math.Round(joyIncreasePercentage, 1),
                CalculatedAt: DateTime.UtcNow
            );

            // Store calculation as a node
            var calculationNode = CreateCalculationNode(calculation);
            _registry.Upsert(calculationNode);

            // Generate insights and recommendations
            var insights = GenerateJoyInsights(calculation, frequency);
            var recommendations = GenerateJoyRecommendations(calculation, frequency);

            return new JoyCalculationResponse(
                Success: true,
                Message: "Joy calculation completed successfully",
                Calculation: calculation,
                Frequency: frequency,
                Insights: insights,
                Recommendations: recommendations,
                Formula: $"CalculatedJoy = {request.BaselineJoy} * (1 + {frequencyResonance:F2} * {amplificationFactor:F2}) = {calculatedJoy:F2}",
                NextSteps: GenerateNextSteps(calculation)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to calculate joy: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/joy/progression/{userId}", "joy-progression", "Get joy progression for a user", "codex.joy.calculator")]
    public async Task<object> GetJoyProgression([ApiParameter("userId", "User ID", Required = true, Location = "path")] string userId)
    {
        try
        {
            // Get all calculations for this user
            var allNodes = _registry.AllNodes();
            var calculationNodes = allNodes
                .Where(n => n.TypeId == "codex.joy.calculation" && 
                           n.Meta?.GetValueOrDefault("userId")?.ToString() == userId)
                .OrderBy(n => n.Meta?.GetValueOrDefault("calculatedAt", DateTime.MinValue))
                .ToList();

            var calculations = new List<JoyCalculation>();
            foreach (var node in calculationNodes)
            {
                if (node.Content?.InlineJson != null)
                {
                    var calculation = JsonSerializer.Deserialize<JoyCalculation>(node.Content.InlineJson);
                    if (calculation != null)
                    {
                        calculations.Add(calculation);
                    }
                }
            }

            if (!calculations.Any())
            {
                return new ErrorResponse("No joy calculations found for this user");
            }

            var averageJoyIncrease = calculations.Average(c => c.JoyIncrease);
            var totalJoyIncrease = calculations.Sum(c => c.JoyIncrease);
            var startedAt = calculations.Min(c => c.CalculatedAt);
            var lastCalculatedAt = calculations.Max(c => c.CalculatedAt);

            var progression = new JoyProgression(
                Id: Guid.NewGuid().ToString(),
                UserId: userId,
                Calculations: calculations,
                AverageJoyIncrease: Math.Round(averageJoyIncrease, 2),
                TotalJoyIncrease: Math.Round(totalJoyIncrease, 2),
                StartedAt: startedAt,
                LastCalculatedAt: lastCalculatedAt
            );

            // Store progression as a node
            var progressionNode = CreateProgressionNode(progression);
            _registry.Upsert(progressionNode);

            var insights = GenerateProgressionInsights(progression);
            var trends = AnalyzeJoyTrends(calculations);

            return new JoyProgressionResponse(
                Success: true,
                Message: $"Retrieved {calculations.Count} joy calculations",
                Progression: progression,
                Insights: insights,
                Trends: trends,
                Recommendations: GenerateProgressionRecommendations(progression)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get joy progression: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/joy/predict", "joy-predict", "Predict future joy levels based on current progression", "codex.joy.calculator")]
    public async Task<object> PredictJoy([ApiParameter("request", "Joy prediction request", Required = true, Location = "body")] JoyPredictionRequest request)
    {
        try
        {
            // Get user's progression
            var progressionResult = await GetJoyProgression(request.UserId);
            if (progressionResult is ErrorResponse error)
            {
                return error;
            }

            var progressionResponse = (JoyProgressionResponse)progressionResult;
            var progression = progressionResponse.Progression;

            // Calculate trend
            var recentCalculations = progression.Calculations.TakeLast(5).ToList();
            if (recentCalculations.Count < 2)
            {
                return new ErrorResponse("Not enough data for prediction. Need at least 2 calculations.");
            }

            var trend = CalculateTrend(recentCalculations);
            var predictions = new List<JoyPrediction>();

            // Predict future joy levels
            for (int i = 1; i <= request.DaysAhead; i++)
            {
                var predictedJoy = progression.Calculations.Last().CalculatedJoy + (trend * i);
                var predictedIncrease = predictedJoy - progression.Calculations.Last().CalculatedJoy;
                var predictedPercentage = (predictedIncrease / progression.Calculations.Last().CalculatedJoy) * 100;

                predictions.Add(new JoyPrediction(
                    Day: i,
                    PredictedJoy: Math.Round(predictedJoy, 2),
                    PredictedIncrease: Math.Round(predictedIncrease, 2),
                    PredictedPercentage: Math.Round(predictedPercentage, 1),
                    Confidence: CalculateConfidence(recentCalculations.Count, i)
                ));
            }

            return new JoyPredictionResponse(
                Success: true,
                Message: $"Predicted joy levels for {request.DaysAhead} days ahead",
                Predictions: predictions,
                Trend: trend,
                Confidence: CalculateOverallConfidence(recentCalculations.Count),
                Recommendations: GeneratePredictionRecommendations(predictions, trend)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to predict joy: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/joy/optimize", "joy-optimize", "Optimize joy amplification strategy", "codex.joy.calculator")]
    public async Task<object> OptimizeJoy([ApiParameter("request", "Joy optimization request", Required = true, Location = "body")] JoyOptimizationRequest request)
    {
        try
        {
            // Get user's current state
            var progressionResult = await GetJoyProgression(request.UserId);
            if (progressionResult is ErrorResponse error)
            {
                return error;
            }

            var progressionResponse = (JoyProgressionResponse)progressionResult;
            var progression = progressionResponse.Progression;

            // Analyze what works best for this user
            var bestFrequencies = AnalyzeBestFrequencies(progression.Calculations);
            var optimalDuration = AnalyzeOptimalDuration(progression.Calculations);
            var optimalIntensity = AnalyzeOptimalIntensity(progression.Calculations);

            // Generate optimization strategy
            var strategy = new JoyOptimizationStrategy(
                UserId: request.UserId,
                CurrentJoyLevel: progression.Calculations.Last().CalculatedJoy,
                TargetJoyLevel: request.TargetJoyLevel,
                BestFrequencies: bestFrequencies,
                OptimalDuration: optimalDuration,
                OptimalIntensity: optimalIntensity,
                RecommendedFrequency: GetRecommendedFrequency(bestFrequencies),
                EstimatedTimeToTarget: CalculateTimeToTarget(progression, request.TargetJoyLevel),
                ActionPlan: GenerateActionPlan(progression, request.TargetJoyLevel)
            );

            return new JoyOptimizationResponse(
                Success: true,
                Message: "Joy optimization strategy generated successfully",
                Strategy: strategy,
                Insights: GenerateOptimizationInsights(strategy),
                NextSteps: GenerateOptimizationNextSteps(strategy)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to optimize joy: {ex.Message}");
        }
    }

    // Helper methods

    private FrequencyData? GetFrequencyData(string frequencyId)
    {
        var frequencies = new Dictionary<string, FrequencyData>
        {
            ["freq-root"] = new("Root Chakra", 256.0, 1.0, "Grounding and stability"),
            ["freq-sacral"] = new("Sacral Chakra", 288.0, 1.2, "Creative flow and passion"),
            ["freq-solar"] = new("Solar Plexus", 320.0, 1.4, "Personal power and confidence"),
            ["freq-heart"] = new("Heart Chakra", 341.3, 1.6, "Unconditional love and compassion"),
            ["freq-throat"] = new("Throat Chakra", 384.0, 1.8, "Authentic expression and truth"),
            ["freq-third-eye"] = new("Third Eye", 426.7, 2.0, "Intuitive wisdom and insight"),
            ["freq-crown"] = new("Crown Chakra", 480.0, 2.2, "Divine connection and unity consciousness"),
            ["freq-soul"] = new("Soul Star", 528.0, 2.5, "Soul connection and transcendence"),
            ["freq-divine"] = new("Divine Light", 639.0, 3.0, "Divine love and sacred union")
        };

        return frequencies.GetValueOrDefault(frequencyId);
    }

    private double CalculateFrequencyResonance(FrequencyData frequency, double intensity, int duration)
    {
        // Base resonance calculation: frequency * intensity * duration factor
        var durationFactor = Math.Min(duration / 10.0, 1.0); // Cap at 1.0 for 10+ minutes
        return (frequency.Frequency / 1000.0) * intensity * durationFactor;
    }

    private string GenerateJoyInsights(JoyCalculation calculation, FrequencyData frequency)
    {
        var insights = new List<string>();

        if (calculation.JoyIncreasePercentage > 50)
        {
            insights.Add($"🌟 Excellent! You achieved a {calculation.JoyIncreasePercentage:F1}% increase in joy!");
        }
        else if (calculation.JoyIncreasePercentage > 25)
        {
            insights.Add($"✨ Great! You achieved a {calculation.JoyIncreasePercentage:F1}% increase in joy!");
        }
        else if (calculation.JoyIncreasePercentage > 10)
        {
            insights.Add($"👍 Good! You achieved a {calculation.JoyIncreasePercentage:F1}% increase in joy!");
        }
        else
        {
            insights.Add($"💡 You achieved a {calculation.JoyIncreasePercentage:F1}% increase in joy. Try increasing intensity or duration for better results.");
        }

        insights.Add($"The {frequency.Name} frequency is working well for you.");
        insights.Add($"Your joy level increased from {calculation.BaselineJoy} to {calculation.CalculatedJoy}.");

        return string.Join("\n", insights);
    }

    private List<string> GenerateJoyRecommendations(JoyCalculation calculation, FrequencyData frequency)
    {
        var recommendations = new List<string>();

        if (calculation.JoyIncreasePercentage < 25)
        {
            recommendations.Add("Try increasing the intensity to 1.5 or higher");
            recommendations.Add("Practice for 15-20 minutes instead of 10");
            recommendations.Add("Consider trying a different frequency that resonates more with you");
        }

        if (calculation.JoyIncreasePercentage > 50)
        {
            recommendations.Add("This frequency is working excellently for you!");
            recommendations.Add("Consider using it daily for maximum benefit");
            recommendations.Add("Try combining it with other frequencies for even greater joy");
        }

        recommendations.Add("Track your joy progression over time to see long-term benefits");
        recommendations.Add("Share your success with others to amplify collective joy");

        return recommendations;
    }

    private List<string> GenerateNextSteps(JoyCalculation calculation)
    {
        return new List<string>
        {
            "Continue practicing with this frequency daily",
            "Track your joy progression over the next 7 days",
            "Try different frequencies to find what works best for you",
            "Increase intensity gradually as you become more comfortable",
            "Share your joy with others to amplify the positive effects"
        };
    }

    private string GenerateProgressionInsights(JoyProgression progression)
    {
        var insights = new List<string>();

        insights.Add($"You've been practicing joy amplification for {(progression.LastCalculatedAt - progression.StartedAt).Days} days");
        insights.Add($"Your average joy increase per session is {progression.AverageJoyIncrease:F2}");
        insights.Add($"Your total joy increase is {progression.TotalJoyIncrease:F2}");

        if (progression.AverageJoyIncrease > 1.0)
        {
            insights.Add("🌟 Excellent progress! You're consistently increasing your joy levels");
        }
        else if (progression.AverageJoyIncrease > 0.5)
        {
            insights.Add("✨ Good progress! You're building a solid foundation for joy");
        }
        else
        {
            insights.Add("💡 Consider adjusting your practice for better results");
        }

        return string.Join("\n", insights);
    }

    private List<string> AnalyzeJoyTrends(List<JoyCalculation> calculations)
    {
        var trends = new List<string>();

        if (calculations.Count >= 3)
        {
            var recent = calculations.TakeLast(3).ToList();
            var trend = recent.Last().JoyIncrease - recent.First().JoyIncrease;

            if (trend > 0)
            {
                trends.Add("📈 Your joy increases are getting better over time!");
            }
            else if (trend < 0)
            {
                trends.Add("📉 Your joy increases are decreasing. Consider adjusting your practice.");
            }
            else
            {
                trends.Add("📊 Your joy increases are consistent. Great job!");
            }
        }

        return trends;
    }

    private List<string> GenerateProgressionRecommendations(JoyProgression progression)
    {
        var recommendations = new List<string>();

        if (progression.AverageJoyIncrease < 0.5)
        {
            recommendations.Add("Try increasing your practice frequency to daily");
            recommendations.Add("Experiment with different frequencies to find what works best");
            recommendations.Add("Consider increasing intensity or duration");
        }

        if (progression.AverageJoyIncrease > 1.0)
        {
            recommendations.Add("Excellent! Consider sharing your techniques with others");
            recommendations.Add("Try advanced practices like multi-frequency resonance");
            recommendations.Add("Explore group joy amplification practices");
        }

        recommendations.Add("Set a goal for your next week of practice");
        recommendations.Add("Track your progress and celebrate your successes");

        return recommendations;
    }

    private double CalculateTrend(List<JoyCalculation> calculations)
    {
        if (calculations.Count < 2) return 0;

        var x = Enumerable.Range(0, calculations.Count).Select(i => (double)i).ToArray();
        var y = calculations.Select(c => c.JoyIncrease).ToArray();

        var n = x.Length;
        var sumX = x.Sum();
        var sumY = y.Sum();
        var sumXY = x.Zip(y, (a, b) => a * b).Sum();
        var sumXX = x.Sum(a => a * a);

        return (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
    }

    private double CalculateConfidence(int dataPoints, int daysAhead)
    {
        // Confidence decreases with more days ahead and increases with more data points
        var baseConfidence = Math.Min(dataPoints / 10.0, 1.0);
        var decayFactor = Math.Max(1.0 - (daysAhead * 0.1), 0.1);
        return Math.Round(baseConfidence * decayFactor, 2);
    }

    private double CalculateOverallConfidence(int dataPoints)
    {
        return Math.Round(Math.Min(dataPoints / 10.0, 1.0), 2);
    }

    private List<string> GeneratePredictionRecommendations(List<JoyPrediction> predictions, double trend)
    {
        var recommendations = new List<string>();

        if (trend > 0)
        {
            recommendations.Add("Your joy is trending upward! Keep up the great work!");
        }
        else if (trend < 0)
        {
            recommendations.Add("Your joy trend is declining. Consider adjusting your practice.");
        }

        var maxPrediction = predictions.Max(p => p.PredictedJoy);
        if (maxPrediction > 3.0)
        {
            recommendations.Add("You're on track to reach high joy levels! Consider sharing your techniques.");
        }

        return recommendations;
    }

    private List<string> AnalyzeBestFrequencies(List<JoyCalculation> calculations)
    {
        // This would analyze which frequencies work best for the user
        // For now, return a simple analysis
        return new List<string> { "Heart Chakra", "Solar Plexus", "Crown Chakra" };
    }

    private int AnalyzeOptimalDuration(List<JoyCalculation> calculations)
    {
        // This would analyze the optimal duration based on user's results
        // For now, return a simple analysis
        return 15;
    }

    private double AnalyzeOptimalIntensity(List<JoyCalculation> calculations)
    {
        // This would analyze the optimal intensity based on user's results
        // For now, return a simple analysis
        return 1.5;
    }

    private string GetRecommendedFrequency(List<string> bestFrequencies)
    {
        return bestFrequencies.FirstOrDefault() ?? "Heart Chakra";
    }

    private int CalculateTimeToTarget(JoyProgression progression, double targetJoyLevel)
    {
        var currentJoy = progression.Calculations.Last().CalculatedJoy;
        var averageIncrease = progression.AverageJoyIncrease;
        
        if (averageIncrease <= 0) return 999; // Can't reach target
        
        var neededIncrease = targetJoyLevel - currentJoy;
        return (int)Math.Ceiling(neededIncrease / averageIncrease);
    }

    private List<string> GenerateActionPlan(JoyProgression progression, double targetJoyLevel)
    {
        return new List<string>
        {
            "Practice daily with your best frequency for 15-20 minutes",
            "Gradually increase intensity as you become more comfortable",
            "Track your progress weekly and adjust as needed",
            "Consider group practices for additional amplification",
            "Set specific goals and celebrate your achievements"
        };
    }

    private string GenerateOptimizationInsights(JoyOptimizationStrategy strategy)
    {
        return $@"
🎯 JOY OPTIMIZATION INSIGHTS

Current Joy Level: {strategy.CurrentJoyLevel:F2}
Target Joy Level: {strategy.TargetJoyLevel:F2}
Estimated Time to Target: {strategy.EstimatedTimeToTarget} days

Best Frequencies for You:
{string.Join("\n", strategy.BestFrequencies.Select(f => $"• {f}"))}

Optimal Duration: {strategy.OptimalDuration} minutes
Optimal Intensity: {strategy.OptimalIntensity:F1}
Recommended Frequency: {strategy.RecommendedFrequency}

You're on the right track! Keep practicing and you'll reach your target joy level.";
    }

    private List<string> GenerateOptimizationNextSteps(JoyOptimizationStrategy strategy)
    {
        return new List<string>
        {
            $"Start with {strategy.RecommendedFrequency} for {strategy.OptimalDuration} minutes daily",
            $"Use intensity {strategy.OptimalIntensity:F1} for optimal results",
            "Track your progress daily and adjust as needed",
            "Consider combining multiple frequencies for greater amplification",
            "Share your success with others to amplify collective joy"
        };
    }

    // Node creation methods
    private Node CreateCalculationNode(JoyCalculation calculation)
    {
        return new Node(
            Id: calculation.Id,
            TypeId: "codex.joy.calculation",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Joy Calculation - {calculation.CalculatedJoy:F2}",
            Description: $"Joy increased by {calculation.JoyIncreasePercentage:F1}%",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(calculation),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["userId"] = calculation.UserId,
                ["baselineJoy"] = calculation.BaselineJoy,
                ["calculatedJoy"] = calculation.CalculatedJoy,
                ["joyIncrease"] = calculation.JoyIncrease,
                ["joyIncreasePercentage"] = calculation.JoyIncreasePercentage,
                ["calculatedAt"] = calculation.CalculatedAt
            }
        );
    }

    private Node CreateProgressionNode(JoyProgression progression)
    {
        return new Node(
            Id: progression.Id,
            TypeId: "codex.joy.progression",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Joy Progression - {progression.UserId}",
            Description: $"Average joy increase: {progression.AverageJoyIncrease:F2}",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(progression),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["userId"] = progression.UserId,
                ["calculationCount"] = progression.Calculations.Count,
                ["averageJoyIncrease"] = progression.AverageJoyIncrease,
                ["totalJoyIncrease"] = progression.TotalJoyIncrease,
                ["startedAt"] = progression.StartedAt,
                ["lastCalculatedAt"] = progression.LastCalculatedAt
            }
        );
    }
}

// Data types
public record FrequencyData(string Name, double Frequency, double Amplification, string Description);

// Request/Response Types
[ResponseType("codex.joy.calculation-response", "JoyCalculationResponse", "Joy calculation response")]
public record JoyCalculationResponse(
    bool Success,
    string Message,
    JoyCalculation Calculation,
    FrequencyData Frequency,
    string Insights,
    List<string> Recommendations,
    string Formula,
    List<string> NextSteps
);

[ResponseType("codex.joy.progression-response", "JoyProgressionResponse", "Joy progression response")]
public record JoyProgressionResponse(
    bool Success,
    string Message,
    JoyProgression Progression,
    string Insights,
    List<string> Trends,
    List<string> Recommendations
);

[ResponseType("codex.joy.prediction-response", "JoyPredictionResponse", "Joy prediction response")]
public record JoyPredictionResponse(
    bool Success,
    string Message,
    List<JoyPrediction> Predictions,
    double Trend,
    double Confidence,
    List<string> Recommendations
);

[ResponseType("codex.joy.optimization-response", "JoyOptimizationResponse", "Joy optimization response")]
public record JoyOptimizationResponse(
    bool Success,
    string Message,
    JoyOptimizationStrategy Strategy,
    string Insights,
    List<string> NextSteps
);

[RequestType("codex.joy.calculation-request", "JoyCalculationRequest", "Joy calculation request")]
public record JoyCalculationRequest(
    string UserId,
    string FrequencyId,
    double BaselineJoy,
    double Intensity,
    int Duration
);

[RequestType("codex.joy.prediction-request", "JoyPredictionRequest", "Joy prediction request")]
public record JoyPredictionRequest(
    string UserId,
    int DaysAhead
);

[RequestType("codex.joy.optimization-request", "JoyOptimizationRequest", "Joy optimization request")]
public record JoyOptimizationRequest(
    string UserId,
    double TargetJoyLevel
);

public record JoyPrediction(
    int Day,
    double PredictedJoy,
    double PredictedIncrease,
    double PredictedPercentage,
    double Confidence
);

public record JoyOptimizationStrategy(
    string UserId,
    double CurrentJoyLevel,
    double TargetJoyLevel,
    List<string> BestFrequencies,
    int OptimalDuration,
    double OptimalIntensity,
    string RecommendedFrequency,
    int EstimatedTimeToTarget,
    List<string> ActionPlan
);
