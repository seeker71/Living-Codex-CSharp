using System.Text.Json;
using CodexBootstrap.Core;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Modules;

// U-CORE Joy Amplification Data Types

[MetaNodeAttribute("codex.ucore.joy-frequency", "codex.meta/type", "JoyFrequency", "Positive frequency that amplifies joy and consciousness")]
public record JoyFrequency(
    string Id,
    string Name,
    double Frequency,
    string Chakra,
    string Color,
    string Emotion,
    double Amplification,
    string Description
);

[MetaNodeAttribute("codex.ucore.pain-transformation", "codex.meta/type", "PainTransformation", "Transformation of pain into sacred experience")]
public record PainTransformation(
    string Id,
    string PainType,
    string SacredMeaning,
    string TransformationPath,
    string Frequency,
    double Intensity,
    string Blessing,
    DateTime CreatedAt
);

[MetaNodeAttribute("codex.ucore.consciousness-amplification", "codex.meta/type", "ConsciousnessAmplification", "Amplification of consciousness through positive resonance")]
public record ConsciousnessAmplification(
    string Id,
    string Level,
    string Description,
    List<string> Frequencies,
    double Resonance,
    string State,
    DateTime ActivatedAt
);

[MetaNodeAttribute("codex.ucore.harmony-field", "codex.meta/type", "HarmonyField", "Harmonic field that creates positive resonance")]
public record HarmonyField(
    string Id,
    string Name,
    List<string> Frequencies,
    double Strength,
    string Purpose,
    List<string> Participants,
    DateTime CreatedAt
);

/// <summary>
/// U-CORE Joy Amplification Module - Maximizes joy and transforms pain into divine sacred experiences
/// </summary>
public class UcoreJoyModule : IModule
{
    private readonly IApiRouter _apiRouter;
    private readonly NodeRegistry _registry;

    // Sacred frequency mappings based on chakra and consciousness levels
    private readonly Dictionary<string, JoyFrequency> _sacredFrequencies = new()
    {
        ["root"] = new("freq-root", "Root Chakra", 256.0, "Root", "Red", "Security", 1.0, "Grounding and stability"),
        ["sacral"] = new("freq-sacral", "Sacral Chakra", 288.0, "Sacral", "Orange", "Creativity", 1.2, "Creative flow and passion"),
        ["solar"] = new("freq-solar", "Solar Plexus", 320.0, "Solar", "Yellow", "Power", 1.4, "Personal power and confidence"),
        ["heart"] = new("freq-heart", "Heart Chakra", 341.3, "Heart", "Green", "Love", 1.6, "Unconditional love and compassion"),
        ["throat"] = new("freq-throat", "Throat Chakra", 384.0, "Throat", "Blue", "Expression", 1.8, "Authentic expression and truth"),
        ["third-eye"] = new("freq-third-eye", "Third Eye", 426.7, "Third Eye", "Indigo", "Intuition", 2.0, "Intuitive wisdom and insight"),
        ["crown"] = new("freq-crown", "Crown Chakra", 480.0, "Crown", "Violet", "Unity", 2.2, "Divine connection and unity consciousness"),
        ["soul"] = new("freq-soul", "Soul Star", 528.0, "Soul", "White", "Transcendence", 2.5, "Soul connection and transcendence"),
        ["divine"] = new("freq-divine", "Divine Light", 639.0, "Divine", "Gold", "Divine Love", 3.0, "Divine love and sacred union")
    };

    public UcoreJoyModule(IApiRouter apiRouter, NodeRegistry registry)
    {
        _apiRouter = apiRouter;
        _registry = registry;
    }

