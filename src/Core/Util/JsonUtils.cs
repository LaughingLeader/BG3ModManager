using LSLib.LS;

using ModManager.Json;
using ModManager.Models.Mod;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization.Metadata;

namespace ModManager.Util;

/// <summary>
/// Source: https://github.com/zcsizmadia/ZCS.DataContractResolver
/// Added an extra check for DefaultValueAttribute, since this attribute is ignored in the regular resolver.
/// This is so we can have defaultly true booleans and so on not serialize (when normally only false is ignored).
/// </summary>
public class DataContractDefaultValueResolver : DefaultJsonTypeInfoResolver
{
	private static Lazy<DataContractDefaultValueResolver> s_defaultInstance = new(() => new DataContractDefaultValueResolver());

	public static DataContractDefaultValueResolver Default => s_defaultInstance.Value;

	private static bool IsNullOrDefault(object? obj)
	{
		if (obj is null)
		{
			return true;
		}

		Type type = obj.GetType();

		if (!type.IsValueType)
		{
			return false;
		}

		return Activator.CreateInstance(type)?.Equals(obj) == true;
	}

	private static bool ShouldSerialize(object _, object? obj) => !IsNullOrDefault(obj);

	private static IEnumerable<MemberInfo> EnumerateFieldsAndProperties(Type type, BindingFlags bindingFlags)
	{
		foreach (FieldInfo fieldInfo in type.GetFields(bindingFlags))
		{
			yield return fieldInfo;
		}

		foreach (PropertyInfo propertyInfo in type.GetProperties(bindingFlags))
		{
			yield return propertyInfo;
		}
	}

	private static IEnumerable<JsonPropertyInfo> CreateDataMembers(JsonTypeInfo jsonTypeInfo)
	{
		bool isDataContract = jsonTypeInfo.Type.GetCustomAttribute<DataContractAttribute>() != null;
		BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

		if (isDataContract)
		{
			bindingFlags |= BindingFlags.NonPublic;
		}

		foreach (MemberInfo memberInfo in EnumerateFieldsAndProperties(jsonTypeInfo.Type, bindingFlags))
		{
			if (memberInfo == null)
			{
				continue;
			}

			DefaultValueAttribute? defaultAttr = null;
			DataMemberAttribute? attr = null;
			if (isDataContract)
			{
				attr = memberInfo.GetCustomAttribute<DataMemberAttribute>();
				defaultAttr = memberInfo.GetCustomAttribute<DefaultValueAttribute>();
				if (attr == null)
				{
					continue;
				}
			}
			else
			{
				if (memberInfo.GetCustomAttribute<IgnoreDataMemberAttribute>() != null)
				{
					continue;
				}
			}

			Func<object, object?>? getValue = null;
			Action<object, object?>? setValue = null;
			Type? propertyType = null;
			string? propertyName = null;

			if (memberInfo.MemberType == MemberTypes.Field && memberInfo is FieldInfo fieldInfo)
			{
				propertyName = attr?.Name ?? fieldInfo.Name;
				propertyType = fieldInfo.FieldType;
				getValue = fieldInfo.GetValue;
				setValue = fieldInfo.SetValue;
			}
			else
			if (memberInfo.MemberType == MemberTypes.Property && memberInfo is PropertyInfo propertyInfo)
			{
				propertyName = attr?.Name ?? propertyInfo.Name;
				propertyType = propertyInfo.PropertyType;
				if (propertyInfo.CanRead)
				{
					getValue = propertyInfo.GetValue;
				}
				if (propertyInfo.CanWrite)
				{
					setValue = propertyInfo.SetValue;
				}
			}
			else
			{
				continue;
			}

			JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(propertyType, propertyName);
			if (jsonPropertyInfo == null)
			{
				continue;
			}

			jsonPropertyInfo.Get = getValue;
			jsonPropertyInfo.Set = setValue;

			if (attr != null)
			{
				jsonPropertyInfo.IsRequired = attr.IsRequired;
				jsonPropertyInfo.Order = attr.Order;

				if(defaultAttr != null)
				{
					//jsonPropertyInfo.ShouldSerialize = (_, obj) => obj != null && obj.Equals(defaultAttr.Value) == false;
					jsonPropertyInfo.ShouldSerialize = (_, obj) => obj?.Equals(defaultAttr.Value) == false;
					//jsonPropertyInfo.ShouldSerialize = (_, obj) =>
					//{
					//	DivinityApp.Log($"ShouldSerialize: {jsonPropertyInfo.Name}: {obj}");
					//	return false;
					//};
				}
				else
				{
					//jsonPropertyInfo.ShouldSerialize = !attr.EmitDefaultValue ? ((_, obj) => !IsNullOrDefault(obj)) : null;
					jsonPropertyInfo.ShouldSerialize = ShouldSerialize;
				}
			}

			if (!jsonPropertyInfo.IsRequired)
			{
				var requiredAttr = memberInfo.GetCustomAttribute<RequiredAttribute>();
				if (requiredAttr != null)
				{
					jsonPropertyInfo.IsRequired = true;
				}
			}

			yield return jsonPropertyInfo;
		}
	}

