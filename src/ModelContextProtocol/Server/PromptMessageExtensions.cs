using ModelContextProtocol.Protocol.Types;
using System;
using System.Collections.Generic;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// Extension methods for working with prompt messages.
    /// </summary>
    public static class PromptMessageExtensions
    {
        /// <summary>
        /// Creates a user prompt message with text content.
        /// </summary>
        /// <param name="text">The text content.</param>
        /// <returns>A new prompt message.</returns>
        public static PromptMessage UserMessage(this string text)
        {
            return new PromptMessage
            {
                Role = Role.User,
                Content = new Content
                {
                    Type = "text",
                    Text = text
                }
            };
        }

        /// <summary>
        /// Creates an assistant prompt message with text content.
        /// </summary>
        /// <param name="text">The text content.</param>
        /// <returns>A new prompt message.</returns>
        public static PromptMessage AssistantMessage(this string text)
        {
            return new PromptMessage
            {
                Role = Role.Assistant,
                Content = new Content
                {
                    Type = "text",
                    Text = text
                }
            };
        }

        /// <summary>
        /// Creates a list of prompt messages from the given messages.
        /// </summary>
        /// <param name="messages">The messages to include.</param>
        /// <returns>A new list of prompt messages.</returns>
        public static List<PromptMessage> ToPromptMessageList(this PromptMessage[] messages)
        {
            return new List<PromptMessage>(messages);
        }

        /// <summary>
        /// Creates a GetPromptResult from the given messages.
        /// </summary>
        /// <param name="messages">The messages to include.</param>
        /// <returns>A new GetPromptResult.</returns>
        public static GetPromptResult ToPromptResult(this IEnumerable<PromptMessage> messages)
        {
            return new GetPromptResult
            {
                Messages = messages is List<PromptMessage> list ? list : new List<PromptMessage>(messages)
            };
        }

        /// <summary>
        /// Creates a GetPromptResult from the given messages.
        /// </summary>
        /// <param name="messages">The messages to include.</param>
        /// <returns>A new GetPromptResult.</returns>
        public static GetPromptResult ToPromptResult(this PromptMessage[] messages)
        {
            return new GetPromptResult
            {
                Messages = new List<PromptMessage>(messages)
            };
        }
    }
} 