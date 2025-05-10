using DynamicData;

using ModManager.Models;
using ModManager.Models.App;
using ModManager.Models.Mod;
using ModManager.Models.Settings;
using ModManager.Util;

using ReactiveUI;

using System.IO;
using System.Reactive.Linq;

namespace ModManager.Services;

public class SettingsService : ReactiveObject, ISettingsService
{
	private readonly IFileSystemService _fs;

	public AppSettings AppSettings { get; private set; }
	public ModManagerSettings ManagerSettings { get; private set; }
	public UserModConfig ModConfig { get; private set; }
	public ScriptExtenderSettings ExtenderSettings { get; private set; }
	public ScriptExtenderUpdateConfig ExtenderUpdaterSettings { get; private set; }

	private readonly List<ISerializableSettings> _loadSettings;
	private readonly List<ISerializableSettings> _saveSettings;

	public bool TryLoadAppSettings(out Exception? error)
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
			var savedFeatures = JsonUtils.SafeDeserializeFromPath<Dictionary<string, bool>>(appFeaturesPath);
			if (savedFeatures != null)
			{
				var features = new Dictionary<string, bool>(savedFeatures, StringComparer.OrdinalIgnoreCase);
				AppSettings.Features.ApplyDictionary(features);
			}
		}

		if (File.Exists(defaultPathwaysPath))
		{
			AppSettings.DefaultPathways = JsonUtils.SafeDeserializeFromPath<DefaultPathwayData>(defaultPathwaysPath);
		}

		if (File.Exists(ignoredModsPath))
		{
			var ignoredModsData = JsonUtils.SafeDeserializeFromPath<IgnoredModsData>(ignoredModsPath);
			if (ignoredModsData != null)
			{
				if (ignoredModsData.IgnoreBuiltinPath != null)
				{
					foreach (var path in ignoredModsData.IgnoreBuiltinPath)
					{
						if (path.IsValid())
						{
							ModDataLoader.IgnoreBuiltinPath.Add(path.Replace(Path.DirectorySeparatorChar, '/'));
						}
					}
				}

				foreach (var ignoredMod in ignoredModsData.Mods)
				{
					if(ignoredMod.UUID.IsValid())
					{
						var mod = new ModData(ignoredMod.UUID);
						mod.SetIsBaseGameMod(true);
						mod.IsLarianMod = true;
						if (ignoredMod.Name.IsValid()) mod.Name = ignoredMod.Name;
						if (ignoredMod.Description.IsValid()) mod.Description = ignoredMod.Description;
						if (ignoredMod.Folder.IsValid()) mod.Folder = ignoredMod.Folder;
						if (ignoredMod.Type.IsValid()) mod.ModType = ignoredMod.Type;
						if (ignoredMod.Author.IsValid()) mod.Author = ignoredMod.Author;
						if (ignoredMod.Version != null) mod.Version = new LarianVersion(ignoredMod.Version.Value);
						if (ignoredMod.Tags.IsValid()) mod.AddTags(ignoredMod.Tags.Split(';'));

						var existing = DivinityApp.IgnoredMods.Lookup(mod.UUID);
						if (!existing.HasValue || existing.Value.Version < mod.Version)
						{
							DivinityApp.IgnoredMods.AddOrUpdate(mod);
						}
					}
				}

				foreach (var uuid in ignoredModsData.IgnoreDependencies)
				{
					if(uuid.IsValid())
					{
						DivinityApp.IgnoredDependencyMods.Add(uuid);
					}
				}

				if(ignoredModsData.MainCampaign?.IsValid() == true)
				{
					var modManager = Locator.Current.GetService<IModManagerService>();
					if (modManager != null)
					{
						modManager.MainCampaignGuid = ignoredModsData.MainCampaign;
					}
				}

				//DivinityApp.LogMessage("Ignored mods:\n" + string.Join("\n", DivinityApp.IgnoredMods.Select(x => x.Name)));
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

	public bool TryLoadAll(out List<Exception> errors, bool saveIfNotFound = true)
	{
		var capturedErrors = new List<Exception>();
		_loadSettings.ForEach(entry =>
		{
			if (!entry.Load(out var ex, saveIfNotFound) && ex != null)
			{
				capturedErrors.Add(ex);
			}
		});
		errors = capturedErrors;
		return errors.Count == 0;
	}

	public bool TrySave(ISerializableSettings settings, out Exception? ex)
	{
		return settings.Save(out ex);
	}

	public bool TryLoad(ISerializableSettings settings, out Exception? ex, bool saveIfNotFound = true)
	{
		return settings.Load(out ex, saveIfNotFound);
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

	public void UpdateLastUpdated(IList<ModData> updatedMods)
	{
		if (updatedMods.Count > 0)
		{
			var time = DateTime.Now.Ticks;
			foreach (var mod in updatedMods)
			{
				if (!string.IsNullOrEmpty(mod.UUID)) ModConfig.LastUpdated[mod.UUID] = time;
			}
			ModConfig.Save(out _);
		}
	}

	public string? GetGameExecutableDirectory()
	{
		var directory = _fs.Path.GetDirectoryName(ManagerSettings.GameExecutablePath);
		if (directory.IsValid())
		{
			return directory;
		}
		return null;
	}

	private static string? GetExtenderLogsDirectory(string? defaultDirectory, string? logDirectory)
	{
		if (!logDirectory.IsValid())
		{
			return defaultDirectory;
		}
		return logDirectory;
	}

	public SettingsService(IFileSystemService fs)
	{
		_fs = fs;

		AppSettings = new AppSettings();
		ManagerSettings = new ModManagerSettings();
		ModConfig = new UserModConfig();
		ExtenderSettings = new ScriptExtenderSettings();
		ExtenderUpdaterSettings = new ScriptExtenderUpdateConfig();

		ManagerSettings.InitSubscriptions();

		_loadSettings = [ManagerSettings, ModConfig, ExtenderSettings, ExtenderUpdaterSettings];
		_saveSettings = [ManagerSettings, ModConfig, ExtenderSettings, ExtenderUpdaterSettings];

		ManagerSettings.WhenAnyValue(x => x.DebugModeEnabled).BindTo(ExtenderSettings, x => x.DevOptionsEnabled);
		ManagerSettings.WhenAnyValue(x => x.DebugModeEnabled).BindTo(ExtenderUpdaterSettings, x => x.DevOptionsEnabled);

		this.WhenAnyValue(x => x.ManagerSettings.DefaultExtenderLogDirectory, x => x.ExtenderSettings.LogDirectory)
		.Select(x => GetExtenderLogsDirectory(x.Item1, x.Item2))
		.BindTo(ManagerSettings, x => x.ExtenderLogDirectory);
	}
}