	public static JsonTypeInfo GetTypeInfo(JsonTypeInfo jsonTypeInfo)
	{
		if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object)
		{
			foreach (var jsonPropertyInfo in CreateDataMembers(jsonTypeInfo).OrderBy((x) => x.Order))
			{
				jsonTypeInfo.Properties.Add(jsonPropertyInfo);
			}
		}

		return jsonTypeInfo;
	}

	public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
	{
		JsonTypeInfo jsonTypeInfo = base.GetTypeInfo(type, options);

		if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
		{
			return jsonTypeInfo;
		}

		jsonTypeInfo.Properties.Clear();

		return GetTypeInfo(jsonTypeInfo);
	}
}

public static class JsonUtils
{
	private static readonly JsonSerializerOptions _defaultSerializerSettings = new()
	{
		AllowTrailingCommas = true,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
		TypeInfoResolver = DataContractDefaultValueResolver.Default,
		//TypeInfoResolver = ModManagerJsonContext.Default,
		PropertyNameCaseInsensitive = true,
	};

	public static JsonSerializerOptions DefaultSerializerSettings => _defaultSerializerSettings;

	static JsonUtils()
	{
		_defaultSerializerSettings.Converters.Add(new JsonStringEnumConverter());
		_defaultSerializerSettings.Converters.Add(new DictionaryToSourceCacheConverter<ModConfig>());
		_defaultSerializerSettings.Converters.Add(new JsonArrayToSourceListConverter<string>());
	}

	public static T? Deserialize<T>(string text, JsonSerializerOptions? opts = null) => JsonSerializer.Deserialize<T?>(text, opts ?? _defaultSerializerSettings);

	public static T? DeserializeFromPath<T>(string path, JsonSerializerOptions? opts = null) => Deserialize<T?>(File.ReadAllText(path), opts ?? _defaultSerializerSettings);

	public static T? SafeDeserialize<T>(string text, JsonSerializerOptions? opts = null)
	{
		try
		{
			var result = JsonSerializer.Deserialize<T?>(text, opts ?? _defaultSerializerSettings);
			if (result != null)
			{
				return result;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log("Error deserializing json:\n" + ex.ToString());
		}
		return default;
	}

	public static T? SafeDeserializeFromPath<T>(string path, JsonSerializerOptions? opts = null)
	{
		try
		{
			if (File.Exists(path))
			{
				var contents = File.ReadAllText(path);
				return SafeDeserialize<T?>(contents, opts ?? _defaultSerializerSettings);
			}
			else
			{
				DivinityApp.Log($"Error deserializing json: File '{path}' does not exist.");
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log("Error deserializing json:\n" + ex.ToString());
		}
		return default;
	}

	public static bool TrySafeDeserialize<T>(string text, [NotNullWhen(true)] out T? result, JsonSerializerOptions? opts = null)
	{
		result = JsonSerializer.Deserialize<T?>(text, opts ?? _defaultSerializerSettings);
		return result != null;
	}

	public static bool TrySafeDeserializeFromPath<T>(string path, [NotNullWhen(true)] out T? result, JsonSerializerOptions? opts = null)
	{
		if (File.Exists(path))
		{
			var contents = File.ReadAllText(path);
			result = JsonSerializer.Deserialize<T?>(contents, opts ?? _defaultSerializerSettings);
			return result != null;
		}
		result = default;
		return false;
	}

	public static async Task<T?> DeserializeFromPathAsync<T>(string path, CancellationToken token, JsonSerializerOptions? opts = null)
	{
		try
		{
			await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
			var result = await JsonSerializer.DeserializeAsync<T?>(stream, opts ?? _defaultSerializerSettings, token);
			return result;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error deserializing '{path}':\n{ex}");
		}
		return default;
	}

	public static async Task<T?> DeserializeFromAbstractAsync<T>(Stream stream, CancellationToken token, JsonSerializerOptions? opts = null)
	{
		try
		{
			using var sr = new StreamReader(stream, Encoding.UTF8);
			var text = await sr.ReadToEndAsync(token);
			if (text.IsValid())
			{
				return JsonSerializer.Deserialize<T?>(text, opts ?? _defaultSerializerSettings);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error deserializing Stream:\n{ex}");
		}
		return default;
	}

	public static async Task<T?> DeserializeFromAbstractAsync<T>(PackagedFileInfo file, CancellationToken token, JsonSerializerOptions? opts = null)
	{
		try
		{
			using var stream = file.CreateContentReader();
			return await DeserializeFromAbstractAsync<T?>(stream, token, opts);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error deserializing AbstractFileInfo:\n{ex}");
		}
		return default;
	}
}
