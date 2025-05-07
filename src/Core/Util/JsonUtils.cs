using LSLib.LS;

using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace ModManager.Util;

public static class JsonUtils
{
	private static readonly JsonSerializerOptions _serializerSettings = new()
	{
		AllowTrailingCommas = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
	};

	public static T? Deserialize<T>(string text, JsonSerializerOptions? opts = null) => JsonSerializer.Deserialize<T?>(text, opts ?? _serializerSettings);

	public static T? DeserializeFromPath<T>(string path, JsonSerializerOptions? opts = null) => Deserialize<T?>(File.ReadAllText(path), opts ?? _serializerSettings);

	public static T? SafeDeserialize<T>(string text, JsonSerializerOptions? opts = null)
	{
		try
		{
			var result = JsonSerializer.Deserialize<T?>(text, opts ?? _serializerSettings);
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
				return SafeDeserialize<T?>(contents, opts ?? _serializerSettings);
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
		result = JsonSerializer.Deserialize<T?>(text, opts ?? _serializerSettings);
		return result != null;
	}

	public static bool TrySafeDeserializeFromPath<T>(string path, [NotNullWhen(true)] out T? result, JsonSerializerOptions? opts = null)
	{
		if (File.Exists(path))
		{
			var contents = File.ReadAllText(path);
			result = JsonSerializer.Deserialize<T?>(contents, opts ?? _serializerSettings);
			return result != null;
		}
		result = default;
		return false;
	}

	public static async Task<T?> DeserializeFromPathAsync<T>(string path, CancellationToken token, JsonSerializerOptions? opts = null)
	{
		try
		{
			var fileBytes = await FileUtils.LoadFileAsBytesAsync(path, token);
			if (fileBytes != null)
			{
				var contents = Encoding.UTF8.GetString(fileBytes);
				if (!string.IsNullOrEmpty(contents))
				{
					return JsonSerializer.Deserialize<T?>(contents, opts ?? _serializerSettings);
				}
			}
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
			if (!string.IsNullOrWhiteSpace(text))
			{
				return JsonSerializer.Deserialize<T?>(text, opts ?? _serializerSettings);
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
