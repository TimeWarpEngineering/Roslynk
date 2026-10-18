using System.ComponentModel;
using System.Reflection;
using ModelContextProtocol.Server;
using Morris.Roslynk.Features.MultiQuery;

namespace Morris.Roslynk.Tests.Infrastructure.Operations;

/// <summary>
/// The operation classes carry the same contract the MCP methods publish: tool name, description,
/// parameter names, descriptions and defaults. Metadata still comes from the method attributes.
/// </summary>
public class OperationContractTests
{
	[Fact]
	public void WhenAToolIsPublished_ThenItsOperationMatchesTheMethodContract()
	{
		Assembly assembly = typeof(MultiQueryCatalog).Assembly;
		int tools = 0;

		foreach (Type toolType in assembly.GetTypes().Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null))
		{
			foreach (MethodInfo method in toolType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
			{
				McpServerToolAttribute? published = method.GetCustomAttribute<McpServerToolAttribute>();
				if (published is null)
					continue;

				tools++;
				string operationName = toolType.Name.EndsWith("Tool", StringComparison.Ordinal)
					? toolType.Name[..^4]
					: toolType.Name;
				Type operationType = assembly.GetType(toolType.Namespace + "." + operationName)
					?? throw new Xunit.Sdk.XunitException($"{toolType.Name} has no operation type '{operationName}'.");

				Type? query = operationType.GetNestedType("Query");
				Type? command = operationType.GetNestedType("Command");
				Type request = published.ReadOnly is false
					? command ?? throw new Xunit.Sdk.XunitException($"{published.Name} is a write and has no Command.")
					: query ?? throw new Xunit.Sdk.XunitException($"{published.Name} is a query and has no Query.");
				Assert.True(published.ReadOnly is false ? query is null : command is null, $"{published.Name} declares both Query and Command.");

				object operation = request.GetCustomAttributes().Single(attribute => attribute.GetType().Name == "OperationAttribute");
				Assert.Equal(published.Name, operation.GetType().GetProperty("Name")!.GetValue(operation));
				Assert.Equal(
					method.GetCustomAttribute<DescriptionAttribute>()?.Description,
					operation.GetType().GetProperty("Description")!.GetValue(operation));

				var parameters = method.GetParameters()
					.Where(parameter => parameter.ParameterType != typeof(CancellationToken))
					.ToDictionary(parameter => parameter.Name!, StringComparer.Ordinal);
				var properties = request
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.ToDictionary(property => property.Name, StringComparer.Ordinal);

				Assert.Equal(
					parameters.Keys.Order(StringComparer.Ordinal).ToArray(),
					properties.Keys.Order(StringComparer.Ordinal).ToArray());

				foreach ((string name, ParameterInfo parameter) in parameters)
				{
					PropertyInfo property = properties[name];
					Assert.Equal(parameter.ParameterType, property.PropertyType);
					Assert.Equal(
						parameter.GetCustomAttribute<DescriptionAttribute>()?.Description,
						property.GetCustomAttribute<DescriptionAttribute>()?.Description);
					DefaultValueAttribute? defaultValue = property.GetCustomAttribute<DefaultValueAttribute>();
					Assert.Equal(parameter.HasDefaultValue, defaultValue is not null);
					if (defaultValue is not null)
						Assert.Equal(parameter.DefaultValue, defaultValue.Value);
				}
			}
		}

		Assert.Equal(29, tools);
	}
}
