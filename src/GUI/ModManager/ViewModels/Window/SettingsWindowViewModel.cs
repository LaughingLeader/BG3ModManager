using DynamicData.Binding;

using ModManager.Enums.Extender;
using ModManager.Extensions;
using ModManager.Models;
using ModManager.Models.Extender;
using ModManager.Models.Settings;
using ModManager.Util;
using ModManager.ViewModels.Main;

using Newtonsoft.Json;

using System.ComponentModel;

namespace ModManager.ViewModels;

public enum SettingsWindowTab
{
	[Description("Mod Manager Settings")]
	Default = 0,
	[Description("Update Settings")]
	Update = 1,
	[Description("Script Extender Settings")]
	Extender = 2,
	[Description("Script Extender Updater Settings")]
	ExtenderUpdater = 3,
	[Description("Keybindings")]
	Keybindings = 4,
	[Description("Advanced Settings")]
	Advanced = 5
}

public class GameLaunchParamEntry : ReactiveObject
{
	[Reactive] public string Name { get; set; }
	[Reactive] public string Description { get; set; }
	[Reactive] public bool DebugModeOnly { get; set; }

	[ObservableAsProperty] public bool HasToolTip { get; }

	public GameLaunchParamEntry(string name, string description, bool debug = false)
	{
		Name = name;
		Description = description;
		DebugModeOnly = debug;

		this.WhenAnyValue(x => x.Description).Select(x => !String.IsNullOrEmpty(x)).ToUIProperty(this, x => x.HasToolTip);
	}
}

