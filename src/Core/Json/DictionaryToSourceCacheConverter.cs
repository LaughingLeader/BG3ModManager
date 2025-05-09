using DynamicData;

namespace ModManager.Json;

public interface IObjectWithId
{
	string Id { get; set; }
}

public class DictionaryToSourceCacheConverter<TValue> : JsonConverter<SourceCache<TValue, string>> where TValue : IObjectWithId
{
	private static readonly Type _type = typeof(SourceCache<TValue, string>);

	public override bool CanConvert(Type objectType) => _type.IsAssignableFrom(objectType);

	public override SourceCache<TValue, string>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.Null) return default;

		if (reader.TokenType == JsonTokenType.StartObject)
		{
			try
			{
				reader.Read();
				var entries = new List<TValue>();
				while (reader.TokenType == JsonTokenType.PropertyName)
				{
					if (reader.GetString() is string key)
					{
						reader.Read();
						if (reader.GetString() is string valStr && JsonSerializer.Deserialize<TValue>(valStr, options) is TValue dictValue)
						{
							dictValue.Id = key;
							entries.Add(dictValue);
						}
					}
					reader.Read();
				}
				var cache = new SourceCache<TValue, string>(x => x.Id);
				foreach (var entry in entries)
				{
					if (entry != null) cache.AddOrUpdate(entry);
				}

				return cache;
			}
			catch (Exception ex)
			{
				throw new Exception($"Error converting dictionary", ex);
			}
		}

		throw new Exception($"Unexpected token type ({reader.TokenType})");
	}

	public override void Write(Utf8JsonWriter writer, SourceCache<TValue, string>? cache, JsonSerializerOptions options)
	{
		if (cache == null)
		{
			writer.WriteNullValue();
		}
		else
		{
			writer.WriteStartObject();

			foreach (var entry in cache.Items)
			{
				writer.WritePropertyName(entry.Id);
				JsonSerializer.Serialize(writer, entry, options);
			}
			writer.WriteEndObject();
		}
	}
}
