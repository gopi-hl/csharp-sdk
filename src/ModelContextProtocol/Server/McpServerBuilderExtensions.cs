using ModelContextProtocol.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// Extension methods for <see cref="IMcpServerBuilder"/>.
    /// </summary>
    public static class McpServerBuilderExtensions
    {
        /// <summary>
        /// Registers all prompt methods from the given assembly.
        /// </summary>
        /// <param name="builder">The server builder.</param>
        /// <param name="assembly">The assembly to scan for prompt methods.</param>
        /// <returns>The server builder for chaining.</returns>
        [RequiresUnreferencedCode("Scans assembly for prompt methods using reflection.")]
        public static IMcpServerBuilder WithPrompts(this IMcpServerBuilder builder, Assembly assembly)
        {
            var provider = new AttributePromptProvider().RegisterAssembly(assembly);
            return RegisterPromptHandlers(builder, provider);
        }

        /// <summary>
        /// Registers all prompt methods from the given type.
        /// </summary>
        /// <param name="builder">The server builder.</param>
        /// <param name="type">The type to scan for prompt methods.</param>
        /// <returns>The server builder for chaining.</returns>
        public static IMcpServerBuilder WithPrompts(this IMcpServerBuilder builder, 
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
            var provider = new AttributePromptProvider().RegisterType(type);
            return RegisterPromptHandlers(builder, provider);
        }

        /// <summary>
        /// Registers all prompt methods from the given instance.
        /// </summary>
        /// <param name="builder">The server builder.</param>
        /// <param name="instance">The instance to scan for prompt methods.</param>
        /// <returns>The server builder for chaining.</returns>
        public static IMcpServerBuilder WithPrompts(this IMcpServerBuilder builder, object instance)
        {
            var provider = new AttributePromptProvider().RegisterInstance(instance);
            return RegisterPromptHandlers(builder, provider);
        }

        /// <summary>
        /// Registers prompt methods from multiple sources (assemblies, types, or instances).
        /// </summary>
        /// <param name="builder">The server builder.</param>
        /// <param name="configure">Action to configure the prompt provider.</param>
        /// <returns>The server builder for chaining.</returns>
        public static IMcpServerBuilder WithPrompts(this IMcpServerBuilder builder, Action<AttributePromptProvider> configure)
        {
            var provider = new AttributePromptProvider();
            configure(provider);
            return RegisterPromptHandlers(builder, provider);
        }

        private static IMcpServerBuilder RegisterPromptHandlers(IMcpServerBuilder builder, AttributePromptProvider provider)
        {
            var capability = provider.CreatePromptsCapability();
            
            builder.WithListPromptsHandler(capability.ListPromptsHandler!);
            builder.WithGetPromptHandler(capability.GetPromptHandler!);
            
            return builder;
        }
    }
} 