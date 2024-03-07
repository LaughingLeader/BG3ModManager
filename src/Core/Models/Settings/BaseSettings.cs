﻿using DivinityModManager.Util;

using Newtonsoft.Json;

using ReactiveUI;

using System.ComponentModel;
using System.IO;

namespace DivinityModManager.Models.Settings
{
	public interface ISerializableSettings
	{
		string FileName { get; }
		string GetDirectory();
		bool Save(out Exception error);
		bool Load(out Exception error, bool saveIfNotFound = true);
	}

	public abstract class BaseSettings<T> : ReactiveObject where T : ISerializableSettings
	{
		[JsonIgnore] public string FileName { get; }

		public virtual string GetDirectory() => DivinityApp.GetAppDirectory("Data");

		private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
		{
			Formatting = Formatting.Indented,
			NullValueHandling = NullValueHandling.Ignore
		};

		public BaseSettings(string fileName)
		{
			FileName = fileName;
		}

		public virtual bool Save(out Exception error)
		{
			error = null;
			try
			{
				var directory = GetDirectory();
				var filePath = Path.Combine(directory, FileName);
				Directory.CreateDirectory(directory);
				var contents = JsonConvert.SerializeObject(this, _jsonSerializerSettings);
				File.WriteAllText(filePath, contents);
				return true;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error saving {FileName}:\n{ex}");
				error = ex;
			}
			return false;
		}

		public virtual bool Load(out Exception error, bool saveIfNotFound = true)
		{
			error = null;
			try
			{
				var directory = GetDirectory();
				var filePath = Path.Combine(directory, FileName);
				if (File.Exists(filePath))
				{
					if (DivinityJsonUtils.TrySafeDeserializeFromPath<T>(filePath, out var settings))
					{
						var props = TypeDescriptor.GetProperties(settings.GetType());
						foreach (PropertyDescriptor pr in props)
						{
							var value = pr.GetValue(settings);
							if (value != null)
							{
								pr.SetValue(this, value);
							}
						}
						return true;
					}
				}
				else if (saveIfNotFound)
				{
					if (!Save(out var saveError))
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
				DivinityApp.Log($"Error saving {FileName}:\n{ex}");
			}
			return false;
		}
	}
}
