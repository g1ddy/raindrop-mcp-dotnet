using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using Mcp.Raindrops;
using ModelContextProtocol.Server;
using Xunit;

namespace Mcp.Tests;

public class MetadataAttributesTests
{
    // Framework-injected types that do not require description attributes
    private static readonly Type[] ExcludedParameterTypes =
    [
        typeof(CancellationToken),
        typeof(McpServer)
    ];

    [Fact]
    public void Tool_method_parameters_have_descriptions()
    {
        var toolTypes = typeof(RaindropsTools).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() != null);

        foreach (var type in toolTypes)
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() != null);
            foreach (var method in methods)
            {
                foreach (var param in method.GetParameters())
                {
                    if (ExcludedParameterTypes.Contains(param.ParameterType))
                        continue;

                    var desc = param.GetCustomAttribute<DescriptionAttribute>();
                    Assert.True(desc != null, $"Parameter '{param.Name}' in method '{method.Name}' of tool '{type.Name}' is missing [Description] attribute.");
                    Assert.True(!string.IsNullOrWhiteSpace(desc!.Description), $"Parameter '{param.Name}' in method '{method.Name}' of tool '{type.Name}' has empty description.");
                }
            }
        }
    }

    [Fact]
    public void Record_properties_have_descriptions()
    {
        var recordTypes = GetDescribedRecordTypes(typeof(Raindrop).Assembly);

        foreach (var type in recordTypes)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                var desc = prop.GetCustomAttribute<DescriptionAttribute>();
                Assert.True(desc != null, $"Property '{prop.Name}' on type '{type.Name}' is missing [Description] attribute.");
                Assert.True(!string.IsNullOrWhiteSpace(desc!.Description), $"Property '{prop.Name}' on type '{type.Name}' has empty description.");
            }
        }
    }

    private static IEnumerable<Type> GetDescribedRecordTypes(Assembly assembly)
        => assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericType
                        && t.Namespace?.Contains("Common") == false
                        && t.GetCustomAttribute<DescriptionAttribute>() != null);
}