public class SettingsWindowViewModel : ReactiveObject, IClosableViewModel, IRoutableViewModel
{
	#region IClosableViewModel/IRoutableViewModel
	public string UrlPathSegment => "settings";
	public IScreen HostScreen { get; }
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }
	#endregion

	private readonly IInteractionsService _interactions;

	public ObservableCollectionExtended<ScriptExtenderUpdateVersion> ScriptExtenderUpdates { get; private set; }
	[Reactive] public ScriptExtenderUpdateVersion TargetVersion { get; set; }
	public ObservableCollectionExtended<GameLaunchParamEntry> LaunchParams { get; private set; }

	[Reactive] public SettingsWindowTab SelectedTabIndex { get; set; }
	[Reactive] public Hotkey SelectedHotkey { get; set; }
	[Reactive] public bool HasFetchedManifest { get; set; }

	[ObservableAsProperty] public bool ExtenderTabIsVisible { get; }
	[ObservableAsProperty] public bool KeybindingsTabIsVisible { get; }
	[ObservableAsProperty] public bool DeveloperModeVisibility { get; }
	[ObservableAsProperty] public bool ExtenderTabVisibility { get; }
	[ObservableAsProperty] public bool ExtenderUpdaterVisibility { get; }
	[ObservableAsProperty] public string ResetSettingsCommandToolTip { get; }
	[ObservableAsProperty] public string ExtenderSettingsFilePath { get; }
	[ObservableAsProperty] public string ExtenderUpdaterSettingsFilePath { get; }

	public RxCommandUnit SaveSettingsCommand { get; }
	public RxCommandUnit OpenSettingsFolderCommand { get; }
	public RxCommandUnit ExportExtenderSettingsCommand { get; }
	public RxCommandUnit ExportExtenderUpdaterSettingsCommand { get; }
	public RxCommandUnit ResetSettingsCommand { get; }
	public RxCommandUnit ClearCacheCommand { get; }
	public ReactiveCommand<string, Unit> AddLaunchParamCommand { get; }
	public RxCommandUnit ClearLaunchParamsCommand { get; }
	public RxCommandUnit AssociateWithNXMCommand { get; }

	private readonly ScriptExtenderUpdateVersion _emptyVersion = new();

	private readonly JsonSerializerSettings _jsonConfigExportSettings = new()
	{
		DefaultValueHandling = DefaultValueHandling.Ignore,
		NullValueHandling = NullValueHandling.Ignore,
		Formatting = Formatting.Indented
	};

	private string SelectedTabToResetTooltip(SettingsWindowTab tab)
	{
		var name = TabToName(tab);
		return $"Reset {name}";
	}

	private string TabToName(SettingsWindowTab tab) => tab.GetDescription();

	public async Task<Unit> GetExtenderUpdatesAsync(ExtenderUpdateChannel channel = ExtenderUpdateChannel.Release)
	{
		var url = String.Format(DivinityApp.EXTENDER_MANIFESTS_URL, channel.GetDescription());
		DivinityApp.Log($"Checking for script extender manifest info at '{url}'");
		var text = await WebHelper.DownloadUrlAsStringAsync(url, CancellationToken.None);
		//#if DEBUG
		//			DivinityApp.Log($"Manifest info:\n{text}");
		//#endif
		if (!String.IsNullOrEmpty(text))
		{
			if (DivinityJsonUtils.TrySafeDeserialize<ScriptExtenderUpdateData>(text, out var data))
			{
				var res = data.Resources.FirstOrDefault();
				if (res != null)
				{
					var lastVersion = ExtenderUpdaterSettings.TargetVersion;
					var lastDigest = ExtenderUpdaterSettings.TargetResourceDigest;
					var lastBuildDate = TargetVersion != _emptyVersion ? TargetVersion?.BuildDate : null;
					await Observable.Start(() =>
					{
						ScriptExtenderUpdateVersion nextVersion = null;
						TargetVersion = null;
						ScriptExtenderUpdates.Clear();
						ScriptExtenderUpdates.Add(_emptyVersion);
						ScriptExtenderUpdates.AddRange(res.Versions.OrderByDescending(x => x.BuildDate));
						if (lastBuildDate != null) nextVersion = ScriptExtenderUpdates.FirstOrDefault(x => x.BuildDate == lastBuildDate);
						if (nextVersion == null && !String.IsNullOrEmpty(lastDigest))
						{
							nextVersion = ScriptExtenderUpdates.FirstOrDefault(x => x.Digest == lastDigest);
						}
						if (nextVersion == null && !String.IsNullOrEmpty(lastVersion))
						{
							nextVersion = ScriptExtenderUpdates.FirstOrDefault(x => x.Version == lastVersion);
						}
						nextVersion ??= _emptyVersion;
						TargetVersion = nextVersion;

						HasFetchedManifest = true;
					}, RxApp.MainThreadScheduler);
				}
			}
		}
		return Unit.Default;
	}

	private IDisposable _manifestFetchingTask;
	private long _lastManifestCheck = -1;

	private bool CanCheckManifest => _lastManifestCheck == -1 || DateTimeOffset.Now.ToUnixTimeSeconds() - _lastManifestCheck >= 3000;

	private void FetchLatestManifestData(ExtenderUpdateChannel channel, bool force = false)
	{
		if (force || CanCheckManifest)
		{
			_manifestFetchingTask?.Dispose();

			_lastManifestCheck = DateTimeOffset.Now.ToUnixTimeSeconds();
			_manifestFetchingTask = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, cts) => await GetExtenderUpdatesAsync(channel));
		}
	}

	private void OnVisibilityChanged(bool b)
	{
		_manifestFetchingTask?.Dispose();

		if (b)
		{
			_manifestFetchingTask = RxApp.TaskpoolScheduler.ScheduleAsync(TimeSpan.FromMilliseconds(100), async (sch, cts) => await GetExtenderUpdatesAsync(ExtenderUpdaterSettings.UpdateChannel));
			//FetchLatestManifestData(ExtenderUpdaterSettings.UpdateChannel);
		}
	}

	[GenerateView]
	public ModManagerSettings Settings { get; private set; }

	[GenerateView]
	public ModManagerUpdateSettings UpdateSettings { get; private set; }

	[GenerateView]
	public ScriptExtenderSettings ExtenderSettings { get; private set; }

	[GenerateView]
	public ScriptExtenderUpdateConfig ExtenderUpdaterSettings { get; private set; }

	public void OnTargetVersionSelected(ScriptExtenderUpdateVersion entry)
	{
		if (HasFetchedManifest)
		{
			if (entry != _emptyVersion)
			{
				ExtenderUpdaterSettings.TargetVersion = entry.Version;
				ExtenderUpdaterSettings.TargetResourceDigest = entry.Digest;
			}
			else
			{
				ExtenderUpdaterSettings.TargetVersion = "";
				ExtenderUpdaterSettings.TargetResourceDigest = "";
			}
		}
	}

	public void OnTargetVersionSelected(object entry)
	{
		OnTargetVersionSelected((ScriptExtenderUpdateVersion)entry);
	}

	public bool ExportExtenderSettings()
	{
		var outputFile = Path.Join(Path.GetDirectoryName(Settings.GameExecutablePath), "ScriptExtenderSettings.json");
		try
		{
			_jsonConfigExportSettings.DefaultValueHandling = ExtenderSettings.ExportDefaultExtenderSettings ? DefaultValueHandling.Include : DefaultValueHandling.Ignore;
			var contents = JsonConvert.SerializeObject(Settings.ExtenderSettings, _jsonConfigExportSettings);
			File.WriteAllText(outputFile, contents);
			AppServices.Commands.ShowAlert($"Saved Script Extender settings to '{outputFile}'", AlertType.Success, 20);
			return true;
		}
		catch (Exception ex)
		{
			AppServices.Commands.ShowAlert($"Error saving Script Extender settings to '{outputFile}':\n{ex}", AlertType.Danger);
		}
		return false;
	}

	public bool ExportExtenderUpdaterSettings()
	{
		var outputFile = Path.Join(Path.GetDirectoryName(Settings.GameExecutablePath), "ScriptExtenderUpdaterConfig.json");
		try
		{
			_jsonConfigExportSettings.DefaultValueHandling = ExtenderSettings.ExportDefaultExtenderSettings ? DefaultValueHandling.Include : DefaultValueHandling.Ignore;
			var contents = JsonConvert.SerializeObject(Settings.ExtenderUpdaterSettings, _jsonConfigExportSettings);
			File.WriteAllText(outputFile, contents);
			AppServices.Commands.ShowAlert($"Saved Script Extender Updater settings to '{outputFile}'", AlertType.Success, 20);

			ViewModelLocator.Main.UpdateExtender(true);

			return true;
		}
		catch (Exception ex)
		{
			AppServices.Commands.ShowAlert($"Error saving Script Extender Updater settings to '{outputFile}':\n{ex}", AlertType.Danger);
		}
		return false;
	}

	public void SaveSettings()
	{
		try
		{
			var attr = File.GetAttributes(Settings.GameExecutablePath);
			if (attr.HasFlag(System.IO.FileAttributes.Directory))
			{
				var exeName = "";
				if (!DivinityRegistryHelper.IsGOG)
				{
					exeName = Path.GetFileName(AppServices.Settings.AppSettings.DefaultPathways.Steam.ExePath);
				}
				else
				{
					exeName = Path.GetFileName(AppServices.Settings.AppSettings.DefaultPathways.GOG.ExePath);
				}

				var exe = Path.Join(Settings.GameExecutablePath, exeName);
				if (File.Exists(exe))
				{
					Settings.GameExecutablePath = exe;
				}
			}
		}
		catch (Exception) { }

		if (IsVisible)
		{
			switch (SelectedTabIndex)
			{
				case SettingsWindowTab.Default:
				case SettingsWindowTab.Update:
				case SettingsWindowTab.Advanced:
					//Handled in _main.SaveSettings
					AppServices.Commands.ShowAlert("Saved settings.", AlertType.Success, 10);
					break;
				case SettingsWindowTab.Extender:
					ExportExtenderSettings();
					break;
				case SettingsWindowTab.ExtenderUpdater:
					ExportExtenderUpdaterSettings();
					break;
				case SettingsWindowTab.Keybindings:
					/*var success = ViewModelLocator.Main.Keys.SaveKeybindings(out var msg);
					if (!success)
					{
						AppServices.Commands.ShowAlert(msg, AlertType.Danger);
					}
					else if (!String.IsNullOrEmpty(msg))
					{
						AppServices.Commands.ShowAlert(msg, AlertType.Success, 10);
					}*/
					break;
			}
		}

		AppServices.Settings.TrySaveAll(out _);
	}

	private static readonly string _associateNXMMessage = @"This will allow updating mods via the ""Mod Manager Download"" button on the Nexus Mods website.
