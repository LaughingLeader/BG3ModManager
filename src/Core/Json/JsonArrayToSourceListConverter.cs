using DynamicData;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ModManager.Json;

public class JsonArrayToSourceListConverter<T> : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType.IsArray && objectType.GetElementType() == typeof(T);
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		T[] arr = null;
		if (reader.TokenType != JsonToken.Null)
		{
			if (reader.TokenType == JsonToken.StartArray)
			{
				var token = JToken.Load(reader);
				arr = token.ToObject<T[]>();
			}
		}

		if (arr != null)
		{
			SourceList<T> result = null;
			if (existingValue is SourceList<T> existingList)
			{
				result = existingList;
			}
			else
			{
				result = new SourceList<T>();
			}
			result.AddRange(arr);
			return result;
		}

		return null;
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
		}
		else if (value is SourceList<T> list)
		{
			writer.WriteStartArray();
			foreach (var entry in list.Items)
			{
				writer.WriteValue(entry);
			}
			writer.WriteEndArray();
		}
	}
}
