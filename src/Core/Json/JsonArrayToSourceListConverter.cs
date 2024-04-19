using DynamicData;

namespace ModManager.Json;

public class JsonArrayToSourceListConverter<T> : JsonConverter<SourceList<T>> where T : class
{
	private static readonly Type _type = typeof(SourceList<T>);

	public override bool CanConvert(Type objectType) => _type.IsAssignableFrom(objectType);

	public override SourceList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray)
		{
			throw new JsonException($"Unexpected JSON token. Expected {JsonTokenType.StartArray} but read {reader.TokenType}");
		}

		SourceList<T> result = new();

		while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
		{
			var entry = JsonSerializer.Deserialize<T>(ref reader, options);
			if (entry != null)
			{
				result.Add(entry);
			}
		}
		return result;
	}

	public override void Write(Utf8JsonWriter writer, SourceList<T>? value, JsonSerializerOptions options)
	{
		if (value is null)
		{
			writer.WriteNullValue();
			return;
		}
		else
		{
			writer.WriteStartArray();

			foreach (var entry in value.Items)
			{
				JsonSerializer.Serialize(writer, entry, options);
			}

			writer.WriteEndArray();
		}
	}
}