The following registry key will be added or updated:
HKEY_CLASSES_ROOT\nxm\shell\open\command
";

	private void AssociateWithNXM()
	{
		_interactions.ShowMessageBox.Handle(new("Associate BG3MM with nxm:// links?", _associateNXMMessage, InteractionMessageBoxType.YesNo)).Subscribe(result =>
		{
			if (result && DivinityApp.GetExePath() is string exePath)
			{
				if (DivinityRegistryHelper.AssociateWithNXMProtocol(exePath))
				{
					UpdateSettings.IsAssociatedWithNXM = true;
					AppServices.Commands.ShowAlert("nxm:// protocol assocation successfully set");
				}
				else
				{
					UpdateSettings.IsAssociatedWithNXM = false;
					AppServices.Commands.ShowAlert("Failed to set nxm protocol in the registry. Check the log", AlertType.Danger);
				}
			}
		});
	}

	public SettingsWindowViewModel(IInteractionsService interactions, IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;
		CloseCommand = this.CreateCloseCommand();

		_interactions = interactions;

		TargetVersion = _emptyVersion;

		if (HostScreen is MainWindowViewModel main)
		{
			main.WhenAnyValue(x => x.Settings).BindTo(this, x => x.Settings);
			main.WhenAnyValue(x => x.Settings.UpdateSettings).BindTo(this, x => x.UpdateSettings);
			main.WhenAnyValue(x => x.Settings.ExtenderSettings).BindTo(this, x => x.ExtenderSettings);
			main.WhenAnyValue(x => x.Settings.ExtenderUpdaterSettings).BindTo(this, x => x.ExtenderUpdaterSettings);
		}

		ScriptExtenderUpdates = [_emptyVersion];
		LaunchParams =
		[
			new("-continueGame", "Automatically load the last save when loading into the main menu"),
			new("-storylog 1", "Enables the story log"),
			new(@"--logPath """, "A directory to write story logs to"),
			new("--cpuLimit x", "Limit the cpu to x amount of threads (unknown if this works)"),
			new("-asserts 1", "", true),
			new("-stats 1", "", true),
			new("-dynamicStory 1", "", true),
			new("-externalcrashhandler", "", true),
			new(@"-nametag """, "", true),
			new(@"-module """, "", true),
			new(@"+connect_lobby """, "", true),
			new("-locaupdater 1", "", true),
			new(@"-mediaPath """, "", true),
		];

		var whenTab = this.WhenAnyValue(x => x.SelectedTabIndex);
		whenTab.Select(x => x == SettingsWindowTab.Extender).ToUIProperty(this, x => x.ExtenderTabIsVisible);
		whenTab.Select(x => x == SettingsWindowTab.Keybindings).ToUIProperty(this, x => x.KeybindingsTabIsVisible);

		this.WhenAnyValue(x => x.Settings.SkipLauncher, x => x.KeybindingsTabIsVisible);
		this.WhenAnyValue(x => x.TargetVersion).WhereNotNull().ObserveOn(RxApp.MainThreadScheduler).Subscribe(OnTargetVersionSelected);

		this.WhenAnyValue(x => x.SelectedTabIndex).Select(SelectedTabToResetTooltip).ToUIProperty(this, x => x.ResetSettingsCommandToolTip);

		ExtenderSettings.WhenAnyValue(x => x.DeveloperMode).Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.DeveloperModeVisibility);

		this.WhenAnyValue(x => x.ExtenderUpdaterSettings.UpdaterIsAvailable)
			.ToUIProperty(this, x => x.ExtenderTabVisibility);

		this.WhenAnyValue(x => x.ExtenderUpdaterSettings.UpdaterIsAvailable,
			x => x.Settings.DebugModeEnabled,
			x => x.ExtenderSettings.DeveloperMode)
			.Select(x => PropertyConverters.BoolToVisibility(x.Item1 && (x.Item2 || x.Item3))).ToUIProperty(this, x => x.ExtenderUpdaterVisibility);

		ExtenderUpdaterSettings.WhenAnyValue(x => x.UpdateChannel).Subscribe((channel) =>
		{
			if (IsVisible)
			{
				FetchLatestManifestData(channel, true);
			}
		});

		Settings.WhenAnyValue(x => x.GameExecutablePath).Select(x => Path.Join(Path.GetDirectoryName(x), DivinityApp.EXTENDER_CONFIG_FILE)).ToUIProperty(this, x => x.ExtenderSettingsFilePath);
		Settings.WhenAnyValue(x => x.GameExecutablePath).Select(x => Path.Join(Path.GetDirectoryName(x), DivinityApp.EXTENDER_UPDATER_CONFIG_FILE)).ToUIProperty(this, x => x.ExtenderUpdaterSettingsFilePath);

		var settingsProperties = new HashSet<string>();
		settingsProperties.UnionWith(Settings.GetSettingsAttributes().Select(x => x.Property.Name));
		settingsProperties.UnionWith(ExtenderSettings.GetSettingsAttributes().Select(x => x.Property.Name));
		settingsProperties.UnionWith(ExtenderUpdaterSettings.GetSettingsAttributes().Select(x => x.Property.Name));

		var whenVisible = this.WhenAnyValue(x => x.IsVisible, (b) => b == true);
		var propertyChanged = nameof(ReactiveObject.PropertyChanged);
		var whenSettings = Observable.FromEventPattern<PropertyChangedEventArgs>(Settings, propertyChanged);
		var whenExtenderSettings = Observable.FromEventPattern<PropertyChangedEventArgs>(ExtenderSettings, propertyChanged);
		var whenExtenderUpdaterSettings = Observable.FromEventPattern<PropertyChangedEventArgs>(ExtenderUpdaterSettings, propertyChanged);

		SaveSettingsCommand = ReactiveCommand.Create(SaveSettings, whenVisible);
		Observable.Merge(whenSettings, whenExtenderSettings, whenExtenderUpdaterSettings)
			.Where(e => settingsProperties.Contains(e.EventArgs.PropertyName))
			.Throttle(TimeSpan.FromMilliseconds(100))
			.Do(x => DivinityApp.Log($"Autosaving due to {x.EventArgs.PropertyName} changing"))
			.Select(x => Unit.Default)
			.InvokeCommand(SaveSettingsCommand);

		OpenSettingsFolderCommand = ReactiveCommand.Create(() =>
		{
			FileUtils.TryOpenPath(DivinityApp.GetAppDirectory(DivinityApp.DIR_DATA));
		});

		ResetSettingsCommand = ReactiveCommand.Create(() =>
		{
			var tabName = TabToName(SelectedTabIndex);
			_interactions.ShowMessageBox.Handle(new(
				$"Confirm {tabName} Reset",
				$"Reset {tabName} to Default?\nCurrent settings will be lost.",
				InteractionMessageBoxType.Warning | InteractionMessageBoxType.YesNo))
			.Subscribe(result =>
			{
				if (result)
				{
					switch (SelectedTabIndex)
					{
						case SettingsWindowTab.Default:
							Settings.SetToDefault();
							break;
						case SettingsWindowTab.Extender:
							Settings.ExtenderSettings.SetToDefault();
							break;
						case SettingsWindowTab.ExtenderUpdater:
							Settings.ExtenderUpdaterSettings.SetToDefault();
							break;
						case SettingsWindowTab.Keybindings:
							//ViewModelLocator.Main.Keys.SetToDefault();
							break;
						case SettingsWindowTab.Advanced:
							Settings.DebugModeEnabled = false;
							Settings.LogEnabled = false;
							Settings.GameLaunchParams = "";
							break;
					}
				}
			});
		});

		ClearCacheCommand = ReactiveCommand.Create(() =>
		{
			_interactions.ShowMessageBox.Handle(new(
				"Confirm Delete Cache",
				$"Delete local mod cache?\nThis cannot be undone.",
				InteractionMessageBoxType.Warning | InteractionMessageBoxType.YesNo))
			.Subscribe(result =>
			{
				if (result)
				{
					try
					{
						if (AppServices.Get<IModUpdaterService>().DeleteCache())
						{
							AppServices.Commands.ShowAlert($"Deleted local cache in {DivinityApp.GetAppDirectory("Data")}", AlertType.Success, 20);
						}
						else
						{
							AppServices.Commands.ShowAlert($"No cache to delete.", AlertType.Warning, 20);
						}
					}
					catch (Exception ex)
					{
						AppServices.Commands.ShowAlert($"Error deleting workshop cache:\n{ex}", AlertType.Danger);
					}
				}
			});
		});

		AddLaunchParamCommand = ReactiveCommand.Create((string param) =>
		{
			if (Settings.GameLaunchParams == null) Settings.GameLaunchParams = "";
			if (Settings.GameLaunchParams.IndexOf(param) < 0)
			{
				if (String.IsNullOrWhiteSpace(Settings.GameLaunchParams))
				{
					Settings.GameLaunchParams = param;
				}
				else
				{
					Settings.GameLaunchParams = Settings.GameLaunchParams + " " + param;
				}
			}
		});

		ClearLaunchParamsCommand = ReactiveCommand.Create(() =>
		{
			Settings.GameLaunchParams = "";
		});

		this.WhenAnyValue(x => x.IsVisible).Subscribe(OnVisibilityChanged);

		AssociateWithNXMCommand = ReactiveCommand.Create(AssociateWithNXM);
	}
}
