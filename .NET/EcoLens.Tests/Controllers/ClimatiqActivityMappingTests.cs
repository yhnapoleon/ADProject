using EcoLens.Api.Services;
using Xunit;

namespace EcoLens.Tests;

public class ClimatiqActivityMappingTests
{
    [Fact]
    public void GetCo2MultiplierForFood_ShouldReturnDefault_WhenTagsNullOrEmpty()
    {
        Assert.Equal(1.0m, ClimatiqActivityMapping.GetCo2MultiplierForFood(null));
        Assert.Equal(1.0m, ClimatiqActivityMapping.GetCo2MultiplierForFood(System.Array.Empty<string>()));
    }

    [Fact]
    public void GetCo2MultiplierForFood_ShouldDetectBeefAsHighEmission()
    {
        var tags = new[] { "en:beef", "en:red-meat" };

        var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(tags);

        Assert.Equal(7.3m, multiplier);
    }

    [Fact]
    public void GetCo2MultiplierForFood_ShouldDetectFishCategory()
    {
        var tags = new[] { "en:seafood", "en:fish" };

        var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(tags);

        Assert.Equal(1.1m, multiplier);
    }

    [Fact]
    public void GetCo2MultiplierForFood_ShouldDetectDairyCategory()
    {
        var tags = new[] { "en:dairy-products", "en:milk" };

        var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(tags);

        Assert.Equal(0.8m, multiplier);
    }

    [Fact]
    public void GetCo2MultiplierForFood_ShouldDetectChocolateAsVeryHigh()
    {
        var tags = new[] { "en:chocolate", "en:snacks" };

        var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(tags);

        Assert.Equal(5.1m, multiplier);
    }

    [Fact]
    public void GetCo2MultiplierForFood_ShouldDetectBeverages()
    {
        var tags = new[] { "en:beverages", "en:carbonated-drinks" };

        var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(tags);

        Assert.Equal(0.3m, multiplier);
    }

    [Fact]
    public void GetCo2MultiplierForFood_ShouldDetectBreadAndGrains()
    {
        var tags = new[] { "en:bread", "en:cereals" };

        var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(tags);

        Assert.Equal(0.4m, multiplier);
    }

    [Fact]
    public void GetCo2MultiplierForFood_ShouldDetectFruitAndVegetable()
    {
        var tags = new[] { "en:fruit-and-vegetables", "en:fresh-food" };

        var multiplier = ClimatiqActivityMapping.GetCo2MultiplierForFood(tags);

        Assert.Equal(0.2m, multiplier);
    }
}