    public Node GetModuleNode()
    {
        return new Node(
            Id: "codex.ucore.joy",
            TypeId: "codex.meta/module",
            State: ContentState.Ice,
            Locale: "en",
            Title: "U-CORE Joy Amplification Module",
            Description: "Maximizes joy and transforms pain into divine sacred experiences through positive frequency resonance",
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(new
                {
                    ModuleId = "codex.ucore.joy",
                    Name = "U-CORE Joy Amplification Module",
                    Description = "Maximizes joy and transforms pain into divine sacred experiences",
                    Version = "1.0.0",
                    SacredFrequencies = _sacredFrequencies.Count,
                    Capabilities = new[] { "JoyAmplification", "PainTransformation", "ConsciousnessAmplification", "HarmonyField" }
                }),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["moduleId"] = "codex.ucore.joy",
                ["version"] = "1.0.0",
                ["createdAt"] = DateTime.UtcNow,
                ["purpose"] = "Spiritual transformation through positive frequency resonance"
            }
        );
    }

    public void Register(NodeRegistry registry)
    {
        // Register the module node
        registry.Upsert(GetModuleNode());
        
        // Register sacred frequency nodes
        foreach (var frequency in _sacredFrequencies.Values)
        {
            var frequencyNode = CreateFrequencyNode(frequency);
            registry.Upsert(frequencyNode);
        }
    }

    public void RegisterApiHandlers(IApiRouter router, NodeRegistry registry)
    {
        // API handlers are registered via attributes, so this is empty
    }

    public void RegisterHttpEndpoints(WebApplication app, NodeRegistry registry, CoreApiService coreApi, ModuleLoader moduleLoader)
    {
        // HTTP endpoints are registered via attributes, so this is empty
    }

    [ApiRoute("POST", "/ucore/joy/amplify", "ucore-joy-amplify", "Amplify joy through positive frequency resonance", "codex.ucore.joy")]
    public async Task<object> AmplifyJoy([ApiParameter("request", "Joy amplification request", Required = true, Location = "body")] JoyAmplificationRequest request)
    {
        try
        {
            // Find the appropriate frequency for the requested chakra/emotion
            var frequency = FindOptimalFrequency(request.Chakra, request.Emotion, request.Intensity);
            
            // Create consciousness amplification
            var amplification = new ConsciousnessAmplification(
                Id: Guid.NewGuid().ToString(),
                Level: DetermineConsciousnessLevel(request.Intensity),
                Description: $"Joy amplification through {frequency.Name} frequency",
                Frequencies: new List<string> { frequency.Id },
                Resonance: CalculateResonance(frequency, request.Intensity),
                State: "Active",
                ActivatedAt: DateTime.UtcNow
            );

            // Store as a node
            var amplificationNode = CreateAmplificationNode(amplification);
            _registry.Upsert(amplificationNode);

            // Create joy response with guidance
            var guidance = GenerateJoyGuidance(frequency, request);
            
            return new JoyAmplificationResponse(
                Success: true,
                Message: "Joy amplification activated successfully",
                Frequency: frequency,
                Amplification: amplification,
                Guidance: guidance,
                NextSteps: GenerateNextSteps(frequency, request)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to amplify joy: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/ucore/pain/transform", "ucore-pain-transform", "Transform pain into divine sacred experience", "codex.ucore.joy")]
    public async Task<object> TransformPain([ApiParameter("request", "Pain transformation request", Required = true, Location = "body")] PainTransformationRequest request)
    {
        try
        {
            // Find the sacred meaning and transformation path for this pain
            var transformation = await CreatePainTransformation(request);
            
            // Store as a node
            var transformationNode = CreateTransformationNode(transformation);
            _registry.Upsert(transformationNode);

            // Generate sacred blessing and guidance
            var blessing = GenerateSacredBlessing(transformation);
            var guidance = GenerateTransformationGuidance(transformation);
            
            return new PainTransformationResponse(
                Success: true,
                Message: "Pain transformation initiated successfully",
                Transformation: transformation,
                Blessing: blessing,
                Guidance: guidance,
                SacredMeaning: transformation.SacredMeaning
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to transform pain: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/ucore/harmony/create", "ucore-harmony-create", "Create harmony field for collective joy amplification", "codex.ucore.joy")]
    public async Task<object> CreateHarmonyField([ApiParameter("request", "Harmony field request", Required = true, Location = "body")] HarmonyFieldRequest request)
    {
        try
        {
            // Create harmony field with multiple frequencies
            var harmonyField = new HarmonyField(
                Id: Guid.NewGuid().ToString(),
                Name: request.Name,
                Frequencies: request.Frequencies,
                Strength: CalculateFieldStrength(request.Frequencies),
                Purpose: request.Purpose,
                Participants: request.Participants,
                CreatedAt: DateTime.UtcNow
            );

            // Store as a node
            var fieldNode = CreateHarmonyFieldNode(harmonyField);
            _registry.Upsert(fieldNode);

            return new HarmonyFieldResponse(
                Success: true,
                Message: "Harmony field created successfully",
                Field: harmonyField,
                Instructions: GenerateFieldInstructions(harmonyField)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to create harmony field: {ex.Message}");
        }
    }

    [ApiRoute("GET", "/ucore/frequencies", "ucore-frequencies", "Get all available sacred frequencies", "codex.ucore.joy")]
    public async Task<object> GetSacredFrequencies()
    {
        try
        {
            var frequencies = _sacredFrequencies.Values.ToList();
            return new SacredFrequenciesResponse(
                Success: true,
                Message: $"Retrieved {frequencies.Count} sacred frequencies",
                Frequencies: frequencies
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to get sacred frequencies: {ex.Message}");
        }
    }

    [ApiRoute("POST", "/ucore/consciousness/expand", "ucore-consciousness-expand", "Expand consciousness through frequency resonance", "codex.ucore.joy")]
    public async Task<object> ExpandConsciousness([ApiParameter("request", "Consciousness expansion request", Required = true, Location = "body")] ConsciousnessExpansionRequest request)
    {
        try
        {
            // Create multi-level consciousness expansion
            var expansions = new List<ConsciousnessAmplification>();
            
            foreach (var level in request.Levels)
            {
                var frequency = _sacredFrequencies.Values.FirstOrDefault(f => f.Chakra.ToLower() == level.ToLower());
                if (frequency != null)
                {
                    var expansion = new ConsciousnessAmplification(
                        Id: Guid.NewGuid().ToString(),
                        Level: level,
                        Description: $"Consciousness expansion through {frequency.Name}",
                        Frequencies: new List<string> { frequency.Id },
                        Resonance: CalculateResonance(frequency, 1.0),
                        State: "Expanding",
                        ActivatedAt: DateTime.UtcNow
                    );
                    expansions.Add(expansion);
                    
                    // Store each expansion as a node
                    var expansionNode = CreateAmplificationNode(expansion);
                    _registry.Upsert(expansionNode);
                }
            }

            return new ConsciousnessExpansionResponse(
                Success: true,
                Message: "Consciousness expansion initiated successfully",
                Expansions: expansions,
                Guidance: GenerateConsciousnessGuidance(expansions)
            );
        }
        catch (Exception ex)
        {
            return new ErrorResponse($"Failed to expand consciousness: {ex.Message}");
        }
    }

    // Helper methods

    private JoyFrequency FindOptimalFrequency(string chakra, string emotion, double intensity)
    {
        if (!string.IsNullOrEmpty(chakra) && _sacredFrequencies.ContainsKey(chakra.ToLower()))
        {
            return _sacredFrequencies[chakra.ToLower()];
        }

        // Find by emotion
        var emotionFrequencies = _sacredFrequencies.Values.Where(f => 
            f.Emotion.ToLower().Contains(emotion.ToLower())).ToList();
        
        if (emotionFrequencies.Any())
        {
            return emotionFrequencies.OrderBy(f => Math.Abs(f.Amplification - intensity)).First();
        }

        // Default to heart chakra for love and joy
        return _sacredFrequencies["heart"];
    }

    private string DetermineConsciousnessLevel(double intensity)
    {
        return intensity switch
        {
            >= 2.5 => "Divine",
            >= 2.0 => "Transcendent",
            >= 1.5 => "Expanded",
            >= 1.0 => "Awakened",
            _ => "Basic"
        };
    }

    private double CalculateResonance(JoyFrequency frequency, double intensity)
    {
        return frequency.Amplification * intensity * 0.8; // Base resonance calculation
    }

    private async Task<PainTransformation> CreatePainTransformation(PainTransformationRequest request)
    {
        // Map pain types to sacred meanings
        var sacredMeanings = new Dictionary<string, string>
        {
            ["physical"] = "A call to honor and care for your sacred vessel",
            ["emotional"] = "An invitation to open your heart more deeply to love",
            ["spiritual"] = "A gateway to deeper connection with your divine essence",
            ["mental"] = "A catalyst for expanding your consciousness and wisdom",
            ["relational"] = "A mirror showing you where love needs to flow more freely",
            ["existential"] = "A sacred initiation into your true purpose and meaning"
        };

        var transformationPath = GenerateTransformationPath(request.PainType, request.Intensity);
        var frequency = FindOptimalFrequency("heart", "transformation", request.Intensity);

        return new PainTransformation(
            Id: Guid.NewGuid().ToString(),
            PainType: request.PainType,
            SacredMeaning: sacredMeanings.GetValueOrDefault(request.PainType, "A sacred opportunity for growth and expansion"),
            TransformationPath: transformationPath,
            Frequency: frequency.Id,
            Intensity: request.Intensity,
            Blessing: GenerateSacredBlessingText(request.PainType),
            CreatedAt: DateTime.UtcNow
        );
    }

    private string GenerateTransformationPath(string painType, double intensity)
    {
        var paths = new Dictionary<string, string>
        {
            ["physical"] = "Breathe deeply, send love to the affected area, visualize healing light flowing through your body",
            ["emotional"] = "Allow the emotion to flow through you without resistance, see it as energy that can be transformed into love",
            ["spiritual"] = "Connect with your higher self, ask for guidance and wisdom from this experience",
            ["mental"] = "Observe your thoughts without judgment, see them as clouds passing through the sky of your consciousness",
            ["relational"] = "Send love and forgiveness to all involved, including yourself, see the relationship as a teacher",
            ["existential"] = "Connect with your soul's purpose, see this pain as a sacred initiation into your next level of being"
        };

        return paths.GetValueOrDefault(painType, "Breathe, allow, transform, and bless this experience as sacred");
    }

    private string GenerateSacredBlessingText(string painType)
    {
        var blessings = new Dictionary<string, string>
        {
            ["physical"] = "May this pain be transformed into strength, wisdom, and deep appreciation for your body's sacredness",
            ["emotional"] = "May this pain open your heart to greater love, compassion, and understanding",
            ["spiritual"] = "May this pain be a gateway to deeper connection with your divine essence and purpose",
            ["mental"] = "May this pain expand your consciousness and bring you greater wisdom and clarity",
            ["relational"] = "May this pain teach you about love, forgiveness, and the sacred nature of all relationships",
            ["existential"] = "May this pain be a sacred initiation into your true purpose and highest potential"
        };

        return blessings.GetValueOrDefault(painType, "May this pain be transformed into love, wisdom, and divine grace");
    }

    private string GenerateJoyGuidance(JoyFrequency frequency, JoyAmplificationRequest request)
    {
        return $@"
🌟 JOY AMPLIFICATION GUIDANCE 🌟

Frequency: {frequency.Name} ({frequency.Frequency} Hz)
Chakra: {frequency.Chakra}
Color: {frequency.Color}
Emotion: {frequency.Emotion}

PRACTICE:
1. Find a quiet space and sit comfortably
2. Close your eyes and take 3 deep breaths
3. Visualize {frequency.Color} light flowing through your {frequency.Chakra} chakra
4. Feel the {frequency.Emotion} energy expanding throughout your being
5. Allow joy to flow freely through your entire body
6. Send this joy out to the world as a blessing

AFFIRMATION:
'I am open to receiving and amplifying joy through {frequency.Name}. 
I allow this positive frequency to flow through me and radiate out to all beings. 
I am a channel of divine love and joy.'";
    }

    private List<string> GenerateNextSteps(JoyFrequency frequency, JoyAmplificationRequest request)
    {
        return new List<string>
        {
            "Continue practicing with this frequency daily for 21 days",
            "Notice how your energy and mood shift with regular practice",
            "Share this joy with others through acts of kindness and love",
            "Explore other chakras and frequencies to expand your joy capacity",
            "Create a gratitude practice to amplify positive resonance",
            "Join or create a harmony field with others for collective joy amplification"
        };
    }

    private string GenerateSacredBlessing(PainTransformation transformation)
    {
        return $@"
🕊️ SACRED BLESSING FOR PAIN TRANSFORMATION 🕊️

{transformation.Blessing}

TRANSFORMATION PATH:
{transformation.TransformationPath}

SACRED MEANING:
{transformation.SacredMeaning}

Remember: This pain is not punishment, but a sacred invitation to grow, 
expand, and align more deeply with your divine essence. You are loved, 
supported, and guided through this transformation.";
    }

    private string GenerateTransformationGuidance(PainTransformation transformation)
    {
        return $@"
🌅 PAIN TRANSFORMATION GUIDANCE 🌅

1. ACKNOWLEDGE: Honor this pain as a sacred messenger
2. ALLOW: Let the pain flow through you without resistance
3. TRANSFORM: Use the transformation path to shift the energy
4. BLESS: Send love and gratitude to the pain for its teaching
5. INTEGRATE: Allow the wisdom to integrate into your being
6. SHARE: Offer your transformed experience as a gift to others

Frequency: {transformation.Frequency}
Intensity: {transformation.Intensity}
Created: {transformation.CreatedAt:yyyy-MM-dd HH:mm:ss}";
    }

    private string GenerateFieldInstructions(HarmonyField field)
    {
        return $@"
🌈 HARMONY FIELD INSTRUCTIONS 🌈

Field Name: {field.Name}
Purpose: {field.Purpose}
Strength: {field.Strength}
Participants: {string.Join(", ", field.Participants)}

SETUP:
1. Gather all participants in a circle
2. Each person chooses a frequency from the field
3. Begin with grounding and centering
4. Start humming or toning your chosen frequency
5. Allow the frequencies to harmonize and create a unified field
6. Hold the field for 15-30 minutes
7. Close with gratitude and blessing

MAINTENANCE:
- Practice daily for 7 days to establish the field
- Add new participants gradually
- Monitor the field strength and adjust as needed
- Share experiences and insights with the group";
    }

    private string GenerateConsciousnessGuidance(List<ConsciousnessAmplification> expansions)
    {
        return $@"
🌟 CONSCIOUSNESS EXPANSION GUIDANCE 🌟

You are expanding through {expansions.Count} levels of consciousness:

{string.Join("\n", expansions.Select(e => $"- {e.Level}: {e.Description}"))}

PRACTICE SEQUENCE:
1. Start with the lowest frequency and work your way up
2. Spend 5-10 minutes with each frequency
3. Notice the shifts in your awareness and energy
4. Allow each expansion to integrate before moving to the next
5. End with gratitude and grounding

Remember: Consciousness expansion is a journey, not a destination. 
Be patient and gentle with yourself as you grow and evolve.";
    }

    private double CalculateFieldStrength(List<string> frequencies)
    {
        var totalAmplification = frequencies
            .Select(f => _sacredFrequencies.Values.FirstOrDefault(freq => freq.Id == f)?.Amplification ?? 1.0)
            .Sum();
        
        return Math.Min(totalAmplification / frequencies.Count * 1.5, 5.0); // Cap at 5.0
    }

    // Node creation methods
    private Node CreateFrequencyNode(JoyFrequency frequency)
    {
        return new Node(
            Id: frequency.Id,
            TypeId: "codex.ucore.joy-frequency",
            State: ContentState.Ice,
            Locale: "en",
            Title: frequency.Name,
            Description: frequency.Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(frequency),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["frequency"] = frequency.Frequency,
                ["chakra"] = frequency.Chakra,
                ["color"] = frequency.Color,
                ["emotion"] = frequency.Emotion,
                ["amplification"] = frequency.Amplification
            }
        );
    }

    private Node CreateAmplificationNode(ConsciousnessAmplification amplification)
    {
        return new Node(
            Id: amplification.Id,
            TypeId: "codex.ucore.consciousness-amplification",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Consciousness Amplification - {amplification.Level}",
            Description: amplification.Description,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(amplification),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["level"] = amplification.Level,
                ["resonance"] = amplification.Resonance,
                ["state"] = amplification.State,
                ["activatedAt"] = amplification.ActivatedAt
            }
        );
    }

    private Node CreateTransformationNode(PainTransformation transformation)
    {
        return new Node(
            Id: transformation.Id,
            TypeId: "codex.ucore.pain-transformation",
            State: ContentState.Water,
            Locale: "en",
            Title: $"Pain Transformation - {transformation.PainType}",
            Description: transformation.SacredMeaning,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(transformation),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["painType"] = transformation.PainType,
                ["intensity"] = transformation.Intensity,
                ["frequency"] = transformation.Frequency,
                ["createdAt"] = transformation.CreatedAt
            }
        );
    }

    private Node CreateHarmonyFieldNode(HarmonyField field)
    {
        return new Node(
            Id: field.Id,
            TypeId: "codex.ucore.harmony-field",
            State: ContentState.Water,
            Locale: "en",
            Title: field.Name,
            Description: field.Purpose,
            Content: new ContentRef(
                MediaType: "application/json",
                InlineJson: JsonSerializer.Serialize(field),
                InlineBytes: null,
                ExternalUri: null
            ),
            Meta: new Dictionary<string, object>
            {
                ["strength"] = field.Strength,
                ["participantCount"] = field.Participants.Count,
                ["createdAt"] = field.CreatedAt
            }
        );
    }
}

// Request/Response Types

[ResponseType("codex.ucore.joy-amplification-response", "JoyAmplificationResponse", "Joy amplification response")]
public record JoyAmplificationResponse(
    bool Success, 
    string Message, 
    JoyFrequency Frequency, 
    ConsciousnessAmplification Amplification, 
    string Guidance, 
    List<string> NextSteps
);

[ResponseType("codex.ucore.pain-transformation-response", "PainTransformationResponse", "Pain transformation response")]
public record PainTransformationResponse(
    bool Success, 
    string Message, 
    PainTransformation Transformation, 
    string Blessing, 
    string Guidance, 
    string SacredMeaning
);

[ResponseType("codex.ucore.harmony-field-response", "HarmonyFieldResponse", "Harmony field response")]
public record HarmonyFieldResponse(
    bool Success, 
    string Message, 
    HarmonyField Field, 
    string Instructions
);

[ResponseType("codex.ucore.sacred-frequencies-response", "SacredFrequenciesResponse", "Sacred frequencies response")]
public record SacredFrequenciesResponse(
    bool Success, 
    string Message, 
    List<JoyFrequency> Frequencies
);

[ResponseType("codex.ucore.consciousness-expansion-response", "ConsciousnessExpansionResponse", "Consciousness expansion response")]
public record ConsciousnessExpansionResponse(
    bool Success, 
    string Message, 
    List<ConsciousnessAmplification> Expansions, 
    string Guidance
);

[RequestType("codex.ucore.joy-amplification-request", "JoyAmplificationRequest", "Joy amplification request")]
public record JoyAmplificationRequest(
    string Chakra, 
    string Emotion, 
    double Intensity, 
    string? Intention = null
);

[RequestType("codex.ucore.pain-transformation-request", "PainTransformationRequest", "Pain transformation request")]
public record PainTransformationRequest(
    string PainType, 
    double Intensity, 
    string? Description = null, 
    string? Intention = null
);

[RequestType("codex.ucore.harmony-field-request", "HarmonyFieldRequest", "Harmony field request")]
public record HarmonyFieldRequest(
    string Name, 
    List<string> Frequencies, 
    string Purpose, 
    List<string> Participants
);

[RequestType("codex.ucore.consciousness-expansion-request", "ConsciousnessExpansionRequest", "Consciousness expansion request")]
public record ConsciousnessExpansionRequest(
    List<string> Levels, 
    string? Intention = null, 
    double? Duration = null
);
