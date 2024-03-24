using LSLib.LS;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.IO;
using System.Text;

namespace DivinityModManager.Util;

public static class DivinityJsonUtils
{
	private static readonly JsonSerializerSettings _errorHandleSettings = new()
	{
		Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
		{
			DivinityApp.Log(args.ErrorContext.Error.Message);
			args.ErrorContext.Handled = true;
		}
	};

	public static T GetValue<T>(this JToken jToken, string key, T defaultValue = default)
	{
		dynamic ret = jToken[key];
		if (ret == null) return defaultValue;
		if (ret is JObject) return JsonConvert.DeserializeObject<T>(ret.ToString());
		return (T)ret;
	}

	public static T SafeDeserialize<T>(string text)
	{
		var result = JsonConvert.DeserializeObject<T>(text, _errorHandleSettings);
		if (result != null)
		{
			return result;
		}
		return default;
	}

	public static T SafeDeserializeFromPath<T>(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				string contents = File.ReadAllText(path);
				return SafeDeserialize<T>(contents);
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

	public static bool TrySafeDeserialize<T>(string text, out T result)
	{
		result = JsonConvert.DeserializeObject<T>(text, _errorHandleSettings);
		return result != null;
	}

	public static bool TrySafeDeserializeFromPath<T>(string path, out T result)
	{
		if (File.Exists(path))
		{
			string contents = File.ReadAllText(path);
			result = JsonConvert.DeserializeObject<T>(contents, _errorHandleSettings);
			return result != null;
		}
		result = default;
		return false;
	}

	public static async Task<T> DeserializeFromPathAsync<T>(string path, CancellationToken token)
	{
		try
		{
			var fileBytes = await FileUtils.LoadFileAsBytesAsync(path, token);
			if (fileBytes != null)
			{
				var contents = Encoding.UTF8.GetString(fileBytes);
				if (!String.IsNullOrEmpty(contents))
				{
					return JsonConvert.DeserializeObject<T>(contents, _errorHandleSettings);
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error deserializing '{path}':\n{ex}");
		}
		return default;
	}

	public static async Task<T> DeserializeFromAbstractAsync<T>(Stream stream, CancellationToken token)
	{
		try
		{
			using var sr = new StreamReader(stream, Encoding.UTF8);
			string text = await sr.ReadToEndAsync(token);
			if (!String.IsNullOrWhiteSpace(text))
			{
				return JsonConvert.DeserializeObject<T>(text, _errorHandleSettings);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error deserializing Stream:\n{ex}");
		}
		return default;
	}

	public static async Task<T> DeserializeFromAbstractAsync<T>(PackagedFileInfo file, CancellationToken token)
	{
		try
		{
			using var stream = file.CreateContentReader();
			return await DeserializeFromAbstractAsync<T>(stream, token);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error deserializing AbstractFileInfo:\n{ex}");
		}
		return default;
	}
}
