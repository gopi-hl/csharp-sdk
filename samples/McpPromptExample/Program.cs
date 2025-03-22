using ModelContextProtocol.Server;
using System.ComponentModel;
using Examples;

var builder = Host.CreateEmptyApplicationBuilder(settings: null);
builder.Services
    // 👇🏼 We build an MCP Server   
    .AddMcpServer()
    // 👇🏼 uses Stdio as transport protocol    
    .WithStdioServerTransport()
    // 👇🏼 Register an instance of PromptsExample for MCPPrompts
    .WithPrompts(new PromptsExample())
    // 👇🏼 Register all tools with McpToolType attribute    
    .WithTools();
await builder.Build().RunAsync();

// 👇🏼 Mark our type as a container for MCP tools
[McpToolType]
public static class TimeTool
{    
    // 👇🏼 Mark a method as an MCP tools
    [McpTool, Description("Get the current time for a city")]
    public static string GetCurrentTime(string city) =>
        $"It is {DateTime.Now.Hour}:{DateTime.Now.Minute} in {city}.";
}