using System.Text.Json;
using System.Text.Json.Serialization;

namespace EcoLens.Api.DTOs.OpenFoodFacts;

/// <summary>
/// Open Food Facts 的 ecoscore_data 可能是对象，也可能是 JSON 字符串，本转换器两种都支持。
/// </summary>
public class EcoScoreDataJsonConverter : JsonConverter<EcoScoreDataDto?>
{
    public override EcoScoreDataDto? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
        if (reader.TokenType == JsonTokenType.String)
        {
            var json = reader.GetString();
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                return JsonSerializer.Deserialize<EcoScoreDataDto>(json, options);
            }
            catch
            {
                return null;
            }
        }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return (EcoScoreDataDto?)JsonSerializer.Deserialize(ref reader, typeof(EcoScoreDataDto), options);
        }
        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, EcoScoreDataDto? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            JsonSerializer.Serialize(writer, value, options);
    }
}
