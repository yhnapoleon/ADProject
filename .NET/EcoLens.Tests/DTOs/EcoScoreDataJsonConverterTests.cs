using System.Text;
using System.Text.Json;
using EcoLens.Api.DTOs.OpenFoodFacts;
using Xunit;

namespace EcoLens.Tests.DTOs;

public class EcoScoreDataJsonConverterTests
{
	private static readonly JsonSerializerOptions Options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true
	};

	[Fact]
	public void Read_NullToken_ReturnsNull()
	{
		var json = "{\"ecoscore_data\": null}";
		var product = JsonSerializer.Deserialize<ProductDto>(json, Options);
		Assert.NotNull(product);
		Assert.Null(product.EcoScoreData);
	}

	[Fact]
	public void Read_StringToken_Empty_ReturnsNull()
	{
		var json = "{\"ecoscore_data\": \"\"}";
		var product = JsonSerializer.Deserialize<ProductDto>(json, Options);
		Assert.NotNull(product);
		Assert.Null(product.EcoScoreData);
	}

	[Fact]
	public void Read_StringToken_ValidJson_ReturnsDto()
	{
		// 直接驱动转换器：reader 位于 ecoscore_data 的字符串值上
		var json = "{\"ecoscore_data\":\"{\\\"agribalyse\\\":{\\\"co2_total\\\":2.5}}\"}";
		var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
		reader.Read(); // StartObject
		reader.Read(); // PropertyName
		reader.Read(); // String value (inner JSON)
		var converter = new EcoScoreDataJsonConverter();
		var result = converter.Read(ref reader, typeof(EcoScoreDataDto), Options);
		Assert.NotNull(result);
		Assert.NotNull(result.Agribalyse);
		Assert.Equal(2.5m, result.Agribalyse.Co2Total);
	}

	[Fact]
	public void Read_StringToken_InvalidJson_ReturnsNull()
	{
		var json = "{\"ecoscore_data\": \"not valid json {{{\"}";
		var product = JsonSerializer.Deserialize<ProductDto>(json, Options);
		Assert.NotNull(product);
		Assert.Null(product.EcoScoreData);
	}

	[Fact]
	public void Read_StartObject_DeserializesDirectly()
	{
		// 直接驱动转换器：reader 位于 ecoscore_data 的对象值（StartObject）上
		var json = "{\"ecoscore_data\":{\"agribalyse\":{\"co2_total\":3.0}}}";
		var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
		reader.Read(); // StartObject
		reader.Read(); // PropertyName "ecoscore_data"
		reader.Read(); // StartObject (value of ecoscore_data)
		var converter = new EcoScoreDataJsonConverter();
		var result = converter.Read(ref reader, typeof(EcoScoreDataDto), Options);
		Assert.NotNull(result);
		Assert.NotNull(result.Agribalyse);
		Assert.Equal(3.0m, result.Agribalyse.Co2Total);
	}

	[Fact]
	public void Write_NullValue_WritesNull()
	{
		var json = JsonSerializer.Serialize(new ProductDto { EcoScoreData = null }, Options);
		// With default options null may be omitted or written as null
		Assert.True(json.Contains("\"ecoscore_data\":null") || !json.Contains("ecoscore_data"));
	}

	[Fact]
	public void Write_NonNullValue_Serializes()
	{
		var dto = new ProductDto
		{
			EcoScoreData = new EcoScoreDataDto
			{
				Agribalyse = new AgribalyseDataDto { Co2Total = 1.5m }
			}
		};
		var json = JsonSerializer.Serialize(dto, Options);
		Assert.NotNull(json);
		Assert.Contains("1.5", json);
	}
}
