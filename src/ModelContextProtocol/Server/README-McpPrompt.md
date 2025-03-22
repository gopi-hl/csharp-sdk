# Using the McpPrompt Attribute in Model Context Protocol

The `McpPrompt` attribute allows you to define prompts for your Model Context Protocol server in a clean, declarative way. This is similar to the `McpTool` attribute but for prompts.

## Basic Usage

```csharp
using ModelContextProtocol.Server;
using ModelContextProtocol.Protocol.Types;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

// Mark the class with DynamicallyAccessedMembers to preserve methods during trimming
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
public class MyPrompts
{
    // Simple prompt without arguments
    [McpPrompt("greeting", Description = "A friendly greeting prompt")]
    public Task<PromptMessage[]> GetGreetingPrompt()
    {
        return Task.FromResult(new[]
        {
            "Hello! How can I assist you today?".AssistantMessage()
        });
    }

    // Prompt with arguments
    [McpPrompt("summarize", Description = "Summarizes text")]
    public Task<GetPromptResult> GetSummaryPrompt(
        [McpPromptArgument(Description = "Text to summarize")] string text,
        [McpPromptArgument(Description = "Number of sentences")] int? sentences = 3)
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
}
```

## Registering Prompts with Your Server

There are several ways to register prompts with your server, with various compatibility considerations for AOT/trimming:

```csharp
// Option 1: Register by type (AOT compatible with proper annotations)
// Requires [DynamicallyAccessedMembers] on your prompt class
services.AddMcpServer(builder => 
{
    builder.WithPrompts(typeof(MyPrompts));
});

// Option 2: Register by instance (AOT compatible)
// The instance's type needs to have its methods preserved
var myPrompts = new MyPrompts();
services.AddMcpServer(builder => 
{
    builder.WithPrompts(myPrompts);
});

// Option 3: Register by assembly (NOT AOT compatible)
// This requires [RequiresUnreferencedCode] and will not work with trimming
// unless you exclude the entire assembly from trimming
services.AddMcpServer(builder => 
{
    builder.WithPrompts(typeof(MyPrompts).Assembly);
});

// Option 4: Register using the flexible configuration (AOT compatibility depends on usage)
services.AddMcpServer(builder => 
{
    builder.WithPrompts(provider => 
    {
        // Option 4a: Instance registration (AOT compatible)
        provider.RegisterInstance(new MyPrompts());
        
        // Option 4b: Type registration (AOT compatible with proper annotations)
        provider.RegisterType(typeof(MyPrompts));
        
        // Option 4c: Assembly registration (NOT AOT compatible)
        // provider.RegisterAssembly(typeof(MyPrompts).Assembly);
    });
});
```

## Prompt Method Requirements

- Must return either `Task<GetPromptResult>` or `Task<PromptMessage[]>`
- Can accept any number of parameters that will be exposed as prompt arguments
- Can optionally have a `CancellationToken` as the last parameter (not exposed as prompt argument)

## Argument Attributes

Use the `McpPromptArgumentAttribute` to describe prompt arguments:

```csharp
[McpPromptArgumentAttribute(Description = "The weather condition")]
public string WeatherCondition { get; set; }

// You can specify if an argument is required (defaults to true for value types)
[McpPromptArgumentAttribute(Description = "Temperature in Celsius", Required = false)]
public double? Temperature { get; set; }
```

## Trimming and AOT Considerations

When using trimming or AOT compilation, you need to make sure that the types and methods you use with reflection are preserved.

### Required Attributes

1. Add `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]` to your prompt classes:

```csharp
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
public class MyPrompts
{
    [McpPrompt("example")]
    public Task<PromptMessage[]> ExamplePrompt() { /* ... */ }
}
```

2. Avoid using assembly scanning with trimming/AOT:

```csharp
// NOT recommended with trimming/AOT:
builder.WithPrompts(typeof(MyPrompts).Assembly);

// Instead, use explicit type registration:
builder.WithPrompts(typeof(MyPrompts));
```

3. If you must use assembly scanning, exclude the assembly from trimming in your project file:

```xml
<ItemGroup>
  <TrimmerRootAssembly Include="YourAssembly" />
</ItemGroup>
```

## Helper Extension Methods

The SDK provides helper extension methods for creating prompt messages:

```csharp
// Create a user message
string.UserMessage("Your input here");

// Create an assistant message
string.AssistantMessage("The response here");

// Convert array to GetPromptResult
promptMessages.ToPromptResult();
``` 