using DynamicData;

using Newtonsoft.Json;

namespace ModManager.Json;

public interface IObjectWithId
{
	string Id { get; set; }
}

public class DictionaryToSourceCacheConverter<TValue> : JsonConverter where TValue : IObjectWithId
{
	private static readonly Type _type = typeof(SourceCache<TValue, string>);

	public override bool CanConvert(Type objectType) => _type.IsAssignableFrom(objectType);

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null) return null;

		if (reader.TokenType == JsonToken.StartObject)
		{
			try
			{
				reader.Read();
				var entries = new List<TValue>();
				while (reader.TokenType == JsonToken.PropertyName)
				{
					var key = reader.Value as string;
					reader.Read();
					var val = serializer.Deserialize<TValue>(reader);
					val.Id = key;
					reader.Read();
					entries.Add(val);
				}
				SourceCache<TValue, string> cache = null;
				if (existingValue is SourceCache<TValue, string> existingCache)
				{
					cache = existingCache;
				}
				else
				{
					cache = new SourceCache<TValue, string>(x => x.Id);
				}

				foreach (var entry in entries)
				{
					cache.AddOrUpdate(entry);
				}

				return cache;
			}
			catch (Exception ex)
			{
				throw new Exception($"Error converting dictionary: {reader.Value}", ex);
			}
		}

		throw new Exception($"Unexpected token({reader.TokenType}) or value({reader.Value}");
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
		}
		else if (value is SourceCache<TValue, string> cache)
		{
			writer.WriteStartObject();

			foreach (var entry in cache.Items)
			{
				writer.WritePropertyName(entry.Id);
				serializer.Serialize(writer, entry);
			}
			writer.WriteEndObject();
		}
	}
}
