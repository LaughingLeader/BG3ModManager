using ModManager.Models.Settings;
using ModManager.Util;

using System.ComponentModel;
using System.Reflection;

namespace ModManager;

public static class ModelExtensions
{
	public static void SetToDefault(this ReactiveObject model)
	{
		/*PropertyInfo[] props = model.GetType().GetProperties();
		foreach (PropertyInfo prop in props)
		{
			var d = prop.GetCustomAttribute<DefaultValueAttribute>();
			if (d != null && prop.GetValue(model) != d.Value)
			{
				prop.SetValue(model, d.Value);
			}
		}*/
		var props = TypeDescriptor.GetProperties(model.GetType());
		foreach (PropertyDescriptor pr in props)
		{
			if (pr.CanResetValue(model))
			{
				pr.ResetValue(model);
			}
		}
	}

	public static void SetFrom<T>(this T target, T from) where T : ReactiveObject
	{
		var props = TypeDescriptor.GetProperties(target.GetType());
		foreach (PropertyDescriptor pr in props)
		{
			var value = pr.GetValue(from);
			if (value != null)
			{
				pr.SetValue(target, value);
			}
		}
	}

	public static void SetFrom<T, T2>(this T target, T from) where T : ReactiveObject where T2 : Attribute
	{
		var attributeType = typeof(T2);
		var props = typeof(T).GetRuntimeProperties().Where(prop => Attribute.IsDefined(prop, attributeType)).ToList();
		foreach (var pr in props)
		{
			var value = pr.GetValue(from);
			if (value != null)
			{
				pr.SetValue(target, value);
			}
		}
	}

	public static readonly JsonSerializerOptions _defaultSerializerSettings = new()
	{
		AllowTrailingCommas = true,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public static bool Save<T>(this T data, out Exception? error) where T : ISerializableSettings
	{
		error = null;
		try
		{
			var directory = data.GetDirectory();
			var filePath = Path.Join(directory, data.FileName);
			Directory.CreateDirectory(directory);
			var contents = JsonSerializer.Serialize(data, data.GetType(), _defaultSerializerSettings);
			File.WriteAllText(filePath, contents);
			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error saving {data.FileName}:\n{ex}");
			error = ex;
		}
		return false;
	}

	public static bool Load<T>(this T data, out Exception? error, bool saveIfNotFound = true) where T : ISerializableSettings
	{
		error = null;
		try
		{
			var directory = data.GetDirectory();
			var filePath = Path.Join(directory, data.FileName);
			if (File.Exists(filePath))
			{
				var outputType = data.GetType();
				var text = File.ReadAllText(filePath);
				var settings = JsonSerializer.Deserialize(text, outputType, _defaultSerializerSettings);
				if (settings != null)
				{
					var props = TypeDescriptor.GetProperties(outputType);
					foreach (PropertyDescriptor pr in props)
					{
						var value = pr.GetValue(settings);
						if (value != null)
						{
							pr.SetValue(data, value);
						}
					}
					return true;
				}
			}
			else if (saveIfNotFound)
			{
				if (!Save(data, out var saveError))
				{
					error = saveError;
					return false;
				}
				return true;
			}
		}
		catch (Exception ex)
		{
			error = ex;
			DivinityApp.Log($"Error saving {data.FileName}:\n{ex}");
		}
		return false;
	}
}
