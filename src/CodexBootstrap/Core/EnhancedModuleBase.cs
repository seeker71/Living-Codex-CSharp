using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CodexBootstrap.Runtime;

namespace CodexBootstrap.Core
{
    /// <summary>
    /// Enhanced Module Base Class with automatic attribute processing
    /// </summary>
    [EnhancedApiModule(
        ModuleId = "enhanced-module-base",
        Name = "Enhanced Module Base",
        Version = "1.0.0",
        Description = "Base class for modules with enhanced attribute processing",
        Category = "core",
        AutoGenerateNodes = true,
        AutoGenerateRoutes = true
    )]
    public abstract class EnhancedModuleBase : IModule
    {
        protected readonly NodeRegistry _nodeRegistry;
        protected readonly AttributeProcessor _attributeProcessor;
        protected readonly Dictionary<string, Node> _moduleNodes = new();

        public EnhancedModuleBase(NodeRegistry nodeRegistry)
        {
            _nodeRegistry = nodeRegistry ?? throw new ArgumentNullException(nameof(nodeRegistry));
            _attributeProcessor = new AttributeProcessor(nodeRegistry);
            
            // Auto-process this module's attributes
            ProcessModuleAttributes();
        }

        /// <summary>
        /// Process this module's attributes and generate nodes
        /// </summary>
        protected virtual void ProcessModuleAttributes()
        {
            _attributeProcessor.ProcessModule(GetType());
            
            // Store generated nodes for this module
            var generatedNodes = _attributeProcessor.GetGeneratedNodes();
            foreach (var node in generatedNodes.Values)
            {
                _moduleNodes[node.Id] = node;
            }
        }

        /// <summary>
        /// Get the module's auto-generated node
        /// </summary>
        public virtual Node GetModuleNode()
        {
            var moduleAttr = GetType().GetCustomAttribute<EnhancedApiModuleAttribute>();
            if (moduleAttr != null)
            {
                var nodeId = !string.IsNullOrEmpty(moduleAttr.ModuleId) ? moduleAttr.ModuleId : $"module.{GetType().Name.ToLower()}";
                
                return new Node
                {
                    Id = nodeId,
                    TypeId = "module",
                    State = ContentState.Active,
                    Locale = "en",
                    Title = !string.IsNullOrEmpty(moduleAttr.Name) ? moduleAttr.Name : GetType().Name,
                    Description = moduleAttr.Description,
                    Content = new Dictionary<string, object>
                    {
                        ["version"] = moduleAttr.Version,
                        ["category"] = moduleAttr.Category,
                        ["tags"] = moduleAttr.Tags,
                        ["autoGenerateNodes"] = moduleAttr.AutoGenerateNodes,
                        ["autoGenerateRoutes"] = moduleAttr.AutoGenerateRoutes,
                        ["moduleType"] = GetType().FullName ?? "",
                        ["properties"] = moduleAttr.ModuleProperties
                    }
                };
            }

            // Fallback to basic module node
            return new Node
            {
                Id = $"module.{GetType().Name.ToLower()}",
                TypeId = "module",
                State = ContentState.Active,
                Locale = "en",
                Title = GetType().Name,
                Description = $"Module: {GetType().Name}",
                Content = new Dictionary<string, object>
                {
                    ["version"] = "1.0.0",
                    ["moduleType"] = GetType().FullName ?? ""
                }
            };
        }

        /// <summary>
        /// Register this module with the node registry
        /// </summary>
        public virtual void Register(NodeRegistry registry)
        {
            var moduleNode = GetModuleNode();
            registry.Upsert(moduleNode);
            _moduleNodes[moduleNode.Id] = moduleNode;

            // Register all generated nodes
            foreach (var node in _moduleNodes.Values)
            {
                registry.Upsert(node);
            }
        }

        /// <summary>
        /// Register API handlers (to be implemented by derived classes)
        /// </summary>
        public abstract void RegisterApiHandlers(IApiRouter router, NodeRegistry registry);

