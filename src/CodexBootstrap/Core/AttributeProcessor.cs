using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Enhanced Attribute Processor for automatic node and meta-node generation
    /// </summary>
    public class AttributeProcessor
    {
        private readonly NodeRegistry _nodeRegistry;
        private readonly Dictionary<string, Node> _generatedNodes = new();

        public AttributeProcessor(NodeRegistry nodeRegistry)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
        }

        /// <summary>
        /// Process all modules and generate nodes from attributes
        /// </summary>
        public void ProcessAllModules(IEnumerable<Type> moduleTypes)
        {
            foreach (var moduleType in moduleTypes)
            {
                ProcessModule(moduleType);
            }
        }

        /// <summary>
        /// Process a single module and generate nodes from its attributes
        /// </summary>
        public void ProcessModule(Type moduleType)
        {
            // Process module-level attributes
            ProcessModuleAttributes(moduleType);

            // Process method-level attributes
            ProcessMethodAttributes(moduleType);

            // Process property-level attributes
            ProcessPropertyAttributes(moduleType);

            // Process field-level attributes
            ProcessFieldAttributes(moduleType);
        }

        private void ProcessModuleAttributes(Type moduleType)
        {
            // Process EnhancedApiModuleAttribute
            var moduleAttr = moduleType.GetCustomAttribute<EnhancedApiModuleAttribute>();
            if (moduleAttr?.AutoGenerateNodes == true)
            {
                var moduleNode = GenerateModuleNode(moduleType, moduleAttr);
                _nodeRegistry.Upsert(moduleNode);
                _generatedNodes[moduleNode.Id] = moduleNode;
            }

            // Process UCoreConceptAttribute
            var conceptAttr = moduleType.GetCustomAttribute<UCoreConceptAttribute>();
            if (conceptAttr?.AutoGenerate == true)
            {
                var conceptNode = GenerateConceptNode(moduleType, conceptAttr);
                _nodeRegistry.Upsert(conceptNode);
                _generatedNodes[conceptNode.Id] = conceptNode;
            }

            // Process UCoreFrequencyAttribute
            var frequencyAttr = moduleType.GetCustomAttribute<UCoreFrequencyAttribute>();
            if (frequencyAttr?.AutoGenerate == true)
            {
                var frequencyNode = GenerateFrequencyNode(moduleType, frequencyAttr);
                _nodeRegistry.Upsert(frequencyNode);
                _generatedNodes[frequencyNode.Id] = frequencyNode;
            }

            // Process UCoreResonanceAttribute
            var resonanceAttr = moduleType.GetCustomAttribute<UCoreResonanceAttribute>();
            if (resonanceAttr?.AutoGenerate == true)
            {
                var resonanceNode = GenerateResonanceNode(moduleType, resonanceAttr);
                _nodeRegistry.Upsert(resonanceNode);
                _generatedNodes[resonanceNode.Id] = resonanceNode;
            }
        }

        private void ProcessMethodAttributes(Type moduleType)
        {
            var methods = moduleType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var method in methods)
            {
                // Process EnhancedApiRouteAttribute
                var routeAttr = method.GetCustomAttribute<EnhancedApiRouteAttribute>();
                if (routeAttr?.AutoGenerate == true)
                {
                    var routeNode = GenerateRouteNode(moduleType, method, routeAttr);
                    _nodeRegistry.Upsert(routeNode);
                    _generatedNodes[routeNode.Id] = routeNode;
                }

                // Process UCoreConceptAttribute
                var conceptAttr = method.GetCustomAttribute<UCoreConceptAttribute>();
                if (conceptAttr?.AutoGenerate == true)
                {
                    var conceptNode = GenerateConceptNode(method, conceptAttr);
                    _nodeRegistry.Upsert(conceptNode);
                    _generatedNodes[conceptNode.Id] = conceptNode;
                }

                // Process UCoreFrequencyAttribute
                var frequencyAttr = method.GetCustomAttribute<UCoreFrequencyAttribute>();
                if (frequencyAttr?.AutoGenerate == true)
                {
                    var frequencyNode = GenerateFrequencyNode(method, frequencyAttr);
                    _nodeRegistry.Upsert(frequencyNode);
                    _generatedNodes[frequencyNode.Id] = frequencyNode;
                }
            }
        }

        private void ProcessPropertyAttributes(Type moduleType)
        {
            var properties = moduleType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var property in properties)
            {
                // Process EnhancedApiTypeAttribute
                var typeAttr = property.GetCustomAttribute<EnhancedApiTypeAttribute>();
                if (typeAttr?.AutoGenerateMetaNode == true)
                {
                    var metaNode = GenerateMetaNode(property, typeAttr);
                    _nodeRegistry.Upsert(metaNode);
                    _generatedNodes[metaNode.Id] = metaNode;
                }

                // Process NodeFieldAttribute
                var fieldAttr = property.GetCustomAttribute<NodeFieldAttribute>();
                if (fieldAttr != null)
                {
                    var fieldNode = GenerateFieldNode(property, fieldAttr);
                    _nodeRegistry.Upsert(fieldNode);
                    _generatedNodes[fieldNode.Id] = fieldNode;
                }

                // Process DynamicContentAttribute
                var contentAttr = property.GetCustomAttribute<DynamicContentAttribute>();
                if (contentAttr != null)
                {
                    var contentNode = GenerateContentNode(property, contentAttr);
                    _nodeRegistry.Upsert(contentNode);
                    _generatedNodes[contentNode.Id] = contentNode;
                }
            }
        }

        private void ProcessFieldAttributes(Type moduleType)
        {
            var fields = moduleType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var field in fields)
            {
                // Process NodeFieldAttribute
                var fieldAttr = field.GetCustomAttribute<NodeFieldAttribute>();
                if (fieldAttr != null)
                {
                    var fieldNode = GenerateFieldNode(field, fieldAttr);
                    _nodeRegistry.Upsert(fieldNode);
                    _generatedNodes[fieldNode.Id] = fieldNode;
                }
            }
        }

        private Node GenerateModuleNode(Type moduleType, EnhancedApiModuleAttribute attr)
        {
            var nodeId = !string.IsNullOrEmpty(attr.ModuleId) ? attr.ModuleId : $"module.{moduleType.Name.ToLower()}";
            
            return new Node
            {
                Id = nodeId,
                TypeId = "module",
                State = ContentState.Active,
                Locale = "en",
                Title = !string.IsNullOrEmpty(attr.Name) ? attr.Name : moduleType.Name,
                Description = attr.Description,
                Content = new Dictionary<string, object>
                {
                    ["version"] = attr.Version,
                    ["category"] = attr.Category,
                    ["tags"] = attr.Tags,
                    ["basePath"] = attr.BasePath,
                    ["autoGenerateNodes"] = attr.AutoGenerateNodes,
                    ["autoGenerateRoutes"] = attr.AutoGenerateRoutes,
                    ["moduleType"] = moduleType.FullName ?? "",
                    ["properties"] = attr.ModuleProperties
                }
            };
        }

        private Node GenerateConceptNode(MemberInfo member, UCoreConceptAttribute attr)
        {
            var nodeId = !string.IsNullOrEmpty(attr.ConceptId) ? attr.ConceptId : $"concept.{member.Name.ToLower()}";
            
            return new Node
            {
                Id = nodeId,
                TypeId = "ucore-concept",
                State = ContentState.Active,
                Locale = "en",
                Title = !string.IsNullOrEmpty(attr.Name) ? attr.Name : member.Name,
                Description = attr.Description,
                Content = new Dictionary<string, object>
                {
                    ["frequency"] = attr.Frequency,
                    ["resonance"] = attr.Resonance,
                    ["category"] = attr.Category,
                    ["relatedConcepts"] = attr.RelatedConcepts,
                    ["memberType"] = member.MemberType.ToString(),
                    ["memberName"] = member.Name
                }
            };
        }

        private Node GenerateFrequencyNode(MemberInfo member, UCoreFrequencyAttribute attr)
        {
            var nodeId = $"frequency.{attr.Value}hz";
            
            return new Node
            {
                Id = nodeId,
                TypeId = "ucore-frequency",
                State = ContentState.Active,
                Locale = "en",
                Title = !string.IsNullOrEmpty(attr.Name) ? attr.Name : $"{attr.Value} Hz",
                Description = attr.Description,
                Content = new Dictionary<string, object>
                {
                    ["value"] = attr.Value,
                    ["resonance"] = attr.Resonance,
                    ["category"] = attr.Category,
                    ["effects"] = attr.Effects,
                    ["memberType"] = member.MemberType.ToString(),
                    ["memberName"] = member.Name
                }
            };
        }

        private Node GenerateResonanceNode(Type moduleType, UCoreResonanceAttribute attr)
        {
            var nodeId = !string.IsNullOrEmpty(attr.ResonanceId) ? attr.ResonanceId : $"resonance.{moduleType.Name.ToLower()}";
            
            return new Node
            {
                Id = nodeId,
                TypeId = "ucore-resonance",
                State = ContentState.Active,
                Locale = "en",
                Title = !string.IsNullOrEmpty(attr.Name) ? attr.Name : moduleType.Name,
                Description = attr.Description,
                Content = new Dictionary<string, object>
                {
                    ["amplitude"] = attr.Amplitude,
                    ["phase"] = attr.Phase,
                    ["frequencies"] = attr.Frequencies,
                    ["moduleType"] = moduleType.FullName ?? ""
                }
            };
        }

        private Node GenerateRouteNode(Type moduleType, MethodInfo method, EnhancedApiRouteAttribute attr)
        {
            var nodeId = !string.IsNullOrEmpty(attr.Name) ? attr.Name : $"route.{moduleType.Name.ToLower()}.{method.Name.ToLower()}";
            
            return new Node
            {
                Id = nodeId,
                TypeId = "api-route",
                State = ContentState.Active,
                Locale = "en",
                Title = !string.IsNullOrEmpty(attr.Name) ? attr.Name : method.Name,
                Description = attr.Description,
                Content = new Dictionary<string, object>
                {
                    ["httpMethod"] = attr.HttpMethod,
                    ["route"] = attr.Route,
                    ["moduleId"] = attr.ModuleId,
                    ["tags"] = attr.Tags,
                    ["requiresAuth"] = attr.RequiresAuth,
                    ["requiredPermissions"] = attr.RequiredPermissions,
                    ["requestType"] = attr.RequestType?.FullName ?? "",
                    ["responseType"] = attr.ResponseType?.FullName ?? "",
                    ["methodName"] = method.Name,
                    ["moduleType"] = moduleType.FullName ?? ""
                }
            };
        }

        private Node GenerateMetaNode(PropertyInfo property, EnhancedApiTypeAttribute attr)
        {
            var nodeId = !string.IsNullOrEmpty(attr.Id) ? attr.Id : $"meta.{property.DeclaringType?.Name.ToLower()}.{property.Name.ToLower()}";
            
            return new Node
            {
                Id = nodeId,
                TypeId = "meta-node",
                State = ContentState.Active,
                Locale = "en",
                Title = !string.IsNullOrEmpty(attr.Name) ? attr.Name : property.Name,
                Description = attr.Description,
                Content = new Dictionary<string, object>
                {
                    ["type"] = attr.Type,
                    ["version"] = attr.Version,
                    ["tags"] = attr.Tags,
                    ["example"] = attr.Example,
                    ["propertyType"] = property.PropertyType.FullName ?? "",
                    ["propertyName"] = property.Name,
                    ["declaringType"] = property.DeclaringType?.FullName ?? "",
                    ["properties"] = attr.TypeProperties
                }
            };
        }

        private Node GenerateFieldNode(MemberInfo member, NodeFieldAttribute attr)
        {
            var nodeId = !string.IsNullOrEmpty(attr.FieldId) ? attr.FieldId : $"field.{member.DeclaringType?.Name.ToLower()}.{member.Name.ToLower()}";
            
            return new Node
            {
                Id = nodeId,
                TypeId = "node-field",
                State = ContentState.Active,
                Locale = "en",
                Title = !string.IsNullOrEmpty(attr.FieldId) ? attr.FieldId : member.Name,
                Description = attr.Description,
                Content = new Dictionary<string, object>
                {
                    ["fieldType"] = attr.FieldType,
                    ["required"] = attr.Required,
                    ["defaultValue"] = attr.DefaultValue,
                    ["validationRules"] = attr.ValidationRules,
                    ["memberType"] = member.MemberType.ToString(),
                    ["memberName"] = member.Name,
                    ["declaringType"] = member.DeclaringType?.FullName ?? "",
                    ["properties"] = attr.FieldProperties
                }
            };
        }

        private Node GenerateContentNode(PropertyInfo property, DynamicContentAttribute attr)
        {
            var nodeId = $"content.{property.DeclaringType?.Name.ToLower()}.{property.Name.ToLower()}";
            
            return new Node
            {
                Id = nodeId,
                TypeId = "dynamic-content",
                State = ContentState.Active,
                Locale = "en",
                Title = property.Name,
                Description = $"Dynamic content for {property.Name}",
                Content = new Dictionary<string, object>
                {
                    ["contentType"] = attr.ContentType,
                    ["generator"] = attr.Generator,
                    ["template"] = attr.Template,
                    ["autoRefresh"] = attr.AutoRefresh,
                    ["refreshInterval"] = attr.RefreshInterval,
                    ["propertyType"] = property.PropertyType.FullName ?? "",
                    ["propertyName"] = property.Name,
                    ["declaringType"] = property.DeclaringType?.FullName ?? "",
                    ["properties"] = attr.ContentProperties
                }
            };
        }

        /// <summary>
        /// Get all generated nodes
        /// </summary>
        public Dictionary<string, Node> GetGeneratedNodes()
        {
            return new Dictionary<string, Node>(_generatedNodes);
        }

        /// <summary>
        /// Get generated nodes by type
        /// </summary>
        public IEnumerable<Node> GetGeneratedNodesByType(string typeId)
        {
            return _generatedNodes.Values.Where(n => n.TypeId == typeId);
        }
    }
}
