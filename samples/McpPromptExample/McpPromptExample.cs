using ModelContextProtocol.Configuration;
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Examples
{
    /// <summary>
    /// Custom prompt message to ensure proper serialization of role
    /// </summary>
    public class CustomPromptMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "assistant";

        [JsonPropertyName("content")]
        public Content Content { get; set; } = new Content();
    }

    /// <summary>
    /// Example showing how to use the McpPrompt attribute
    /// </summary>
    /// <remarks>
    /// When using AOT compilation or trimming, it's important to preserve the type and methods
    /// that contain McpPrompt attributes. Mark the class with the DynamicallyAccessedMembers attribute.
    /// </remarks>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public class PromptsExample
    {
        // Simple prompt without arguments
        [McpPrompt("greeting", Description = "A friendly greeting prompt")]
        public Task<PromptMessage[]> GetGreetingPrompt()
        {
            return Task.FromResult(new PromptMessage[]
            {
                new PromptMessage
                {
                    Role = Role.Assistant,
                    Content = new Content
                    {
                        Type = "text",
                        Text = "Hello! How can I assist you today?"
                    }
                }
            });
        }

        // Prompt with arguments
        [McpPrompt("summary", Description = "Summarizes text")]
        public Task<GetPromptResult> GetSummaryPrompt(
            [McpPromptArgument(Description = "Text to summarize")] string text,
            [McpPromptArgument(Description = "Number of sentences")] int? sentences = 3,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GetPromptResult
            {
                Messages = new List<PromptMessage>
                {
                    new PromptMessage
                    {
                        Role = Role.User,
                        Content = new Content
                        {
                            Type = "text",
                            Text = $"Please summarize the following text in {sentences} sentence(s):\n\n{text}"
                        }
                    }
                }
            });
        }

        // Chat prompt with dynamic template
        [McpPrompt("chatPrompt", Description = "A chat prompt with customizable style")]
        public Task<PromptMessage[]> GetChatPrompt(
            [McpPromptArgument(Description = "The chat topic")] string topic,
            [McpPromptArgument(Description = "Response style (friendly, formal, etc.)")] string style = "friendly",
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PromptMessage[]
            {
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new Content
                    {
                        Type = "text",
                        Text = $"I'd like to have a {style} conversation about {topic}."
                    }
                },
                new PromptMessage
                {
                    Role = Role.Assistant,
                    Content = new Content
                    {
                        Type = "text",
                        Text = $"I'd be happy to discuss {topic} with you! What would you like to know about it?"
                    }
                }
            });
        }
    }

    /// <summary>
    /// Example showing how to set up an MCP server with attribute-based prompts
    /// </summary>
    public class ServerExample
    {
        public void ConfigureServer()
        {
            // Get a server builder from dependency injection or create one with McpServerFactory
            IMcpServerBuilder builder = GetServerBuilder();

            // For AOT/trimming scenarios:
            // 1. Use WithPrompts(Type) with a type that has DynamicallyAccessedMembers
            var serviceProvider = builder
                .WithPrompts(typeof(PromptsExample)) // Type has [DynamicallyAccessedMembers]
                .Services.BuildServiceProvider();
            var server = serviceProvider.GetRequiredService<IMcpServer>();

            // 2. Create an instance and register it
            var promptsInstance = new PromptsExample();
            var serviceProvider2 = builder
                .WithPrompts(promptsInstance)
                .Services.BuildServiceProvider();
            var server2 = serviceProvider2.GetRequiredService<IMcpServer>();

            // 3. WARNING: Using assembly scanning with trimming requires 
            // marking your assembly not to be trimmed in your project file:
            // <ItemGroup>
            //   <TrimmerRootAssembly Include="YourAssembly" />
            // </ItemGroup>
            #if !ENABLE_AOT
            var serviceProvider3 = builder
                .WithPrompts(typeof(PromptsExample).Assembly) // Not AOT-compatible
                .Services.BuildServiceProvider();
            var server3 = serviceProvider3.GetRequiredService<IMcpServer>();
            #endif
            
            // 4. Use the flexible configuration approach
            var serviceProvider4 = builder
                .WithPrompts(provider =>
                {
                    provider.RegisterInstance(new PromptsExample());
                    // Add more instances or types as needed
                })
                .Services.BuildServiceProvider();
            var server4 = serviceProvider4.GetRequiredService<IMcpServer>();
        }

        private IMcpServerBuilder GetServerBuilder()
        {
            // In a real application, you would typically get this from dependency injection
            // or create one using the appropriate factory
            throw new NotImplementedException("Replace with actual server builder creation");
        }
    }
} 