        /// <summary>
        /// Register HTTP endpoints with automatic route generation
        /// </summary>
        public virtual void RegisterHttpEndpoints(WebApplication app, NodeRegistry nodeRegistry, CoreApiService coreApi, ModuleLoader moduleLoader)
        {
            var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            
            foreach (var method in methods)
            {
                var routeAttr = method.GetCustomAttribute<EnhancedApiRouteAttribute>();
                if (routeAttr?.AutoGenerate == true)
                {
                    RegisterEndpoint(app, method, routeAttr);
                }
            }
        }

        /// <summary>
        /// Register a single endpoint
        /// </summary>
        protected virtual void RegisterEndpoint(WebApplication app, MethodInfo method, EnhancedApiRouteAttribute routeAttr)
        {
            try
            {
                var endpointName = !string.IsNullOrEmpty(routeAttr.Name) ? routeAttr.Name : $"{GetType().Name.ToLower()}-{method.Name.ToLower()}";
                
                switch (routeAttr.HttpMethod.ToUpper())
                {
                    case "GET":
                        app.MapGet(routeAttr.Route, CreateHandlerDelegate(method))
                            .WithName(endpointName)
                            .WithTags(routeAttr.Tags);
                        break;
                    case "POST":
                        app.MapPost(routeAttr.Route, CreateHandlerDelegate(method))
                            .WithName(endpointName)
                            .WithTags(routeAttr.Tags);
                        break;
                    case "PUT":
                        app.MapPut(routeAttr.Route, CreateHandlerDelegate(method))
                            .WithName(endpointName)
                            .WithTags(routeAttr.Tags);
                        break;
                    case "DELETE":
                        app.MapDelete(routeAttr.Route, CreateHandlerDelegate(method))
                            .WithName(endpointName)
                            .WithTags(routeAttr.Tags);
                        break;
                    case "PATCH":
                        app.MapPatch(routeAttr.Route, CreateHandlerDelegate(method))
                            .WithName(endpointName)
                            .WithTags(routeAttr.Tags);
                        break;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail the entire registration
                Console.WriteLine($"Error registering endpoint {method.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a handler delegate for the method
        /// </summary>
        protected virtual Delegate CreateHandlerDelegate(MethodInfo method)
        {
            // This is a simplified implementation
            // In a real implementation, you'd need to handle parameter binding, return types, etc.
            return (Delegate)Delegate.CreateDelegate(typeof(Func<object>), this, method);
        }

        /// <summary>
        /// Get all nodes generated for this module
        /// </summary>
        public virtual IEnumerable<Node> GetModuleNodes()
        {
            return _moduleNodes.Values;
        }

        /// <summary>
        /// Get nodes by type
        /// </summary>
        public virtual IEnumerable<Node> GetNodesByType(string typeId)
        {
            return _moduleNodes.Values.Where(n => n.TypeId == typeId);
        }

        /// <summary>
        /// Get U-CORE concept nodes
        /// </summary>
        public virtual IEnumerable<Node> GetConceptNodes()
        {
            return GetNodesByType("ucore-concept");
        }

        /// <summary>
        /// Get U-CORE frequency nodes
        /// </summary>
        public virtual IEnumerable<Node> GetFrequencyNodes()
        {
            return GetNodesByType("ucore-frequency");
        }

        /// <summary>
        /// Get U-CORE resonance nodes
        /// </summary>
        public virtual IEnumerable<Node> GetResonanceNodes()
        {
            return GetNodesByType("ucore-resonance");
        }

        /// <summary>
        /// Get API route nodes
        /// </summary>
        public virtual IEnumerable<Node> GetRouteNodes()
        {
            return GetNodesByType("api-route");
        }

        /// <summary>
        /// Get meta-nodes
        /// </summary>
        public virtual IEnumerable<Node> GetMetaNodes()
        {
            return GetNodesByType("meta-node");
        }
    }
}
