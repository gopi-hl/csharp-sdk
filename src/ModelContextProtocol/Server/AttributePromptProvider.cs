using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ModelContextProtocol.Server
{
    /// <summary>
    /// A helper class that scans assemblies for methods decorated with <see cref="McpPromptAttribute"/>
    /// and registers them as MCP prompts.
    /// </summary>
    public class AttributePromptProvider
    {
        private readonly Dictionary<string, PromptHandler> _handlers = new Dictionary<string, PromptHandler>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Prompt> _prompts = new Dictionary<string, Prompt>(StringComparer.OrdinalIgnoreCase);

        private class PromptHandler
        {
            public MethodInfo Method { get; init; } = null!;
            public object? Instance { get; init; }
            public ParameterInfo[] Parameters { get; init; } = null!;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributePromptProvider"/> class.
        /// </summary>
        public AttributePromptProvider()
        {
        }

        /// <summary>
        /// Registers all prompt methods in the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly to scan for prompt methods.</param>
        /// <returns>This provider instance for chaining.</returns>
        [RequiresUnreferencedCode("Scans types in the assembly for prompt methods using reflection")]
        public AttributePromptProvider RegisterAssembly(Assembly assembly)
        {
            Throw.IfNull(assembly);

            foreach (var type in assembly.GetTypes())
            {
                RegisterType(type);
            }

            return this;
        }

        /// <summary>
        /// Registers all prompt methods in the given type.
        /// </summary>
        /// <param name="type">The type to scan for prompt methods.</param>
        /// <returns>This provider instance for chaining.</returns>
        public AttributePromptProvider RegisterType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
            Throw.IfNull(type);

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                var promptAttr = method.GetCustomAttribute<McpPromptAttribute>();
                if (promptAttr != null)
                {
                    RegisterMethod(type, method, promptAttr);
                }
            }

            return this;
        }

        /// <summary>
        /// Registers an instance of a class containing prompt methods.
        /// </summary>
        /// <param name="instance">The instance to scan for prompt methods. The type should be preserved at compilation.</param>
        /// <returns>This provider instance for chaining.</returns>
        public AttributePromptProvider RegisterInstance(object instance)
        {
            Throw.IfNull(instance);

            // When scanning an instance, we need to ensure its type is preserved
            var type = instance.GetType();
            
            // We manually need to ensure that public methods on this type are preserved
            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var promptAttr = method.GetCustomAttribute<McpPromptAttribute>();
                if (promptAttr != null)
                {
                    RegisterMethod(type, method, promptAttr, instance);
                }
            }

            return this;
        }

        private void RegisterMethod(Type type, MethodInfo method, McpPromptAttribute promptAttr, object? instance = null)
        {
            // For instance methods, we need an instance
            if (!method.IsStatic && instance == null)
            {
                throw new InvalidOperationException($"Cannot register instance method '{method.Name}' on type '{type.FullName}' without an instance.");
            }

            // Check if a prompt with the same name already exists
            if (_prompts.ContainsKey(promptAttr.Name))
            {
                throw new InvalidOperationException($"A prompt with the name '{promptAttr.Name}' is already registered.");
            }

            // Verify return type is Task<GetPromptResult> or Task<PromptMessage[]>
            if (!IsValidReturnType(method.ReturnType))
            {
                throw new InvalidOperationException(
                    $"Method '{method.Name}' on type '{type.FullName}' has an invalid return type. " +
                    $"Expected Task<GetPromptResult> or Task<PromptMessage[]>.");
            }

            // Create PromptArgument list from method parameters
            var arguments = new List<PromptArgument>();
            var parameters = method.GetParameters();

            // Last parameter might be a CancellationToken which we don't expose as a prompt argument
            var lastParam = parameters.Length > 0 ? parameters[parameters.Length - 1] : null;
            bool hasCanellationToken = lastParam != null && lastParam.ParameterType == typeof(CancellationToken);

            // Process parameters, skipping CancellationToken if present
            for (int i = 0; i < parameters.Length; i++)
            {
                // Skip CancellationToken parameter
                if (hasCanellationToken && i == parameters.Length - 1)
                {
                    continue;
                }

                var param = parameters[i];
                var argAttr = param.GetCustomAttribute<McpPromptArgumentAttribute>();

                var promptArg = new PromptArgument
                {
                    Name = param.Name ?? $"arg{i}",
                    Description = argAttr?.Description,
                    Required = argAttr?.Required ?? IsRequiredParameterType(param)
                };

                arguments.Add(promptArg);
            }

            // Create the prompt
            var prompt = new Prompt
            {
                Name = promptAttr.Name,
                Description = promptAttr.Description,
                Arguments = arguments.Count > 0 ? arguments : null
            };

            // Store the prompt and handler
            _prompts.Add(promptAttr.Name, prompt);
            _handlers.Add(promptAttr.Name, new PromptHandler
            {
                Method = method,
                Instance = instance,
                Parameters = parameters
            });
        }

        private static bool IsValidReturnType(Type returnType)
        {
            if (!returnType.IsGenericType || returnType.GetGenericTypeDefinition() != typeof(Task<>))
            {
                return false;
            }

            var resultType = returnType.GetGenericArguments()[0];
            return resultType == typeof(GetPromptResult) || 
                   (resultType.IsArray && resultType.GetElementType() == typeof(PromptMessage));
        }

        private static bool IsRequiredParameterType(ParameterInfo parameter)
        {
            // Value types are always required
            if (parameter.ParameterType.IsValueType)
            {
                return true;
            }

            // Check for Nullable<T>
            if (parameter.ParameterType.IsGenericType && 
                parameter.ParameterType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return false;
            }

            // For reference types, check if they have a default value
            return !parameter.HasDefaultValue;
        }

        /// <summary>
        /// Creates a PromptsCapability handler that uses the registered prompt methods.
        /// </summary>
        /// <returns>A configured PromptsCapability instance.</returns>
        public PromptsCapability CreatePromptsCapability()
        {
            return new PromptsCapability
            {
                ListPromptsHandler = HandleListPrompts,
                GetPromptHandler = HandleGetPrompt
            };
        }

        private Task<ListPromptsResult> HandleListPrompts(RequestContext<ListPromptsRequestParams> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new ListPromptsResult
            {
                Prompts = _prompts.Values.ToList()
            });
        }

        private async Task<GetPromptResult> HandleGetPrompt(RequestContext<GetPromptRequestParams> request, CancellationToken cancellationToken)
        {
            Throw.IfNull(request);
            Throw.IfNull(request.Params);
            
            var promptName = request.Params.Name;
            if (string.IsNullOrEmpty(promptName))
            {
                throw new McpServerException("Prompt name cannot be null or empty");
            }
            
            if (!_handlers.TryGetValue(promptName, out var handler))
            {
                throw new McpServerException($"Unknown prompt: {promptName}");
            }

            var arguments = request.Params.Arguments;
            var parameters = handler.Parameters;
            var args = new object?[parameters.Length];

            // Fill in the arguments
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                // If this is a CancellationToken parameter, pass the cancellation token
                if (param.ParameterType == typeof(CancellationToken))
                {
                    args[i] = cancellationToken;
                    continue;
                }

                // Try to get the argument value
                if (arguments != null && arguments.TryGetValue(param.Name!, out var value))
                {
                    // Convert the value to the parameter type
                    args[i] = ConvertArgument(value, param.ParameterType);
                }
                else if (param.HasDefaultValue)
                {
                    // Use the default value
                    args[i] = param.DefaultValue;
                }
                else
                {
                    // Missing required argument
                    throw new McpServerException($"Missing required argument: {param.Name}");
                }
            }

            // Invoke the handler method
            try
            {
                var result = await InvokePromptHandler(handler, args);
                return result;
            }
            catch (Exception ex) when (ex is not McpServerException)
            {
                throw new McpServerException($"Error executing prompt handler: {ex.Message}", ex);
            }
        }

        private static async Task<GetPromptResult> InvokePromptHandler(PromptHandler handler, object?[] args)
        {
            // Invoke the handler method
            var task = (Task)handler.Method.Invoke(handler.Instance, args)!;
            await task.ConfigureAwait(false);
            
            // Convert result to GetPromptResult
            var resultType = handler.Method.ReturnType.GetGenericArguments()[0];
            var resultProperty = task.GetType().GetProperty("Result");
            var result = resultProperty?.GetValue(task);
            
            if (resultType == typeof(GetPromptResult))
            {
                return (GetPromptResult)result!;
            }
            else
            {
                // Assuming this is PromptMessage[]
                var messages = (PromptMessage[])result!;
                return new GetPromptResult { Messages = messages.ToList() };
            }
        }

        private static object? ConvertArgument(object value, Type targetType)
        {
            // Handle simple case where type already matches
            if (value.GetType() == targetType || targetType.IsAssignableFrom(value.GetType()))
            {
                return value;
            }

            // Handle string to value type conversion
            if (value is string stringValue)
            {
                if (targetType == typeof(int) || targetType == typeof(int?))
                {
                    return int.Parse(stringValue);
                }
                if (targetType == typeof(double) || targetType == typeof(double?))
                {
                    return double.Parse(stringValue);
                }
                if (targetType == typeof(bool) || targetType == typeof(bool?))
                {
                    return bool.Parse(stringValue);
                }
                // Add other type conversions as needed
            }

            // For more complex conversions, you might want to use a JSON deserializer
            // or other conversion strategy

            return System.Convert.ChangeType(value, targetType);
        }
    }
} 