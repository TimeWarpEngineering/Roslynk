using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace Morris.Roslynk.Tests.Server;

public class ToolCompositionTests
{
	[Fact]
	public void WhenTheEngineIsComposedAsTheServedAppDoes_ThenEveryToolResolves()
	{
		// The roslynk app registers AddRoslynk plus one singleton per tool type and resolves the tool that owns
		// each operation's handler. The tools that drive fixes take the analyzer-aware diagnostics provider, so a
		// missing registration would only show up when a tool is first invoked.
		Type[] tools = typeof(ServicesRegistration).Assembly.GetTypes()
			.Where(type => type.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
			.ToArray();
		var services = new ServiceCollection();
		services.AddRoslynk();
		foreach (Type tool in tools)
			services.AddSingleton(tool);
		using ServiceProvider provider = services.BuildServiceProvider();

		Assert.Equal(29, tools.Length);
		foreach (Type tool in tools)
			Assert.NotNull(provider.GetRequiredService(tool));
	}
}
