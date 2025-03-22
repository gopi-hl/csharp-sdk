using System;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// Marks a method as an MCP prompt that can be listed and retrieved via the prompts/list and prompts/get methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class McpPromptAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the prompt.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description of the prompt.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="McpPromptAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the prompt. Must be unique within the server.</param>
        public McpPromptAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
} 