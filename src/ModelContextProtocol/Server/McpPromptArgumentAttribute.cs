using System;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// Describes an argument for an MCP prompt.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class McpPromptArgumentAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the description of the argument.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets whether this argument is required.
        /// </summary>
        /// <remarks>
        /// If not set, the requirement is inferred from the parameter type.
        /// Value types (int, bool, etc.) and non-nullable reference types are considered required by default.
        /// </remarks>
        public bool? Required { get; set; }
    }
} 