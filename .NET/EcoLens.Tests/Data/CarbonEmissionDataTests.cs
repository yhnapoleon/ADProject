using EcoLens.Api.Data;
using Xunit;

namespace EcoLens.Tests.Data;

public class CarbonEmissionDataTests
{
	[Fact]
	public void GetFactor_Returns1_WhenLabelIsNull()
	{
		Assert.Equal(1.0, CarbonEmissionData.GetFactor(null!));
	}

	[Fact]
	public void GetFactor_Returns1_WhenLabelIsEmpty()
	{
		Assert.Equal(1.0, CarbonEmissionData.GetFactor(""));
	}

	[Fact]
	public void GetFactor_Returns1_WhenLabelIsWhitespace()
	{
		Assert.Equal(1.0, CarbonEmissionData.GetFactor("   "));
	}

	[Fact]
	public void GetFactor_ReturnsExactMatch_WhenKeyExists()
	{
		Assert.Equal(1.10, CarbonEmissionData.GetFactor("Hainanese Chicken Rice"));
		Assert.Equal(0.90, CarbonEmissionData.GetFactor("Fried Rice"));
		Assert.Equal(0.4, CarbonEmissionData.GetFactor("Bubble Tea"));
	}

	[Fact]
	public void GetFactor_ReturnsCaseInsensitiveMatch_WhenNoExactMatch()
	{
		// Exact is "Fried Rice" -> 0.90; case-insensitive should find it
		Assert.Equal(0.90, CarbonEmissionData.GetFactor("fried rice"));
		Assert.Equal(0.90, CarbonEmissionData.GetFactor("FRIED RICE"));
	}

	[Fact]
	public void GetFactor_Returns1_WhenLabelNotFound()
	{
		Assert.Equal(1.0, CarbonEmissionData.GetFactor("NonExistentLabel123"));
	}
}
