extern alias app;

using Microsoft.Extensions.Configuration;
using Morris.Roslynk.Infrastructure.Lifecycle;
using Morris.Roslynk.Infrastructure.Outlines;
using RoslynkSettings = app::Morris.Roslynk.Cli.Hosting.RoslynkSettings;

namespace Morris.Roslynk.Cli.Tests;

/// <summary>
/// The one typed settings object the engine starts from (mrpmorris/Roslynk#142): absent keys stay null and
/// the Effective members own every default; and the shipped example (mrpmorris/Roslynk#143) changes nothing
/// when copied as it is.
/// </summary>
public class RoslynkSettingsTests
{
	private static RoslynkSettings Bind(params (string Key, string Value)[] values)
	{
		Dictionary<string, string?> collection = new();
		foreach ((string key, string value) in values)
			collection[$"Roslynk:{key}"] = value;

		return RoslynkSettings.FromConfiguration(new ConfigurationBuilder().AddInMemoryCollection(collection).Build());
	}

	[Fact]
	public void WhenNothingIsConfigured_ThenEveryValueIsNull()
	{
		RoslynkSettings settings = RoslynkSettings.FromConfiguration(new ConfigurationBuilder().Build());

		Assert.Null(settings.MaxResponseChars);
		Assert.Null(settings.LoadWaitSeconds);
		Assert.Null(settings.IdleEvictionMinutes);
		Assert.Null(settings.EvictionCheckMinutes);
	}

	[Fact]
	public void WhenMaxResponseCharsIsAbsent_ThenTheDefaultBudgetApplies()
	{
		Assert.Equal(ResponseBudget.DefaultMaxChars, Bind().EffectiveMaxChars);
	}

	[Theory]
	[InlineData("0", 8_000)]
	[InlineData("500", 8_000)]
	[InlineData("200000", 200_000)]
	public void WhenMaxResponseCharsIsConfigured_ThenItIsUsedAboveTheMinimum(string configured, int expected)
	{
		Assert.Equal(expected, Bind(("MaxResponseChars", configured)).EffectiveMaxChars);
	}

	[Fact]
	public void WhenLoadWaitSecondsIsAbsent_ThenThirtySecondsApplies()
	{
		Assert.Equal(TimeSpan.FromSeconds(30), Bind().EffectiveLoadWaitSeconds);
		Assert.Equal(LoadWaitTimeout.Default.Duration, Bind().EffectiveLoadWaitTimeout.Duration);
	}

	[Theory]
	[InlineData("0", 0)]
	[InlineData("-5", 0)]
	[InlineData("12.5", 12.5)]
	public void WhenLoadWaitSecondsIsConfigured_ThenItIsUsedAndNegativeClampsToZero(string configured, double expectedSeconds)
	{
		Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), Bind(("LoadWaitSeconds", configured)).EffectiveLoadWaitTimeout.Duration);
	}

	[Fact]
	public void WhenIdleEvictionMinutesIsAbsent_ThenThirtyMinutesApplies()
	{
		Assert.Equal(TimeSpan.FromMinutes(30), Bind().EffectiveIdleEvictionTimeout);
	}

	[Fact]
	public void WhenIdleEvictionMinutesIsZero_ThenEvictionIsDisabled()
	{
		Assert.True(Bind(("IdleEvictionMinutes", "0")).EffectiveIdleEvictionTimeout <= TimeSpan.Zero);
	}

	[Fact]
	public void WhenEvictionCheckMinutesIsAbsentOrBelowTheFloor_ThenTheDefaultOrTheFloorApplies()
	{
		Assert.Equal(TimeSpan.FromMinutes(5), Bind().EffectiveEvictionCheckInterval);
		Assert.Equal(TimeSpan.FromMinutes(1), Bind(("EvictionCheckMinutes", "0")).EffectiveEvictionCheckInterval);
	}

	[Fact]
	public void WhenTheShippedExampleIsCopiedAsAppSettings_ThenTheCodeDefaultsApply()
	{
		string example = Path.Combine(AppContext.BaseDirectory, "appsettings.json.example");
		Assert.True(File.Exists(example), $"Expected the example beside roslynk at '{example}'.");

		string directory = Path.Combine(Path.GetTempPath(), "roslynk-tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		try
		{
			File.Copy(example, Path.Combine(directory, "appsettings.json"));
			IConfiguration configuration = new ConfigurationBuilder()
				.SetBasePath(directory)
				.AddJsonFile("appsettings.json", optional: false)
				.Build();

			// Every key is commented out, so the section is empty and the code defaults apply.
			Assert.False(configuration.GetSection(RoslynkSettings.SectionName).Exists());
			RoslynkSettings settings = RoslynkSettings.FromConfiguration(configuration);
			Assert.Equal(ResponseBudget.DefaultMaxChars, settings.EffectiveMaxChars);
			Assert.Equal(LoadWaitTimeout.Default.Duration, settings.EffectiveLoadWaitTimeout.Duration);
			Assert.Equal(TimeSpan.FromMinutes(RoslynkSettings.DefaultIdleEvictionMinutes), settings.EffectiveIdleEvictionTimeout);
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}
