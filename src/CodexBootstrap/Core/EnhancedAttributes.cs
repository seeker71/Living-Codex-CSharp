using System;
using System.Collections.Generic;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Enhanced Meta-Node Attribute for automatic node generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MetaNodeAttribute : Attribute
    {
        public string Id { get; set; } = "";
        public string TypeId { get; set; } = "meta-node";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string Category { get; set; } = "";
        public bool AutoGenerate { get; set; } = true;
        public Dictionary<string, object> Properties { get; set; } = new();

        public MetaNodeAttribute() { }
        public MetaNodeAttribute(string id, string typeId, string name, string description)
        {
            Id = id;
            TypeId = typeId;
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Enhanced API Module Attribute with automatic node generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class EnhancedApiModuleAttribute : Attribute
    {
        public string ModuleId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public string Description { get; set; } = "";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string Category { get; set; } = "";
        public bool AutoGenerateNodes { get; set; } = true;
        public bool AutoGenerateRoutes { get; set; } = true;
        public string BasePath { get; set; } = "";
        public Dictionary<string, object> ModuleProperties { get; set; } = new();
    }

    /// <summary>
    /// Enhanced API Type Attribute with automatic meta-node generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class EnhancedApiTypeAttribute : Attribute
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "object";
        public string Description { get; set; } = "";
        public string Example { get; set; } = "";
        public string Version { get; set; } = "1.0.0";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public bool AutoGenerateMetaNode { get; set; } = true;
        public Dictionary<string, object> TypeProperties { get; set; } = new();
    }

    /// <summary>
    /// Enhanced API Route Attribute with automatic endpoint generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EnhancedApiRouteAttribute : Attribute
    {
        public string HttpMethod { get; set; } = "GET";
        public string Route { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string ModuleId { get; set; } = "";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public bool RequiresAuth { get; set; } = false;
        public string[] RequiredPermissions { get; set; } = Array.Empty<string>();
        public Type? RequestType { get; set; }
        public Type? ResponseType { get; set; }
        public bool AutoGenerate { get; set; } = true;
        public Dictionary<string, object> RouteProperties { get; set; } = new();
    }

    /// <summary>
    /// Auto-Generate Attribute for automatic code generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class AutoGenerateAttribute : Attribute
    {
        public string GeneratorType { get; set; } = "";
        public string Template { get; set; } = "";
        public Dictionary<string, object> Parameters { get; set; } = new();
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Node Field Attribute for automatic field generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NodeFieldAttribute : Attribute
    {
        public string FieldId { get; set; } = "";
        public string FieldType { get; set; } = "string";
        public string Description { get; set; } = "";
        public bool Required { get; set; } = false;
        public object? DefaultValue { get; set; }
        public string[] ValidationRules { get; set; } = Array.Empty<string>();
        public Dictionary<string, object> FieldProperties { get; set; } = new();
    }

    /// <summary>
    /// U-CORE Specific Attributes
    /// </summary>
    
    /// <summary>
    /// U-CORE Concept Attribute for automatic concept node generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class UCoreConceptAttribute : Attribute
    {
        public string ConceptId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double Frequency { get; set; } = 0.0;
        public double Resonance { get; set; } = 0.0;
        public string Category { get; set; } = "";
        public string[] RelatedConcepts { get; set; } = Array.Empty<string>();
        public bool AutoGenerate { get; set; } = true;
    }

    /// <summary>
    /// U-CORE Frequency Attribute for automatic frequency node generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class UCoreFrequencyAttribute : Attribute
    {
        public double Value { get; set; } = 0.0;
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public double Resonance { get; set; } = 0.0;
        public string[] Effects { get; set; } = Array.Empty<string>();
        public bool AutoGenerate { get; set; } = true;
    }

    /// <summary>
    /// U-CORE Resonance Attribute for automatic resonance node generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class UCoreResonanceAttribute : Attribute
    {
        public string ResonanceId { get; set; } = "";
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public double Amplitude { get; set; } = 0.0;
        public double Phase { get; set; } = 0.0;
        public string[] Frequencies { get; set; } = Array.Empty<string>();
        public bool AutoGenerate { get; set; } = true;
    }

    /// <summary>
    /// Breath Framework Attribute for automatic breath loop integration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class BreathFrameworkAttribute : Attribute
    {
        public string[] RequiredPhases { get; set; } = new[] { "compose", "expand", "validate", "contract" };
        public string Phase { get; set; } = "";
        public bool UseBreathLoop { get; set; } = true;
        public Dictionary<string, object> PhaseProperties { get; set; } = new();
    }

    /// <summary>
    /// LLM Integration Attribute for automatic LLM integration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class LLMIntegrationAttribute : Attribute
    {
        public string Provider { get; set; } = "ollama";
        public string Model { get; set; } = "";
        public string Mode { get; set; } = "chat";
        public bool AutoGenerate { get; set; } = true;
        public Dictionary<string, object> LLMProperties { get; set; } = new();
    }

    /// <summary>
    /// Dynamic Content Attribute for automatic content generation
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class DynamicContentAttribute : Attribute
    {
        public string ContentType { get; set; } = "text";
        public string Generator { get; set; } = "llm";
        public string Template { get; set; } = "";
        public bool AutoRefresh { get; set; } = false;
        public int RefreshInterval { get; set; } = 0;
        public Dictionary<string, object> ContentProperties { get; set; } = new();
    }



    // Legacy attribute compatibility with different constructor signatures
          [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
          public class ResponseTypeAttribute : Attribute
          {
              public string Name { get; set; } = "";
              public string Type { get; set; } = "";
              public string Description { get; set; } = "";
              public string Example { get; set; } = "";
              public string Id { get; set; } = "";

              public ResponseTypeAttribute() { }
              public ResponseTypeAttribute(string id, string name, string description)
              {
                  Id = id;
                  Name = name;
                  Description = description;
              }
              public ResponseTypeAttribute(string id, string name, string type, string description)
              {
                  Id = id;
                  Name = name;
                  Type = type;
                  Description = description;
              }
              // Additional constructor for compatibility
              public ResponseTypeAttribute(string id, string name, string type, string description, string example)
              {
                  Id = id;
                  Name = name;
                  Type = type;
                  Description = description;
                  Example = example;
              }
          }

          [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property)]
          public class RequestTypeAttribute : Attribute
          {
              public string Name { get; set; } = "";
              public string Type { get; set; } = "";
              public string Description { get; set; } = "";
              public string Example { get; set; } = "";
              public string Id { get; set; } = "";

              public RequestTypeAttribute() { }
              public RequestTypeAttribute(string id, string name, string description)
              {
                  Id = id;
                  Name = name;
                  Description = description;
              }
              public RequestTypeAttribute(string id, string name, string type, string description)
              {
                  Id = id;
                  Name = name;
                  Type = type;
                  Description = description;
              }
              // Additional constructor for compatibility
              public RequestTypeAttribute(string id, string name, string type, string description, string example)
              {
                  Id = id;
                  Name = name;
                  Type = type;
                  Description = description;
                  Example = example;
              }
          }



    // Legacy attribute compatibility - MetaNodeField without "Attribute" suffix
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class MetaNodeField : Attribute
    {
        public bool Required { get; set; } = false;
        public string Description { get; set; } = "";
        public string Type { get; set; } = "";
        public string Kind { get; set; } = "";
        public string ArrayItemType { get; set; } = "";
        public string[] EnumValues { get; set; } = Array.Empty<string>();
        public string ReferenceType { get; set; } = "";

              public MetaNodeField() { }
              public MetaNodeField(string name, string type, bool required = true, string description = "", string kind = "", string arrayItemType = "", string[] enumValues = null, string referenceType = "")
              {
                  Type = type;
                  Required = required;
                  Description = description;
                  Kind = kind;
                  ArrayItemType = arrayItemType;
                  EnumValues = enumValues ?? Array.Empty<string>();
                  ReferenceType = referenceType;
              }
    }
}
