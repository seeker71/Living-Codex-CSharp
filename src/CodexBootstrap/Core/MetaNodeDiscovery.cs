using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core;

/// <summary>
/// System for discovering and registering meta-nodes from C# attributes
/// </summary>
public static class MetaNodeDiscovery
{
    /// <summary>
    /// Discovers and registers all meta-nodes from attributes in the current assembly
    /// </summary>
    public static IEnumerable<Node> DiscoverMetaNodes(Assembly assembly)
    {
        var nodes = new List<Node>();

        // Discover meta-node types
        var metaNodeTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<MetaNodeAttribute>() != null);

        foreach (var type in metaNodeTypes)
        {
            var metaNode = CreateMetaNodeFromAttribute(type);
            if (metaNode != null)
            {
                nodes.Add(metaNode);
            }
        }

        // Discover response types
        var responseTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<ResponseTypeAttribute>() != null);

        foreach (var type in responseTypes)
        {
            var responseNode = CreateResponseNodeFromAttribute(type);
            if (responseNode != null)
            {
                nodes.Add(responseNode);
            }
        }

        // Discover request types
        var requestTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<RequestTypeAttribute>() != null);

        foreach (var type in requestTypes)
        {
            var requestNode = CreateRequestNodeFromAttribute(type);
            if (requestNode != null)
            {
                nodes.Add(requestNode);
            }
        }

        return nodes;
    }

    /// <summary>
    /// Creates a meta-node from a type with MetaNodeAttribute
    /// </summary>
    private static Node? CreateMetaNodeFromAttribute(Type type)
    {
        var attribute = type.GetCustomAttribute<MetaNodeAttribute>();
        if (attribute == null) return null;

        var fields = GetFieldSpecsFromType(type);
        var content = CreateContentFromType(type, attribute, fields);

        var meta = new Dictionary<string, object>
        {
            ["typeName"] = attribute.Name,
            ["kind"] = attribute.Kind ?? "Object",
            ["isMetaNode"] = true,
            ["isCore"] = attribute.IsCore,
            ["isState"] = attribute.IsState,
            ["isType"] = attribute.IsType,
            ["isApi"] = attribute.IsApi,
            ["isResponse"] = attribute.IsResponse,
            ["isRequest"] = attribute.IsRequest,
            ["isModule"] = attribute.IsModule
        };

        if (attribute.ParentType != null)
            meta["parentType"] = attribute.ParentType;

        if (attribute.Value != null)
            meta["value"] = attribute.Value;

        if (attribute.ModuleId != null)
            meta["moduleId"] = attribute.ModuleId;

        if (attribute.ApiName != null)
            meta["apiName"] = attribute.ApiName;

        if (attribute.Route != null)
            meta["route"] = attribute.Route;

        if (attribute.Verb != null)
            meta["verb"] = attribute.Verb;

        // Add additional metadata
        if (attribute.AdditionalMeta != null)
        {
            foreach (var kvp in attribute.AdditionalMeta)
            {
                meta[kvp.Key] = kvp.Value;
            }
        }

        return NodeHelpers.CreateNode(
            id: attribute.Id,
            typeId: attribute.TypeId,
            state: ContentState.Ice,
            title: attribute.Name,
            description: attribute.Description,
            content: NodeHelpers.CreateJsonContent(content),
            additionalMeta: meta
        );
    }

    /// <summary>
    /// Creates a response node from a type with ResponseTypeAttribute
    /// </summary>
    private static Node? CreateResponseNodeFromAttribute(Type type)
    {
        var attribute = type.GetCustomAttribute<ResponseTypeAttribute>();
        if (attribute == null) return null;

        var fields = GetFieldSpecsFromType(type);
        var content = new
        {
            name = attribute.Name,
            description = attribute.Description,
            fields = fields.Select(f => new
            {
                name = f.Name,
                type = f.Type,
                required = f.Required,
                description = f.Description,
                kind = f.Kind.ToString().ToLowerInvariant(),
                arrayItemType = f.ArrayItemType,
                referenceType = f.ReferenceType,
                enumValues = f.EnumValues
            }).ToArray()
        };

        var meta = new Dictionary<string, object>
        {
            ["responseName"] = attribute.Name,
            ["isMetaNode"] = true,
            ["isResponse"] = true
        };

        if (attribute.ModuleId != null)
            meta["moduleId"] = attribute.ModuleId;

        if (attribute.ApiName != null)
            meta["apiName"] = attribute.ApiName;

        return NodeHelpers.CreateNode(
            id: attribute.Id,
            typeId: "codex.meta/response",
            state: ContentState.Ice,
            title: attribute.Name,
            description: attribute.Description,
            content: NodeHelpers.CreateJsonContent(content),
            additionalMeta: meta
        );
    }

    /// <summary>
    /// Creates a request node from a type with RequestTypeAttribute
    /// </summary>
    private static Node? CreateRequestNodeFromAttribute(Type type)
    {
        var attribute = type.GetCustomAttribute<RequestTypeAttribute>();
        if (attribute == null) return null;

        var fields = GetFieldSpecsFromType(type);
        var content = new
        {
            name = attribute.Name,
            description = attribute.Description,
            fields = fields.Select(f => new
            {
                name = f.Name,
                type = f.Type,
                required = f.Required,
                description = f.Description,
                kind = f.Kind.ToString().ToLowerInvariant(),
                arrayItemType = f.ArrayItemType,
                referenceType = f.ReferenceType,
                enumValues = f.EnumValues
            }).ToArray()
        };

        var meta = new Dictionary<string, object>
        {
            ["requestName"] = attribute.Name,
            ["isMetaNode"] = true,
            ["isRequest"] = true
        };

        if (attribute.ModuleId != null)
            meta["moduleId"] = attribute.ModuleId;

        if (attribute.ApiName != null)
            meta["apiName"] = attribute.ApiName;

        return NodeHelpers.CreateNode(
            id: attribute.Id,
            typeId: "codex.meta/type",
            state: ContentState.Ice,
            title: attribute.Name,
            description: attribute.Description,
            content: NodeHelpers.CreateJsonContent(content),
            additionalMeta: meta
        );
    }

    /// <summary>
    /// Gets field specifications from a type's properties and fields
    /// </summary>
    private static List<FieldSpec> GetFieldSpecsFromType(Type type)
    {
        var fields = new List<FieldSpec>();

        // Get properties
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            var fieldAttribute = property.GetCustomAttribute<MetaNodeFieldAttribute>();
            if (fieldAttribute != null)
            {
                fields.Add(new FieldSpec(
                    Name: fieldAttribute.Name,
                    Type: fieldAttribute.Type,
                    Required: fieldAttribute.Required,
                    Description: fieldAttribute.Description,
                    Kind: Enum.TryParse<TypeKind>(fieldAttribute.Kind, true, out var kind) ? kind : TypeKind.Primitive,
                    ArrayItemType: fieldAttribute.ArrayItemType,
                    ReferenceType: fieldAttribute.ReferenceType,
                    EnumValues: fieldAttribute.EnumValues
                ));
            }
        }

        // Get fields
        var typeFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var field in typeFields)
        {
            var fieldAttribute = field.GetCustomAttribute<MetaNodeFieldAttribute>();
            if (fieldAttribute != null)
            {
                fields.Add(new FieldSpec(
                    Name: fieldAttribute.Name,
                    Type: fieldAttribute.Type,
                    Required: fieldAttribute.Required,
                    Description: fieldAttribute.Description,
                    Kind: Enum.TryParse<TypeKind>(fieldAttribute.Kind, true, out var kind) ? kind : TypeKind.Primitive,
                    ArrayItemType: fieldAttribute.ArrayItemType,
                    ReferenceType: fieldAttribute.ReferenceType,
                    EnumValues: fieldAttribute.EnumValues
                ));
            }
        }

        return fields;
    }

    /// <summary>
    /// Creates content from a type and its attribute
    /// </summary>
    private static object CreateContentFromType(Type type, MetaNodeAttribute attribute, List<FieldSpec> fields)
    {
        var content = new Dictionary<string, object>
        {
            ["name"] = attribute.Name,
            ["description"] = attribute.Description,
            ["kind"] = attribute.Kind ?? "Object"
        };

        if (attribute.ParentType != null)
            content["parentType"] = attribute.ParentType;

        if (attribute.Value != null)
            content["value"] = attribute.Value;

        if (attribute.ModuleId != null)
            content["moduleId"] = attribute.ModuleId;

        if (attribute.ApiName != null)
            content["apiName"] = attribute.ApiName;

        if (attribute.Route != null)
            content["route"] = attribute.Route;

        if (attribute.Verb != null)
            content["verb"] = attribute.Verb;

        if (fields.Any())
        {
            content["fields"] = fields.Select(f => new
            {
                name = f.Name,
                type = f.Type,
                required = f.Required,
                description = f.Description,
                kind = f.Kind.ToString().ToLowerInvariant(),
                arrayItemType = f.ArrayItemType,
                referenceType = f.ReferenceType,
                enumValues = f.EnumValues
            }).ToArray();
        }

        return content;
    }
}


