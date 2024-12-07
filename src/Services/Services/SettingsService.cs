﻿using ModManager.Models;
using ModManager.Models.App;
using ModManager.Models.Mod;
using ModManager.Models.Settings;
using ModManager.Util;

using ReactiveUI;

using System.IO;

namespace ModManager.Services;

public class SettingsService : ReactiveObject, ISettingsService
{
	public AppSettings AppSettings { get; private set; }
	public ModManagerSettings ManagerSettings { get; private set; }
	public UserModConfig ModConfig { get; private set; }

	private readonly List<ISerializableSettings> _loadSettings;
	private readonly List<ISerializableSettings> _saveSettings;

	public bool TryLoadAppSettings(out Exception error)
	{
		error = null;
		try
		{
			LoadAppSettings();
			return true;
		}
		catch (Exception ex)
		{
			error = ex;
		}
		return false;
	}

	private void LoadAppSettings()
	{
		var resourcesFolder = DivinityApp.GetAppDirectory(DivinityApp.PATH_RESOURCES);
		var appFeaturesPath = Path.Join(resourcesFolder, DivinityApp.PATH_APP_FEATURES);
		var defaultPathwaysPath = Path.Join(resourcesFolder, DivinityApp.PATH_DEFAULT_PATHWAYS);
		var ignoredModsPath = Path.Join(resourcesFolder, DivinityApp.PATH_IGNORED_MODS);

		DivinityApp.Log($"Loading resources from '{resourcesFolder}'");

		if (File.Exists(appFeaturesPath))
		{
			var savedFeatures = DivinityJsonUtils.SafeDeserializeFromPath<Dictionary<string, bool>>(appFeaturesPath);
			if (savedFeatures != null)
			{
				var features = new Dictionary<string, bool>(savedFeatures, StringComparer.OrdinalIgnoreCase);
				AppSettings.Features.ApplyDictionary(features);
			}
		}

		if (File.Exists(defaultPathwaysPath))
		{
			AppSettings.DefaultPathways = DivinityJsonUtils.SafeDeserializeFromPath<DefaultPathwayData>(defaultPathwaysPath);
		}

		if (File.Exists(ignoredModsPath))
		{
			var ignoredModsData = DivinityJsonUtils.SafeDeserializeFromPath<IgnoredModsData>(ignoredModsPath);
			if (ignoredModsData != null)
			{
				if (ignoredModsData.IgnoreBuiltinPath != null)
				{
					foreach (var path in ignoredModsData.IgnoreBuiltinPath)
					{
						if (!String.IsNullOrEmpty(path))
						{
							DivinityModDataLoader.IgnoreBuiltinPath.Add(path.Replace(Path.DirectorySeparatorChar, '/'));
						}
					}
				}

				foreach (var ignoredMod in ignoredModsData.Mods)
				{
					var mod = new DivinityModData();
					mod.SetIsBaseGameMod(true);
					mod.IsLarianMod = true;
					if (ignoredMod.UUID.IsValid()) mod.UUID = ignoredMod.UUID;
					if (ignoredMod.Name.IsValid()) mod.Name = ignoredMod.Name;
					if (ignoredMod.Description.IsValid()) mod.Description = ignoredMod.Description;
					if (ignoredMod.Folder.IsValid()) mod.Folder = ignoredMod.Folder;
					if (ignoredMod.Type.IsValid()) mod.ModType = ignoredMod.Type;
					if (ignoredMod.Author.IsValid()) mod.Author = ignoredMod.Author;
					if (ignoredMod.Version != null) mod.Version = new LarianVersion(ignoredMod.Version.Value);
					if (ignoredMod.Tags.IsValid()) mod.AddTags(ignoredMod.Tags.Split(';'));
					DivinityApp.IgnoredMods.Add(mod);
				}

				foreach (var uuid in ignoredModsData.IgnoreDependencies)
				{
					var mod = DivinityApp.IgnoredMods.FirstOrDefault(x => x.UUID.ToLower() == uuid.ToLower());
					if (mod != null)
					{
						DivinityApp.IgnoredDependencyMods.Add(mod);
					}
				}

				//DivinityApp.LogMessage("Ignored mods:\n" + String.Join("\n", DivinityApp.IgnoredMods.Select(x => x.Name)));
			}
		}
	}

	public bool TrySaveAll(out List<Exception> errors)
	{
		var capturedErrors = new List<Exception>();
		_saveSettings.ForEach(entry =>
		{
			if (!entry.Save(out var ex) && ex != null)
			{
				capturedErrors.Add(ex);
			}
		});
		errors = capturedErrors;
		return errors.Count == 0;
	}

	public bool TryLoadAll(out List<Exception> errors)
	{
		var capturedErrors = new List<Exception>();
		_loadSettings.ForEach(entry =>
		{
			if (!entry.Load(out var ex) && ex != null)
			{
				capturedErrors.Add(ex);
			}
		});
		errors = capturedErrors;
		return errors.Count == 0;
	}

	public void UpdateLastUpdated(IList<string> updatedModIds)
	{
		if (updatedModIds.Count > 0)
		{
			var time = DateTime.Now.Ticks;
			foreach (var id in updatedModIds)
			{
				ModConfig.LastUpdated[id] = time;
			}
			ModConfig.Save(out _);
		}
	}

	public void UpdateLastUpdated(IList<DivinityModData> updatedMods)
	{
		if (updatedMods.Count > 0)
		{
			var time = DateTime.Now.Ticks;
			foreach (var mod in updatedMods)
			{
				if (!String.IsNullOrEmpty(mod.UUID)) ModConfig.LastUpdated[mod.UUID] = time;
			}
			ModConfig.Save(out _);
		}
	}

	public SettingsService()
	{
		AppSettings = new AppSettings();
		ManagerSettings = new ModManagerSettings();
		ModConfig = new UserModConfig();

		_loadSettings = [ManagerSettings, ModConfig];
		_saveSettings = [ManagerSettings, ModConfig];
	}
}