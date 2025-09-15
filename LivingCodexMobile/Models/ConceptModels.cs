using System.Text.Json.Serialization;

namespace LivingCodexMobile.Models;

public class OnboardingStep
{
    public int StepNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public bool IsNewsFeedStep { get; set; }
}

public class Concept
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public int Complexity { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ImageUrl { get; set; }
    public double? Resonance { get; set; }
    public double? Energy { get; set; }
    public bool IsInterested { get; set; }
    public int InterestCount { get; set; }
    public List<ConceptRelationship> Relationships { get; set; } = new();
}

public class ConceptRelationship
{
    public string Id { get; set; } = string.Empty;
    public string FromConceptId { get; set; } = string.Empty;
    public string ToConceptId { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = string.Empty;
    public double Weight { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConceptQualityAssessment
{
    public string ConceptId { get; set; } = string.Empty;
    public double OverallScore { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
    public DateTime AssessedAt { get; set; }
}

// Request models
public record CreateConceptRequest(
    string Name,
    string Description,
    string Domain = "",
    int Complexity = 1,
    List<string>? Tags = null,
    Dictionary<string, object>? Metadata = null
);

public record UpdateConceptRequest(
    string? Name = null,
    string? Description = null,
    string? Domain = null,
    int? Complexity = null,
    List<string>? Tags = null,
    Dictionary<string, object>? Metadata = null
);

public record ConceptQuery(
    string? Domain = null,
    int? Complexity = null,
    string? SearchTerm = null,
    int? Skip = null,
    int? Take = null
);

public record ConceptSearchRequest(
    string? SearchTerm = null,
    string[]? Domains = null,
    int[]? Complexities = null,
    string[]? Tags = null,
    string? SortBy = null,
    bool SortDescending = false,
    int? Skip = null,
    int? Take = null
);

public record ConceptDiscoveryRequest(
    string Content,
    string ContentType = "text/plain",
    string[]? Domains = null,
    int? MaxConcepts = null
);

public record ConceptRelateRequest(
    string FromConceptId,
    string ToConceptId,
    string RelationshipType,
    double Weight = 1.0,
    Dictionary<string, object>? Metadata = null
);

public record ConceptInterestRequest(
    string UserId,
    string ConceptId,
    bool Interested
);

public record ConceptQualityAssessmentRequest(
    string ConceptId,
    string[]? Standards = null
);

// Response models
public record ConceptResponse(
    bool Success,
    Concept? Concept,
    string? Message = null
);

public record ConceptListResponse(
    bool Success,
    List<Concept> Concepts,
    int TotalCount,
    int Skip,
    int Take,
    string? Message = null
);

public record ConceptSearchResponse(
    bool Success,
    List<Concept> Concepts,
    int TotalCount,
    int Skip,
    int Take,
    string? Message = null
);

public record ConceptDiscoveryResponse(
    bool Success,
    List<Concept> Concepts,
    int TotalCount,
    string? Message = null
);

public record ConceptRelateResponse(
    bool Success,
    ConceptRelationship? Relationship,
    string? Message = null
);

public record ConceptQualityAssessmentResponse(
    bool Success,
    ConceptQualityAssessment? Assessment,
    string? Message = null
);

// UI-specific models
public class ConceptCard
{
    public Concept Concept { get; set; } = null!;
    public bool IsExpanded { get; set; }
    public bool IsLoading { get; set; }
    public string DisplayImage { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public List<string> DisplayTags { get; set; } = new();
}

public class ConceptFilter
{
    public string? SearchTerm { get; set; }
    public string? Domain { get; set; }
    public int? MinComplexity { get; set; }
    public int? MaxComplexity { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool ShowInterestedOnly { get; set; }
    public string SortBy { get; set; } = "name";
    public bool SortDescending { get; set; }
}

public class ConceptDiscoveryResult
{
    public List<Concept> DiscoveredConcepts { get; set; } = new();
    public List<Concept> RelatedConcepts { get; set; } = new();
    public List<Concept> RecommendedConcepts { get; set; } = new();
    public bool IsLoading { get; set; }
    public string? ErrorMessage { get; set; }
}
