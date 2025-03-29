using Avalonia.Threading;

using DynamicData;
using DynamicData.Binding;

using ModManager.Extensions;
using ModManager.Models;
using ModManager.Models.App;
using ModManager.Models.Mod;
using ModManager.Models.NexusMods;
using ModManager.Models.Settings;
using ModManager.Models.Updates;
using ModManager.ModUpdater.Cache;
using ModManager.Services;
using ModManager.Util;
using ModManager.Windows;

using Newtonsoft.Json.Linq;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;

namespace ModManager.ViewModels.Main;

public class MainWindowViewModel : ReactiveObject, IScreen
{
	private const int ARCHIVE_BUFFER = 128000;

	public RoutingState Router { get; }
	public ViewManager Views { get; }

	private readonly IPathwaysService _pathways;
	public DivinityPathwayData PathwayData => _pathways.Data;
	private readonly ModImportService _importer;
	private readonly IModManagerService _manager;
	private readonly IModUpdaterService _updater;
	private readonly ISettingsService _settings;
	private readonly INexusModsService _nexusMods;
	private readonly IInteractionsService _interactions;
	private readonly IDialogService _dialogs;
	private readonly IEnvironmentService _environment;
	private readonly IGlobalCommandsService _globalCommands;

	[Reactive] public MainWindow Window { get; private set; }
	public DownloadActivityBarViewModel DownloadBar { get; private set; }

	public IProgressBarViewModel Progress => ViewModelLocator.Progress;

	[Reactive] public string Title { get; set; }
	[Reactive] public string Version { get; set; }

	//private readonly AppKeys _keys;
	//public AppKeys Keys => _keys;

	[Reactive] public bool IsInitialized { get; private set; }

	public AppSettings AppSettings { get; private set; }
	public ModManagerSettings Settings { get; private set; }
	public UserModConfig UserModConfig { get; private set; }
	[Reactive] public bool AppSettingsLoaded { get; set; }
	[Reactive] public bool GameIsRunning { get; private set; }
	[Reactive] public bool CanForceLaunchGame { get; set; }
	[Reactive] public bool IsRefreshing { get; private set; }
	[Reactive] public bool IsRefreshingModUpdates { get; private set; }

	/// <summary>Used to locked certain functionality when data is loading or the user is dragging an item.</summary>
	[Reactive] public bool IsLocked { get; private set; }
	[Reactive] public bool IsDragging { get; set; }
	[Reactive] public bool AllowDrop { get; private set; }

	[Reactive] public string StatusText { get; set; }
	[Reactive] public string StatusBarRightText { get; set; }

	[Reactive] public bool ModUpdatesAvailable { get; set; }
	[Reactive] public bool HighlightExtenderDownload { get; set; }
	[Reactive] public bool GameDirectoryFound { get; set; }

	[ObservableAsProperty] public bool CanLaunchGame { get; }
	[ObservableAsProperty] public bool IsDeletingFiles { get; }

	[ObservableAsProperty] public bool UpdatesViewIsVisible { get; }

	[Reactive] public bool StatusBarBusyIndicatorVisibility { get; set; }
	[ObservableAsProperty] public bool GitHubModSupportEnabled { get; }
	[ObservableAsProperty] public bool NexusModsSupportEnabled { get; }
	[ObservableAsProperty] public bool ModioSupportEnabled { get; }
	[ObservableAsProperty] public string NexusModsLimitsText { get; }
	[ObservableAsProperty] public Uri? NexusModsProfileBitmapUri { get; }
	[ObservableAsProperty] public bool NexusModsProfileAvatarVisibility { get; }
	[ObservableAsProperty] public bool UpdatingBusyIndicatorVisibility { get; }
	[ObservableAsProperty] public bool UpdateCountVisibility { get; }
	[ObservableAsProperty] public bool DeveloperModeVisibility { get; }

	public RxCommandUnit CancelMainProgressCommand { get; }
	public EventHandler OnRefreshed { get; set; }

	public bool DebugMode { get; set; }

	public void RefreshModUpdates()
	{
		ViewModelLocator.ModUpdates?.Clear();
		ModUpdatesAvailable = false;
		RefreshAllModUpdatesBackground();
	}

	public void RefreshStart()
	{
		_globalCommands.CanExecuteCommands = false;

		Progress.Title = !IsInitialized ? "Loading..." : "Refreshing...";
		IsRefreshing = true;
		_manager.Refresh();
		ViewModelLocator.ModUpdates.Clear();
		ViewModelLocator.ModOrder.Clear();
		ModUpdatesAvailable = false;
		//Window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
		//Window.TaskbarItemInfo.ProgressValue = 0;
		LoadAppConfig();
		Progress.Start(RefreshAsync, false, ViewModelLocator.ModOrder);
	}

	private async Task RefreshAsync(CancellationToken token)
	{
		DivinityApp.Log("Refreshing...");

		//Wait for UI to update
		//await View.Dispatcher.InvokeAsync(() => {}, System.Windows.Threading.DispatcherPriority.Background);

		await ViewModelLocator.ModOrder.RefreshAsync(this, token);

		await _updater.LoadCacheAsync(_manager.AllMods, Version, token);

		await Observable.Start(() =>
		{
			if (AppSettings.Features.ScriptExtender)
			{
				LoadExtenderSettingsBackground();
			}

			if (!GameDirectoryFound)
			{
				_globalCommands.ShowAlert("Game Data folder is not valid. Please set it in the preferences window and refresh", AlertType.Danger);
				//App.WM.Settings.Toggle(true);
			}

			IsRefreshing = false;

			RefreshModUpdates();

			IsInitialized = true;
			_globalCommands.CanExecuteCommands = true;
		}, RxApp.MainThreadScheduler);

		//RxApp.MainThreadScheduler.Schedule(ViewModelLocator.ModOrder.LoadCurrentProfile);
	}

	private async Task DownloadScriptExtenderAsync(CancellationToken token)
	{
		Progress.Title = "Setting up the Script Extender...";

		var exeDir = Path.GetDirectoryName(Settings.GameExecutablePath);
		var dllDestination = Path.Join(exeDir, DivinityApp.EXTENDER_UPDATER_FILE);

		var successes = 0;
		Stream? webStream = null;
		Stream? unzippedEntryStream = null;
		try
		{
			Progress.WorkText = $"Downloading {PathwayData.ScriptExtenderLatestReleaseUrl}";
			webStream = await WebHelper.DownloadFileAsStreamAsync(PathwayData.ScriptExtenderLatestReleaseUrl, token);
			if (webStream != null)
			{
				successes += 1;
				Progress.IncreaseValue(25, $"Extracting zip to {exeDir}...");
				using var archive = new ZipArchive(webStream);
				foreach (var entry in archive.Entries)
				{
					if (token.IsCancellationRequested) break;
					if (entry.Name.Equals(DivinityApp.EXTENDER_UPDATER_FILE, StringComparison.OrdinalIgnoreCase))
					{
						unzippedEntryStream = entry.Open(); // .Open will return a stream
						using var fs = File.Create(dllDestination, ARCHIVE_BUFFER, FileOptions.Asynchronous);
						await unzippedEntryStream.CopyToAsync(fs, ARCHIVE_BUFFER, token);
						successes += 1;
						break;
					}
				}
				Progress.IncreaseValue(50);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error downloading the script extender: {ex}");
		}
		finally
		{
			Progress.IncreaseValue(25, "Cleaning up...");
			webStream?.Dispose();
			unzippedEntryStream?.Dispose();
			successes += 1;
		}

		await Observable.Start(() =>
		{
			if (successes >= 3)
			{
				_globalCommands.ShowAlert($"Successfully installed the Extender updater {DivinityApp.EXTENDER_UPDATER_FILE} to '{exeDir}'", AlertType.Success, 20);
				HighlightExtenderDownload = false;
				Settings.ExtenderUpdaterSettings.UpdaterIsAvailable = true;
			}
			else
			{
				_globalCommands.ShowAlert($"Error occurred when installing the Extender updater {DivinityApp.EXTENDER_UPDATER_FILE} - Check the log", AlertType.Danger, 30);
			}
		}, RxApp.MainThreadScheduler);

		if (Settings.ExtenderUpdaterSettings.UpdaterIsAvailable)
		{
			await LoadExtenderSettingsAsync(token);
			await Observable.Start(() => UpdateExtender(true), RxApp.TaskpoolScheduler);
		}
	}

	private void DownloadScriptExtender()
	{
		ViewModelLocator.Progress.Title = "Setting up the Script Extender...";
		ViewModelLocator.Progress.Start(DownloadScriptExtenderAsync, true);
	}

	private void OnToolboxOutput(object sender, DataReceivedEventArgs e)
	{
		if (!String.IsNullOrEmpty(e.Data)) DivinityApp.Log($"[Toolbox] {e.Data}");
	}

	public void UpdateExtender(bool updateMods = true, CancellationToken? t = null)
	{
		if (AppSettings.Features.ScriptExtender && Settings.UpdateSettings.UpdateScriptExtender)
		{
			try
			{
				var exeDir = Path.GetDirectoryName(Settings.GameExecutablePath);
				var extenderUpdaterPath = Path.Join(exeDir, DivinityApp.EXTENDER_UPDATER_FILE);
				var toolboxPath = DivinityApp.GetToolboxPath();

				if (File.Exists(toolboxPath) && File.Exists(extenderUpdaterPath)
					&& Settings.ExtenderUpdaterSettings.UpdaterVersion >= 4
					&& RuntimeHelper.NetCoreRuntimeGreaterThanOrEqualTo(7))
				{
					DivinityApp.Log($"Running '{toolboxPath}' to update the script extender.");

					using var process = new Process();
					var info = process.StartInfo;
					info.FileName = toolboxPath;
					info.WorkingDirectory = Path.GetDirectoryName(toolboxPath);
					info.Arguments = $"UpdateScriptExtender -u \"{extenderUpdaterPath}\" -b \"{exeDir}\"";
					info.UseShellExecute = false;
					info.CreateNoWindow = true;
					info.RedirectStandardOutput = true;
					info.RedirectStandardError = true;
					process.ErrorDataReceived += OnToolboxOutput;
					process.OutputDataReceived += OnToolboxOutput;

					process.Start();
					process.BeginOutputReadLine();
					process.BeginErrorReadLine();
					if (!process.WaitForExit(120000))
					{
						process.Kill();
					}
					process.ErrorDataReceived -= OnToolboxOutput;
					process.OutputDataReceived -= OnToolboxOutput;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error updating script extender:\n{ex}");
			}
		}
		if (IsInitialized && !IsRefreshing)
		{
			CheckExtenderInstalledVersion(t);
			if (updateMods) RxApp.MainThreadScheduler.Schedule(UpdateExtenderVersionForAllMods);
		}
	}

	private bool OpenRepoLinkToDownload { get; set; }

	private void AskToDownloadScriptExtender()
	{
		if (!OpenRepoLinkToDownload)
		{
			if (!String.IsNullOrWhiteSpace(Settings.GameExecutablePath) && File.Exists(Settings.GameExecutablePath))
			{
				var exeDir = Path.GetDirectoryName(Settings.GameExecutablePath);
				var messageText = String.Format(@"Download and install the Script Extender?
The Script Extender is used by mods to extend the scripting language of the game, allowing new functionality.
The extender needs to only be installed once, as it automatically updates when you launch the game.
Download url: 
{0}
Directory the zip will be extracted to:
{1}", PathwayData.ScriptExtenderLatestReleaseUrl, exeDir);

				_interactions.ShowMessageBox.Handle(new("Download & Install the Script Extender?",
					messageText,
					InteractionMessageBoxType.YesNo)).Subscribe(result =>
				{
					if (result)
					{
						DownloadScriptExtender();
					}
				});
			}
			else
			{
				_globalCommands.ShowAlert("The 'Game Executable Path' is not set or is not valid", AlertType.Danger);
			}
		}
		else
		{
			DivinityApp.Log($"Getting a release download link failed for some reason. Opening repo url: {DivinityApp.EXTENDER_LATEST_URL}");
			FileUtils.TryOpenPath(DivinityApp.EXTENDER_LATEST_URL);
		}
	}

	private void CheckExtenderUpdaterVersion()
	{
		var extenderUpdaterPath = Path.Join(Path.GetDirectoryName(Settings.GameExecutablePath), DivinityApp.EXTENDER_UPDATER_FILE);
		DivinityApp.Log($"Looking for Script Extender at '{extenderUpdaterPath}'.");
		if (File.Exists(extenderUpdaterPath))
		{
			DivinityApp.Log($"Checking {DivinityApp.EXTENDER_UPDATER_FILE} for Script Extender ASCII bytes.");
			try
			{
				var fvi = FileVersionInfo.GetVersionInfo(extenderUpdaterPath);
				if (fvi != null && fvi.ProductName.IndexOf("Script Extender", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Settings.ExtenderUpdaterSettings.UpdaterIsAvailable = true;
					DivinityApp.Log($"Found the Extender at '{extenderUpdaterPath}'.");
					var extenderInfo = FileVersionInfo.GetVersionInfo(extenderUpdaterPath);
					if (!String.IsNullOrEmpty(extenderInfo.FileVersion))
					{
						var version = extenderInfo.FileVersion.Split('.')[0];
						if (int.TryParse(version, out var intVersion))
						{
							Settings.ExtenderUpdaterSettings.UpdaterVersion = intVersion;
						}
					}
				}
				else
				{
					DivinityApp.Log($"'{extenderUpdaterPath}' isn't the Script Extender?");
				}
			}
			catch (System.IO.IOException)
			{
				// This can happen if the game locks up the dll.
				// Assume it's the extender for now.
				Settings.ExtenderUpdaterSettings.UpdaterIsAvailable = true;
				DivinityApp.Log($"WARNING: {extenderUpdaterPath} is locked by a process.");
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading: '{extenderUpdaterPath}'\n{ex}");
			}
		}
		else
		{
			Settings.ExtenderUpdaterSettings.UpdaterIsAvailable = false;
			DivinityApp.Log($"Extender updater {DivinityApp.EXTENDER_UPDATER_FILE} not found.");
		}
	}

	public bool CheckExtenderInstalledVersion(CancellationToken? t)
	{
		var extenderAppDataDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DivinityApp.EXTENDER_APPDATA_DIRECTORY);
		if (Directory.Exists(extenderAppDataDir))
		{
			var files = FileUtils.EnumerateFiles(extenderAppDataDir, FileUtils.RecursiveOptions, (f) => f.EndsWith(DivinityApp.EXTENDER_APPDATA_DLL, StringComparison.OrdinalIgnoreCase));
			var isInstalled = false;
			var fullExtenderVersion = "";
			var majorVersion = -1;
			var targetVersion = Settings.ExtenderUpdaterSettings.TargetVersion;

			foreach (var f in files)
			{
				isInstalled = true;
				try
				{
					var extenderInfo = FileVersionInfo.GetVersionInfo(f);
					if (extenderInfo != null)
					{
						var fileVersion = $"{extenderInfo.FileMajorPart}.{extenderInfo.FileMinorPart}.{extenderInfo.FileBuildPart}.{extenderInfo.FilePrivatePart}";
						if (fileVersion == targetVersion)
						{
							majorVersion = extenderInfo.FileMajorPart;
							fullExtenderVersion = fileVersion;
							break;
						}
						if (extenderInfo.FileMajorPart > majorVersion)
						{
							majorVersion = extenderInfo.FileMajorPart;
							fullExtenderVersion = fileVersion;
						}
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error getting file info from: '{f}'\n\t{ex}");
				}
			}
			if (majorVersion > -1)
			{
				DivinityApp.Log($"Script Extender version found ({majorVersion})");
				Settings.ExtenderSettings.ExtenderIsAvailable = isInstalled;
				Settings.ExtenderSettings.ExtenderVersion = fullExtenderVersion;
				Settings.ExtenderSettings.ExtenderMajorVersion = majorVersion;
				return true;
			}
		}
		else
		{
			DivinityApp.Log($"Extender Local AppData folder not found at '{extenderAppDataDir}'. Skipping.");
		}
		return false;
	}

	private async Task<bool> CheckForLatestExtenderUpdaterRelease(CancellationToken token)
	{
		try
		{
			var latestReleaseZipUrl = "";
			DivinityApp.Log($"Checking for latest {DivinityApp.EXTENDER_UPDATER_FILE} release at 'https://github.com/{DivinityApp.EXTENDER_REPO_URL}'.");
			var latestReleaseData = await GitHubHelper.GetLatestReleaseJsonStringAsync(DivinityApp.EXTENDER_REPO_URL, token);
			if (!String.IsNullOrEmpty(latestReleaseData))
			{
				var jsonData = DivinityJsonUtils.SafeDeserialize<Dictionary<string, object>>(latestReleaseData);
				if (jsonData != null)
				{
					if (jsonData.TryGetValue("assets", out var assetsArray) && assetsArray is JArray assets)
					{
						foreach (var obj in assets.Children<JObject>())
						{
							if (obj.TryGetValue("browser_download_url", StringComparison.OrdinalIgnoreCase, out var browserUrl))
							{
								var url = browserUrl.ToString();
								if (url.EndsWith(".zip"))
								{
									latestReleaseZipUrl = url;
									if (url.IndexOf("Console") <= -1) break;
								}
							}
						}
					}
					if (jsonData.TryGetValue("tag_name", out var tagName) && tagName is string tag)
					{
						PathwayData.ScriptExtenderLatestReleaseVersion = tag;
					}
				}
				if (!String.IsNullOrEmpty(latestReleaseZipUrl))
				{
					OpenRepoLinkToDownload = false;
					PathwayData.ScriptExtenderLatestReleaseUrl = latestReleaseZipUrl;
					DivinityApp.Log($"Script Extender latest release url found: {latestReleaseZipUrl}");
					return true;
				}
				else
				{
					DivinityApp.Log($"Script Extender latest release not found.");
				}
			}
			else
			{
				OpenRepoLinkToDownload = true;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error checking for latest Script Extender release: {ex}");

			OpenRepoLinkToDownload = true;
		}

		return false;
	}

	private async Task<Unit> LoadExtenderSettingsAsync(CancellationToken token)
	{
		await Observable.Start(() =>
		{
			var settingsFilePath = PathwayData.ScriptExtenderSettingsFile(Settings);
			try
			{
				if (settingsFilePath.IsExistingFile())
				{
					if (DivinityJsonUtils.TrySafeDeserializeFromPath<ScriptExtenderSettings>(settingsFilePath, out var data))
					{
						DivinityApp.Log($"Loaded {settingsFilePath}");
						Settings.ExtenderSettings.SetFrom(data);
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading '{settingsFilePath}':\n{ex}");
			}

			var updaterSettingsFilePath = PathwayData.ScriptExtenderUpdaterConfigFile(Settings);
			try
			{
				if (updaterSettingsFilePath.IsExistingFile())
				{
					if (DivinityJsonUtils.TrySafeDeserializeFromPath<ScriptExtenderUpdateConfig>(updaterSettingsFilePath, out var data))
					{
						Settings.ExtenderUpdaterSettings.SetFrom(data);
						DivinityApp.Log($"Loaded {updaterSettingsFilePath}");
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error loading '{updaterSettingsFilePath}':\n{ex}");
			}

			CheckExtenderUpdaterVersion();
			CheckExtenderInstalledVersion(token);
			UpdateExtenderVersionForAllMods();

			return Unit.Default;
		}, RxApp.MainThreadScheduler);

		return Unit.Default;
	}

	public void LoadExtenderSettingsBackground()
	{
		DivinityApp.Log($"Loading extender settings.");
		RxApp.TaskpoolScheduler.ScheduleAsync(async (c, t) =>
		{
			await CheckForLatestExtenderUpdaterRelease(t);
			await LoadExtenderSettingsAsync(t);
			UpdateExtender(true, t);
			return Disposable.Empty;
		});
	}

	private void TryStartGameExe(string exePath, string launchParams = "")
	{
		//var isLoggingEnabled = Window.DebugLogListener != null;
		//if (!isLoggingEnabled) Window.ToggleLogging(true);

		try
		{
			Process proc = new();
			proc.StartInfo.FileName = exePath;
			proc.StartInfo.Arguments = launchParams;
			proc.StartInfo.WorkingDirectory = Directory.GetParent(exePath).FullName;
			proc.Start();

			//Update whether the game is running or not
			RxApp.TaskpoolScheduler.Schedule(TimeSpan.FromSeconds(5), () =>
			{
				AppServices.Get<IGameUtilitiesService>().CheckForGameProcess();
			});
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error starting game exe:\n{ex}");
			_globalCommands.ShowAlert("Error occurred when trying to start the game - Check the log", AlertType.Danger);
		}

		//if (!isLoggingEnabled) Window.ToggleLogging(false);
	}

	public void LaunchGame()
	{
		if (Settings.DisableLauncherTelemetry || Settings.DisableLauncherModWarnings)
		{
			RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, t) =>
			{
				await DivinityModDataLoader.UpdateLauncherPreferencesAsync(_pathways.GetLarianStudiosAppDataFolder(), !Settings.DisableLauncherTelemetry, !Settings.DisableLauncherModWarnings);
			});
		}

		if (!Settings.LaunchThroughSteam)
		{
			if (!File.Exists(Settings.GameExecutablePath))
			{
				if (String.IsNullOrWhiteSpace(Settings.GameExecutablePath))
				{
					_globalCommands.ShowAlert("No game executable path set", AlertType.Danger, 30);
				}
				else
				{
					_globalCommands.ShowAlert($"Failed to find game exe at, \"{Settings.GameExecutablePath}\"", AlertType.Danger, 90);
				}
				return;
			}
		}

		var exeArgs = new List<string>();
		var userLaunchParams = !String.IsNullOrEmpty(Settings.GameLaunchParams) ? Settings.GameLaunchParams : "";

		if (Settings.GameStoryLogEnabled && !Settings.ExtenderSettings.EnableLogging)
		{
			exeArgs.Add("-storylog 1");
		}

		if (Settings.SkipLauncher)
		{
			exeArgs.Add("--skip-launcher");
		}

		if (!Settings.LaunchThroughSteam)
		{
			//Args always set by the launcher
			exeArgs.Add("-externalcrashhandler");
			var sendStats = !Settings.DisableLauncherTelemetry ? 1 : 0;
			exeArgs.Add($"-stats {sendStats}");
			var isModded = ViewModelLocator.ModOrder.ActiveMods.Count > 0 ? 1 : 0;
			exeArgs.Add($"-modded {isModded}");
		}

		if (!String.IsNullOrEmpty(userLaunchParams))
		{
			foreach (var entry in exeArgs)
			{
				userLaunchParams.Replace(entry, "");
			}
		}

		exeArgs.Add(userLaunchParams);

		var launchParams = String.Join(" ", exeArgs);

		if (!Settings.LaunchThroughSteam)
		{
			var exePath = Settings.GameExecutablePath;
			var exeDir = Path.GetDirectoryName(exePath);

			if (Settings.LaunchDX11)
			{
				var nextExe = Path.Join(exeDir, "bg3_dx11.exe");
				if (File.Exists(nextExe))
				{
					exePath = nextExe;
				}
			}

			DivinityApp.Log($"Opening game exe at: {exePath} with args {launchParams}");
			TryStartGameExe(exePath, launchParams);
		}
		else
		{
			var appid = AppSettings.DefaultPathways.Steam.AppID ?? "1086940";
			var steamUrl = $"steam://run/{appid}//{launchParams}";
			DivinityApp.Log($"Opening game through steam via '{steamUrl}'");
			FileUtils.TryOpenPath(steamUrl);
		}

		if (Settings.ActionOnGameLaunch != GameLaunchWindowAction.None)
		{
			switch (Settings.ActionOnGameLaunch)
			{
				case GameLaunchWindowAction.Minimize:
					Window.WindowState = WindowState.Minimized;
					break;
				case GameLaunchWindowAction.Close:
					App.Current.Shutdown();
					break;
			}
		}
	}

	private bool CanLaunchGameCheck(ValueTuple<string, bool, bool, bool> x) => x.Item1.IsExistingFile() && (!x.Item2 || !x.Item3 || x.Item4);

	private void InitSettingsBindings()
	{
		var canOpenWorkshopFolder = this.WhenAnyValue(x => x.ModioSupportEnabled, x => x.Settings.WorkshopPath,
			(b, p) => (b && !String.IsNullOrEmpty(p) && Directory.Exists(p))).StartWith(false);

		var gameUtils = AppServices.Get<IGameUtilitiesService>();
		gameUtils.WhenAnyValue(x => x.GameIsRunning).BindTo(this, x => x.GameIsRunning);

		Settings.WhenAnyValue(x => x.GameExecutablePath).Subscribe(path =>
		{
			if (!String.IsNullOrEmpty(path)) gameUtils.AddGameProcessName(Path.GetFileNameWithoutExtension(path));
		});

		var whenGameExeProperties = this.WhenAnyValue(x => x.Settings.GameExecutablePath, x => x.Settings.LimitToSingleInstance, x => x.GameIsRunning, x => x.CanForceLaunchGame);
		var canOpenGameExe = whenGameExeProperties.Select(CanLaunchGameCheck);
		canOpenGameExe.ToUIProperty(this, x => x.CanLaunchGame);

		/*Keys.LaunchGame.AddAction(LaunchGame, canOpenGameExe);

		var canOpenLogDirectory = Settings.WhenAnyValue(x => x.ExtenderLogDirectory, (f) => Directory.Exists(f)).StartWith(false);

		var canDownloadScriptExtender = this.WhenAnyValue(x => x.PathwayData.ScriptExtenderLatestReleaseUrl, (p) => !String.IsNullOrEmpty(p));
		Keys.DownloadScriptExtender.AddAction(() => AskToDownloadScriptExtender(), canDownloadScriptExtender);

		var canOpenModsFolder = this.WhenAnyValue(x => x.PathwayData.AppDataModsPath, (p) => !String.IsNullOrEmpty(p) && Directory.Exists(p));
		Keys.OpenModsFolder.AddAction(() =>
		{
			FileUtils.TryOpenPath(PathwayData.AppDataModsPath);
		}, canOpenModsFolder);

		var canOpenGameFolder = Settings.WhenAnyValue(x => x.GameExecutablePath, (p) => !String.IsNullOrEmpty(p) && File.Exists(p));
		Keys.OpenGameFolder.AddAction(() =>
		{
			var folder = Path.GetDirectoryName(Settings.GameExecutablePath);
			if (Directory.Exists(folder))
			{
				FileUtils.TryOpenPath(folder);
			}
		}, canOpenGameFolder);

		Keys.OpenLogsFolder.AddAction(() =>
		{
			FileUtils.TryOpenPath(Settings.ExtenderLogDirectory);
		}, canOpenLogDirectory);

		Keys.OpenWorkshopFolder.AddAction(() =>
		{
			//DivinityApp.Log($"WorkshopSupportEnabled:{WorkshopSupportEnabled} canOpenWorkshopFolder CanExecute:{OpenWorkshopFolderCommand.CanExecute(null)}");
			if (!String.IsNullOrEmpty(Settings.WorkshopPath) && Directory.Exists(Settings.WorkshopPath))
			{
				FileUtils.TryOpenPath(Settings.WorkshopPath);
			}
		}, canOpenWorkshopFolder);*/

		Settings.WhenAnyValue(x => x.LogEnabled).Subscribe((logEnabled) =>
		{
			//Window.ToggleLogging(logEnabled);
		});

		// Updating extender requirement display
		Settings.WhenAnyValue(x => x.ExtenderSettings.EnableExtensions).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
		{
			if (Settings.SettingsWindowIsOpen)
			{
				UpdateExtenderVersionForAllMods();
			}
		});

		var actionLaunchChanged = Settings.WhenAnyValue(x => x.ActionOnGameLaunch).Skip(1).ObserveOn(RxApp.MainThreadScheduler);
		actionLaunchChanged.Subscribe((action) =>
		{
			if (!Settings.SettingsWindowIsOpen)
			{
				SaveSettings();
			}
		});

		Settings.WhenAnyValue(x => x.DisplayFileNames).Subscribe((b) =>
		{
			//TODO
			/*if (View != null && View.MenuItems.TryGetValue("ToggleFileNameDisplay", out var menuItem))
			{
				if (b)
				{
					menuItem.Header = "Show Display Names for Mods";
				}
				else
				{
					menuItem.Header = "Show File Names for Mods";
				}
			}*/
		});

		Settings.WhenAnyValue(x => x.DocumentsFolderPathOverride).Skip(1).Subscribe((x) =>
		{
			if (!IsLocked)
			{
				_pathways.SetGamePathways(Settings.GameDataPath, x);
				if (AppSettings.Features.ScriptExtender && IsInitialized && !IsRefreshing)
				{
					LoadExtenderSettingsBackground();
				}
				_globalCommands.ShowAlert($"Larian folder changed to '{x}' - Make sure to refresh", AlertType.Warning, 60);
			}
		});

		//Settings.WhenAnyValue(x => x.SaveWindowLocation).Subscribe(Window.ToggleWindowPositionSaving);
	}

	private void OnOrderNameChanged(object sender, OrderNameChangedArgs e)
	{
		if (Settings.LastOrder == e.LastName)
		{
			Settings.LastOrder = e.NewName;
			QueueSave();
		}
	}

	private async Task<bool> LoadSettings()
	{
		var success = true;
		if (!_settings.TryLoadAll(out var errors))
		{
			var errorMessage = String.Join("\n", errors.Select(x => x.ToString()));
			_globalCommands.ShowAlert($"Error loading settings: {errorMessage}", AlertType.Danger);
			success = false;
		}

		LoadAppConfig();

#if DOS2
		Settings.DefaultExtenderLogDirectory = Path.Join(Pathways.GetLarianStudiosAppDataFolder(), "Divinity Original Sin 2 Definitive Edition", "Extender Logs");
#else
		Settings.DefaultExtenderLogDirectory = Path.Join(_pathways.GetLarianStudiosAppDataFolder(), "Baldur's Gate 3", "Extender Logs");
#endif

		var githubSupportEnabled = AppSettings.Features.GitHub;
		var nexusModsSupportEnabled = AppSettings.Features.NexusMods;
		var workshopSupportEnabled = AppSettings.Features.Modio;

		if (workshopSupportEnabled)
		{
			if (!String.IsNullOrWhiteSpace(Settings.WorkshopPath))
			{
				var baseName = Path.GetFileNameWithoutExtension(Settings.WorkshopPath);
				if (baseName == "steamapps")
				{
					var newFolder = Path.Join(Settings.WorkshopPath, $"workshop/content/{AppSettings.DefaultPathways.Steam.AppID}");
					if (Directory.Exists(newFolder))
					{
						Settings.WorkshopPath = newFolder;
					}
					else
					{
						Settings.WorkshopPath = "";
					}
				}
			}

			if (String.IsNullOrEmpty(Settings.WorkshopPath) || !Directory.Exists(Settings.WorkshopPath))
			{
				Settings.WorkshopPath = DivinityRegistryHelper.GetWorkshopPath(AppSettings.DefaultPathways.Steam.AppID).Replace("\\", "/");
				if (!String.IsNullOrEmpty(Settings.WorkshopPath) && Directory.Exists(Settings.WorkshopPath))
				{
					DivinityApp.Log($"Workshop path set to: '{Settings.WorkshopPath}'.");
				}
			}
			else if (Directory.Exists(Settings.WorkshopPath))
			{
				DivinityApp.Log($"Found workshop folder at: '{Settings.WorkshopPath}'.");
			}
		}
		else
		{
			Settings.WorkshopPath = "";
		}

		//if (Settings.LogEnabled) Window.ToggleLogging(true);

		if (!_pathways.SetGamePathways(Settings.GameDataPath, Settings.DocumentsFolderPathOverride))
		{
			GameDirectoryFound = false;

			if (!FileUtils.HasDirectoryReadPermission(Settings.GameDataPath, Settings.DocumentsFolderPathOverride))
			{
				var message = $"BG3MM lacks permission to read one or both of the following paths:\nGame Data Path: ({Settings.GameDataPath})\nGame Executable Path: ({Settings.GameExecutablePath})";
				await _interactions.ShowMessageBox.Handle(new("File Permission Issue", message, InteractionMessageBoxType.Error));
			}
			else
			{
				var result = await _dialogs.OpenFolderAsync(new("Set Game Installation Folder",
					Environment.GetFolderPath(Environment.SpecialFolder.MyComputer),
					"Set the path to the Baldur's Gate 3 root installation folder"));

				if (result.Success)
				{
					var data = PathwayData;
					var dir = result.File;
					var dataDirectory = Path.Join(dir, AppSettings.DefaultPathways.GameDataFolder);
					var exePath = Path.Join(dir, AppSettings.DefaultPathways.Steam.ExePath);
					if (!File.Exists(exePath))
					{
						exePath = Path.Join(dir, AppSettings.DefaultPathways.GOG.ExePath);
					}
					if (Directory.Exists(dataDirectory))
					{
						Settings.GameDataPath = dataDirectory;
						GameDirectoryFound = true;
					}
					else
					{
						_globalCommands.ShowAlert("Failed to find Data folder with given installation directory", AlertType.Danger);
					}
					if (File.Exists(exePath))
					{
						Settings.GameExecutablePath = exePath;
					}
					else
					{
						_globalCommands.ShowAlert("Failed to find bg3.exe path with given installation directory", AlertType.Danger);
					}
					data.InstallPath = dir;
					//Services.Settings.TrySaveAll(out _);
				}
			}
		}
		else
		{
			GameDirectoryFound = true;
		}

		if (AppSettings.Features.ScriptExtender && IsInitialized && !IsRefreshing)
		{
			LoadExtenderSettingsBackground();
		}

		if (success)
		{
			Settings.CanSaveSettings = false;
		}

		return success;
	}

	public void SaveSettings()
	{
		if (!_settings.TrySaveAll(out var errors))
		{
			var errorMessage = String.Join("\n", errors.Select(x => x.ToString()));
			_globalCommands.ShowAlert($"Error saving settings: {errorMessage}", AlertType.Danger);
		}
		else
		{
			Settings.CanSaveSettings = false;
			/*if (!Keys.SaveKeybindings(out var errorMsg))
			{
				_globalCommands.ShowAlert(errorMsg, AlertType.Danger);
			}*/
		}
	}

	private IDisposable _deferSave;

	public void QueueSave()
	{
		_deferSave?.Dispose();
		_deferSave = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () => SaveSettings());
	}



	private string GetInitialStartingDirectory(string prioritizePath = "")
	{
		var directory = prioritizePath;

		if (!String.IsNullOrEmpty(prioritizePath) && FileUtils.TryGetDirectoryOrParent(prioritizePath, out var actualDir))
		{
			directory = actualDir;
		}
		else
		{
			if (!String.IsNullOrEmpty(Settings.LastImportDirectoryPath))
			{
				directory = Settings.LastImportDirectoryPath;
			}

			if (!Directory.Exists(directory) && !String.IsNullOrEmpty(PathwayData.LastSaveFilePath) && FileUtils.TryGetDirectoryOrParent(PathwayData.LastSaveFilePath, out var lastDir))
			{
				directory = lastDir;
			}
		}

		if (String.IsNullOrEmpty(directory) || !Directory.Exists(directory))
		{
			directory = DivinityApp.GetAppDirectory();
		}

		return directory;
	}

	/*private void MainWindowMessageBox_Closed_ResetColor(object sender, EventArgs e)
	{
		if (sender is Xceed.Wpf.Toolkit.MessageBox messageBox)
		{
			messageBox.WindowBackground = new SolidColorBrush(Color.FromRgb(78, 56, 201));
			messageBox.Closed -= MainWindowMessageBox_Closed_ResetColor;
		}
	}*/

	private void UpdateModExtenderStatus(DivinityModData mod)
	{
		mod.CurrentExtenderVersion = Settings.ExtenderSettings.ExtenderMajorVersion;

		if (mod.ScriptExtenderData != null && mod.ScriptExtenderData.HasAnySettings)
		{
			if (mod.ScriptExtenderData.Lua)
			{
				if (!Settings.ExtenderUpdaterSettings.UpdaterIsAvailable)
				{
					mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_MISSING_UPDATER;
				}
				else if (!Settings.ExtenderSettings.EnableExtensions)
				{
					mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_DISABLED;
				}
				else
				{
					if (Settings.ExtenderSettings.ExtenderMajorVersion > -1)
					{
						if (mod.ScriptExtenderData.RequiredVersion > -1 && Settings.ExtenderSettings.ExtenderMajorVersion < mod.ScriptExtenderData.RequiredVersion)
						{
							mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_OLD;
						}
						else
						{
							mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED;
						}
					}
					else
					{
						mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_MISSING;
					}
				}
			}
			else
			{
				mod.ExtenderModStatus = DivinityExtenderModStatus.SUPPORTS;
			}
		}
		else
		{
			mod.ExtenderModStatus = DivinityExtenderModStatus.NONE;
		}
	}

	public void UpdateExtenderVersionForAllMods()
	{
		foreach (var mod in _manager.AllMods)
		{
			UpdateModExtenderStatus(mod);
		}
	}

	private bool CanUpdateMod(DivinityModData mod, DateTime now, TimeSpan minWaitPeriod, ISettingsService settingsService)
	{
		if (settingsService.ModConfig.LastUpdated.TryGetValue(mod.UUID, out var last))
		{
			var time = new DateTime(last);
			return now - time >= minWaitPeriod;
		}
		return true;
	}

	private IList<DivinityModData> GetUpdateableMods()
	{
		var settingsService = AppServices.Get<ISettingsService>();
		var minUpdateTime = Settings.UpdateSettings.MinimumUpdateTimePeriod;
		if (minUpdateTime > TimeSpan.Zero)
		{
			var now = DateTime.Now;
			return _manager.UserMods.Where(x => CanUpdateMod(x, now, minUpdateTime, settingsService)).ToList();
		}
		return _manager.UserMods;
	}

	private IDisposable _refreshGitHubModsUpdatesBackgroundTask;

	private async Task<Unit> RefreshGitHubModsUpdatesBackgroundAsync(IScheduler sch, CancellationToken token)
	{
		var results = await _updater.GetGitHubUpdatesAsync(GetUpdateableMods(), Version, token);
		await sch.Yield(token);
		if (!token.IsCancellationRequested && results.Count > 0)
		{
			await Observable.Start(() =>
			{
				foreach (var kvp in results)
				{
					if (_manager.TryGetMod(kvp.Key, out var mod))
					{
						var updateData = new DivinityModUpdateData()
						{
							Mod = mod,
							DownloadData = new ModDownloadData()
							{
								DownloadPath = kvp.Value.BrowserDownloadLink,
								DownloadPathType = ModDownloadPathType.URL,
								DownloadSourceType = ModSourceType.GITHUB,
								Version = kvp.Value.Version,
								Date = kvp.Value.Date
							},
						};
						ViewModelLocator.ModUpdates.Add(updateData);
					}
				}
			}, RxApp.MainThreadScheduler);
		}
		return Unit.Default;
	}

	public void RefreshGitHubModsUpdatesBackground()
	{
		_refreshGitHubModsUpdatesBackgroundTask?.Dispose();
		_refreshGitHubModsUpdatesBackgroundTask = RxApp.TaskpoolScheduler.ScheduleAsync(RefreshGitHubModsUpdatesBackgroundAsync);
	}

	private IDisposable _refreshNexusModsUpdatesBackgroundTask;

	private async Task<Unit> RefreshNexusModsUpdatesBackgroundAsync(IScheduler sch, CancellationToken token)
	{
		var updates = await _updater.GetNexusModsUpdatesAsync(GetUpdateableMods(), Version, token);
		await sch.Yield(token);
		if (!token.IsCancellationRequested && updates.Count > 0)
		{
			var isPremium = _nexusMods.IsPremium;
			//$"https://www.nexusmods.com/Core/Libs/Common/Widgets/DownloadPopUp?id={DownloadPath}6&nmm=1&game_id={DivinityApp.NEXUSMODS_GAME_ID}";
			await Observable.Start(() =>
			{
				foreach (var update in updates.Values)
				{
					var updateData = new DivinityModUpdateData()
					{
						Mod = update.Mod,
						DownloadData = new ModDownloadData()
						{
							DownloadPath = update.DownloadLink.Uri.ToString(),
							DownloadPathType = ModDownloadPathType.URL,
							DownloadSourceType = ModSourceType.NEXUSMODS,
							Version = update.File.ModVersion,
							Date = DateUtils.UnixTimeStampToDateTime(update.File.UploadedTimestamp)
						},
					};
					if (!isPremium)
					{
						var nxmEnabled = "";
						if (Settings.UpdateSettings.IsAssociatedWithNXM)
						{
							nxmEnabled = "&nmm=1";
						}
						//Make this a link to the browser, where the user needs to initiate a .nxm url
						updateData.DownloadData.IsIndirectDownload = true;
						updateData.DownloadData.DownloadPath = $"https://www.nexusmods.com/Core/Libs/Common/Widgets/DownloadPopUp?id={update.File.FileId}{nxmEnabled}&game_id={DivinityApp.NEXUSMODS_GAME_ID}";
					}
					ViewModelLocator.ModUpdates.Add(updateData);
				}
			}, RxApp.MainThreadScheduler);
		}
		return Unit.Default;
	}

	public void RefreshNexusModsUpdatesBackground()
	{
		_refreshNexusModsUpdatesBackgroundTask?.Dispose();
		_refreshNexusModsUpdatesBackgroundTask = RxApp.TaskpoolScheduler.ScheduleAsync(RefreshNexusModsUpdatesBackgroundAsync);
	}

	private IDisposable _refreshModioUpdatesBackgroundTask;

	private async Task<Unit> RefreshModioUpdatesBackgroundAsync(IScheduler sch, CancellationToken token)
	{
		var results = await _updater.GetModioUpdatesAsync(Settings, GetUpdateableMods(), Version, token);
		await sch.Yield(token);
		if (!token.IsCancellationRequested && results.Count > 0)
		{
			await Observable.Start(() =>
			{
				foreach (var kvp in results)
				{
					if (_manager.TryGetMod(kvp.Key, out var mod))
					{
						//TODO
						var updateData = new DivinityModUpdateData()
						{
							Mod = mod,
							DownloadData = new ModDownloadData()
							{
								DownloadPath = kvp.Value.BinaryUrl?.ToString(),
								DownloadPathType = ModDownloadPathType.URL,
								DownloadSourceType = ModSourceType.MODIO
							},
						};
						ViewModelLocator.ModUpdates.Add(updateData);
					}
				}
			}, RxApp.MainThreadScheduler);
		}
		return Unit.Default;
	}

	public void RefreshModioUpdatesBackground()
	{
		_refreshModioUpdatesBackgroundTask?.Dispose();
		_refreshModioUpdatesBackgroundTask = RxApp.TaskpoolScheduler.ScheduleAsync(RefreshModioUpdatesBackgroundAsync);
	}

	private IDisposable _refreshAllModUpdatesBackgroundTask;

	public void RefreshAllModUpdatesBackground()
	{
		IsRefreshingModUpdates = true;
		_refreshAllModUpdatesBackgroundTask?.Dispose();
		_refreshAllModUpdatesBackgroundTask = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, token) =>
		{
			if (Settings.UpdateSettings.UpdateGitHubMods) await RefreshGitHubModsUpdatesBackgroundAsync(sch, token);
			if (Settings.UpdateSettings.UpdateNexusMods) await RefreshNexusModsUpdatesBackgroundAsync(sch, token);
			if (Settings.UpdateSettings.UpdateModioMods) await RefreshModioUpdatesBackgroundAsync(sch, token);

			IsRefreshingModUpdates = false;
		});
	}

	public void AddImportedMod(DivinityModData mod, bool toActiveList = false)
	{
		mod.ModioEnabled = ModioSupportEnabled;
		mod.NexusModsEnabled = NexusModsSupportEnabled;

		if (_manager.TryGetMod(mod.UUID, out var existingMod) && existingMod.IsActive)
		{
			mod.Index = existingMod.Index;
		}

		_manager.Add(mod);
		UpdateModExtenderStatus(mod);

		if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
		{
			DivinityApp.Log($"Imported Override Mod: {mod}");
			return;
		}

		var entry = mod.ToModInterface();
		if (mod.IsActive)
		{
			var existingInterface = ViewModelLocator.ModOrder.ActiveMods.FirstOrDefault(x => x.UUID == mod.UUID);
			if (existingInterface != null)
			{
				ViewModelLocator.ModOrder.ActiveMods.Replace(existingInterface, entry);
			}
			else
			{
				ViewModelLocator.ModOrder.ActiveMods.Add(entry);
				mod.Index = ViewModelLocator.ModOrder.ActiveMods.Count - 1;
			}
		}
		else
		{
			var existingInterface = ViewModelLocator.ModOrder.InactiveMods.FirstOrDefault(x => x.UUID == mod.UUID);
			if (existingInterface != null)
			{
				ViewModelLocator.ModOrder.InactiveMods.Replace(existingInterface, entry);
			}
			else
			{
				ViewModelLocator.ModOrder.InactiveMods.Add(entry);
			}
		}

		DivinityApp.Log($"Imported Mod: {mod}");
	}

	public async Task ExportLoadOrderToArchiveAsync()
	{
		//view.MainWindowMessageBox.Text = "Add active mods to a zip file?";
		//view.MainWindowMessageBox.Caption = "Depending on the number of mods, this may take some time.";
		var result = await _interactions.ShowMessageBox.Handle(new(
			"Confirm Archive Creation", 
			$"Save active mods to a zip file?{Environment.NewLine}Depending on the number of mods, this may take some time.",
			InteractionMessageBoxType.YesNo));
		if (result)
		{
			Progress.Title = "Adding active mods to zip...";
			Progress.Start(async token =>
			{
				await _importer.ExportLoadOrderToArchiveAsync(ViewModelLocator.ModOrder.SelectedProfile, ViewModelLocator.ModOrder.SelectedModOrder, "", token);
			}, true);
		}
	}

	public async Task ExportLoadOrderToArchiveAsAsync() => await ExportLoadOrderToArchiveAsAsync(ViewModelLocator.ModOrder.SelectedProfile, ViewModelLocator.ModOrder.SelectedModOrder);

	public async Task ExportLoadOrderToArchiveAsAsync(DivinityProfileData? profile = null, DivinityLoadOrder? order = null)
	{
		if (profile != null && order != null)
		{
			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
			var baseOrderName = order.Name;
			if (order.IsModSettings)
			{
				baseOrderName = $"{profile.Name}_{order.Name}";
			}

			var outputName = DivinityModDataLoader.MakeSafeFilename($"{baseOrderName}-{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.zip", '_');

			var result = await _dialogs.SaveFileAsync(new(
				"Export Load Order As...",
				Path.Join(GetInitialStartingDirectory(), outputName),
				CommonFileTypes.ArchiveFileTypes));

			if (result.Success)
			{
				Progress.Title = "Adding active mods to zip...";
				Progress.Start(async token =>
				{
					await _importer.ExportLoadOrderToArchiveAsync(ViewModelLocator.ModOrder.SelectedProfile, ViewModelLocator.ModOrder.SelectedModOrder, result.File, token);
				}, true);
			}
		}
		else
		{
			_globalCommands.ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
		}
	}

	public async Task RenameSaveAsync()
	{
		var profileSavesDirectory = "";
		if (ViewModelLocator.ModOrder.SelectedProfile != null)
		{
			profileSavesDirectory = Path.GetFullPath(Path.Join(ViewModelLocator.ModOrder.SelectedProfile.FilePath, "Savegames"));
		}

		var startPath = "";
		if (ViewModelLocator.ModOrder.SelectedProfile != null)
		{
			var profilePath = Path.GetFullPath(Path.Join(ViewModelLocator.ModOrder.SelectedProfile.FilePath, "Savegames"));
			var storyPath = Path.Join(profilePath, "Story");
			if (Directory.Exists(storyPath))
			{
				startPath = storyPath;
			}
			else
			{
				startPath = profilePath;
			}
		}

		var pickFile = await _dialogs.OpenFileAsync(new(
			"Pick Save to Rename...",
			GetInitialStartingDirectory(startPath),
			[CommonFileTypes.LarianSaveFile]));

		if (pickFile.Success)
		{
			var rootFolder = Path.GetDirectoryName(pickFile.File);
			var rootFileName = Path.GetFileNameWithoutExtension(pickFile.File);
			PathwayData.LastSaveFilePath = rootFolder;

			var initialFilePath = Path.Join(rootFolder, rootFileName + "_1.lsv");
			var renameFile = await _dialogs.SaveFileAsync(new(
				"Rename Save As...",
				initialFilePath,
				[CommonFileTypes.LarianSaveFile], true));

			if (renameFile.Success)
			{
				AppServices.Get<IFileSystemService>()!.EnsureDirectoryExists(rootFolder);

				rootFolder = Path.GetDirectoryName(renameFile.File);
				PathwayData.LastSaveFilePath = rootFolder;
				DivinityApp.Log($"Renaming '{pickFile.File}' to '{renameFile.File}'.");

				if (DivinitySaveTools.RenameSave(pickFile.File, renameFile.File))
				{
					try
					{
						var previewImage = Path.Join(rootFolder, rootFileName + ".WebP");
						var renamedImage = Path.Join(rootFolder, Path.GetFileNameWithoutExtension(renameFile.File) + ".WebP");
						if (File.Exists(previewImage))
						{
							File.Move(previewImage, renamedImage);
							DivinityApp.Log($"Renamed save screenshot '{previewImage}' to '{renamedImage}'.");
						}

						var originalDirectory = Path.GetDirectoryName(pickFile.File);
						var desiredDirectory = Path.GetDirectoryName(renameFile.File);

						if (!String.IsNullOrEmpty(profileSavesDirectory) && FileUtils.IsSubdirectoryOf(profileSavesDirectory, desiredDirectory))
						{
							if (originalDirectory == desiredDirectory)
							{
								var dirInfo = new DirectoryInfo(originalDirectory);
								if (dirInfo.Name.Equals(Path.GetFileNameWithoutExtension(pickFile.File)))
								{
									desiredDirectory = Path.Join(dirInfo.Parent.FullName, Path.GetFileNameWithoutExtension(renameFile.File));
									RecycleBinHelper.DeleteFile(pickFile.File, false, false);
									Directory.Move(originalDirectory, desiredDirectory);
									DivinityApp.Log($"Renamed save folder '{originalDirectory}' to '{desiredDirectory}'.");
								}
							}
						}

						_globalCommands.ShowAlert($"Successfully renamed '{pickFile.File}' to '{renameFile.File}'", AlertType.Success, 15);
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Failed to rename '{pickFile.File}' to '{renameFile.File}':\n" + ex.ToString());
					}
				}
				else
				{
					DivinityApp.Log($"Failed to rename '{pickFile.File}' to '{renameFile.File}'");
				}
			}
		}
	}

	/*public void CheckForUpdates(bool force = false)
	{
		AutoUpdater.ReportErrors = true;
		Settings.LastUpdateCheck = DateTimeOffset.Now.ToUnixTimeSeconds();
		if (!force)
		{
			if (Settings.LastUpdateCheck == -1 || (DateTimeOffset.Now.ToUnixTimeSeconds() - Settings.LastUpdateCheck >= 43200))
			{
				try
				{
					AutoUpdater.Start(DivinityApp.URL_UPDATE);
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error running AutoUpdater:\n{ex}");
				}
			}
		}
		else
		{
			AutoUpdater.Start(DivinityApp.URL_UPDATE);
		}
	}*/

	private bool _userInvokedUpdate = false;

	/*private void OnAppUpdate(UpdateInfoEventArgs e)
	{
		if (_userInvokedUpdate)
		{
			if (e.Error == null)
			{
				if (e.IsUpdateAvailable)
				{
					ShowAlert("Update found!", AlertType.Success, 10);
				}
				else
				{
					ShowAlert("Already up-to-date", AlertType.Info, 10);
				}
			}
			else
			{
				ShowAlert($"Error occurred when checking for updates: {e.Error.Message}", AlertType.Danger, 30);
			}
		}

		if (e.Error == null)
		{
			if (_userInvokedUpdate || e.IsUpdateAvailable)
			{
				App.WM.AppUpdate.Window.ViewModel.CheckArgsAndOpen(e);
			}
		}
		else
		{
			if (e.Error is System.Net.WebException)
			{
				App.WM.Main.Window.DisplayError("Update Check Failed", "There was a problem reaching the update server. Please check your internet connection and try again later.", false);
			}
			else
			{
				App.WM.Main.Window.DisplayError($"Error occurred while checking for updates:\n{e.Error}");
			}
		}

		_userInvokedUpdate = false;
	}*/

	public async Task OnViewActivated(MainWindow window)
	{
		Window = window;

		var canExtractAdventure = ViewModelLocator.ModOrder.WhenAnyValue(x => x.SelectedAdventureMod).Select(x => x != null && !x.IsEditorMod && !x.IsLarianMod);
		//Keys.ExtractSelectedAdventure.AddAsyncAction(ExtractSelectedAdventure, canExtractAdventure);

		ViewModelLocator.DeleteFiles.WhenAnyValue(x => x.IsVisible).ToUIProperty(this, x => x.IsDeletingFiles);
		ViewModelLocator.ModUpdates.WhenAnyValue(x => x.TotalUpdates, total => total > 0).BindTo(this, x => x.ModUpdatesAvailable);

		ViewModelLocator.ModUpdates.CloseView = new Action<bool>((bool refresh) =>
		{
			ViewModelLocator.ModUpdates.Clear();
			//TODO Replace with reloading the individual mods that changed
			if (refresh) Dispatcher.UIThread.Invoke(RefreshStart, DispatcherPriority.Background);
			Window.Activate();
		});

		InitSettingsBindings();

		await LoadSettings();
		//Keys.LoadKeybindings(this);
		//if (Settings.CheckForUpdates) CheckForUpdates();
		SaveSettings();

		AppServices.Get<WindowManagerService>().RestoreSavedWindowPosition();

		ViewModelLocator.CommandBar.CreateCommands(this, ViewModelLocator.ModOrder);

		LoadInitial();
	}

	public void LoadInitial()
	{
		if (!IsInitialized)
		{
			Progress.Title = "Loading...";
			RefreshStart();
		}
	}

	private readonly MainWindowExceptionHandler exceptionHandler;

	private void DeleteMods(IEnumerable<IModEntry> targetMods, bool isDeletingDuplicates = false)
	{
		if (!IsDeletingFiles)
		{
			var targetUUIDs = targetMods.Select(x => x.UUID).ToHashSet();

			List<ModFileDeletionData> deleteFilesData = [];
			foreach (var entry in targetMods)
			{
				var data = ModFileDeletionData.FromModEntry(entry, false, isDeletingDuplicates, AppServices.Mods.AllMods);
				if (data != null)
				{
					deleteFilesData.Add(data);
				}
			}
			ViewModelLocator.DeleteFiles.IsDeletingDuplicates = isDeletingDuplicates;
			ViewModelLocator.DeleteFiles.Files.AddRange(deleteFilesData);

			ViewModelLocator.DeleteFiles.IsVisible = true;
		}
	}

	private async Task ExtractSelectedMods_ChooseFolder()
	{
		var result = await _dialogs.OpenFolderAsync(new(
			"Select folder to extract mod(s) to...",
			GetInitialStartingDirectory(Settings.LastExtractOutputPath)));

		if (result.Success)
		{
			Settings.LastExtractOutputPath = result.File;
			SaveSettings();

			var outputDirectory = result.File;
			DivinityApp.Log($"Extracting selected mods to '{outputDirectory}'.");

			var targetMods = _manager.SelectedPakMods.ToImmutableList();

			var totalWork = targetMods.Count;
			var taskStepAmount = 100d / totalWork;
			Progress.Title = $"Extracting {totalWork} mods...";

			var openOutputPath = result.File;

			var successes = 0;

			var filesToProcess = targetMods.Select(x => x.FilePath);
			Progress.Start(async token =>
			{
				await Parallel.ForEachAsync(filesToProcess, token, async (path, t) =>
				{
					if (t.IsCancellationRequested) return;

					try
					{
						//Put each pak into its own folder
						var pakName = Path.GetFileNameWithoutExtension(path);
						Progress.WorkText += $"Extracting {pakName}...\n";
						var destination = Path.Join(outputDirectory, pakName);

						//In case the foldername == the pak name and we're only extracting one pak
						if (totalWork == 1 && Path.GetDirectoryName(outputDirectory).Equals(pakName))
						{
							destination = outputDirectory;
						}
						var success = await FileUtils.ExtractPackageAsync(path, destination, token);
						if (success)
						{
							successes += 1;
							if (totalWork == 1)
							{
								openOutputPath = destination;
							}
						}
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error extracting package: {ex}");
					}
					Progress.Value += taskStepAmount;
				});
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					if (successes >= totalWork)
					{
						_globalCommands.ShowAlert($"Successfully extracted all selected mods to '{result.File}'", AlertType.Success, 20);
						FileUtils.TryOpenPath(openOutputPath);
					}
					else
					{
						_globalCommands.ShowAlert($"Error occurred when extracting selected mods to '{result.File}'", AlertType.Danger, 30);
					}
				});
			}, true);
		}
	}

	private async Task ExtractSelectedMods_Start()
	{
		//var selectedMods = Mods.Where(x => x.IsSelected && !x.IsEditorMod).ToList();

		if (_manager.SelectedPakMods.Count == 1)
		{
			await ExtractSelectedMods_ChooseFolder();
		}
		else
		{
			var msg = $"Extract the following mods?\n'{String.Join("\n", _manager.SelectedPakMods.Select(x => $"{x.DisplayName}"))}";
			var result = await _interactions.ShowMessageBox.Handle(new("Extract Mods?", msg, InteractionMessageBoxType.YesNo));
			if (result)
			{
				await ExtractSelectedMods_ChooseFolder();
			}
		}
	}

	private async Task ExtractSelectedAdventure()
	{
		var mod = ViewModelLocator.ModOrder.SelectedAdventureMod;

		if (mod == null || mod.IsEditorMod || mod.IsLarianMod || !File.Exists(mod.FilePath))
		{
			var displayName = mod != null ? mod.DisplayName : "";
			_globalCommands.ShowAlert($"Current adventure mod '{displayName}' is not extractable", AlertType.Warning, 30);
			return;
		}

		var result = await _dialogs.OpenFolderAsync(new("Select folder to extract mod to...",
			GetInitialStartingDirectory(Settings.LastExtractOutputPath)));

		if (result.Success)
		{
			Settings.LastExtractOutputPath = result.File;
			SaveSettings();

			var outputDirectory = result.File;
			DivinityApp.Log($"Extracting adventure mod to '{outputDirectory}'.");

			Progress.Title = $"Extracting {mod.DisplayName}...";

			var openOutputPath = result.File;

			var path = mod.FilePath;
			var success = false;

			Progress.Start(async token =>
			{
				try
				{
					var pakName = Path.GetFileNameWithoutExtension(path);
					Progress.WorkText = $"Extracting {pakName}...";
					var destination = Path.Join(outputDirectory, pakName);
					if (Path.GetDirectoryName(outputDirectory).Equals(pakName))
					{
						destination = outputDirectory;
					}
					openOutputPath = destination;
					success = await FileUtils.ExtractPackageAsync(path, destination, token);
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error extracting package: {ex}");
				}

				RxApp.MainThreadScheduler.Schedule(() =>
				{
					if (success)
					{
						_globalCommands.ShowAlert($"Successfully extracted adventure mod to '{result.File}'", AlertType.Success, 20);
						FileUtils.TryOpenPath(openOutputPath);
					}
					else
					{
						_globalCommands.ShowAlert($"Error occurred when extracting adventure mod to '{result.File}'", AlertType.Danger, 30);
					}
				});
			}, true);
		}
	}

	private void LoadAppConfig()
	{
		AppSettingsLoaded = false;
		if (!_settings.TryLoadAppSettings(out var ex))
		{
			_globalCommands.ShowAlert($"Error loading app settings: {ex.Message}", AlertType.Danger);
			return;
		}
		AppSettingsLoaded = true;
	}

	private static string NexusModsLimitToText(NexusModsObservableApiLimits limits)
	{
		return $"NexusMods Limits [Hourly ({limits.HourlyRemaining}/{limits.HourlyLimit}) Daily ({limits.DailyRemaining}/{limits.DailyLimit})]";
	}

	public MainWindowViewModel(
		IPathwaysService pathwaysService,
		ISettingsService settingsService,
		ModImportService modImportService,
		IModManagerService modManagerService,
		IModUpdaterService modUpdaterService,
		INexusModsService nexusModsService,
		IInteractionsService interactionsService,
		IEnvironmentService environmentService,
		IGlobalCommandsService globalCommands,
		IDialogService dialogsService
		)
	{
		_importer = modImportService;
		_pathways = pathwaysService;
		_manager = modManagerService;
		_updater = modUpdaterService;
		_settings = settingsService;
		_nexusMods = nexusModsService;
		_interactions = interactionsService;
		_environment = environmentService;
		_dialogs = dialogsService;
		_globalCommands = globalCommands;

		//var canCancelProgress = RefreshCommand.IsExecuting.CombineLatest(this.WhenAnyValue(x => x.CanCancelProgress)).Select(x => x.First && x.Second);

		//CancelMainProgressCommand = ReactiveCommand.Create(() =>
		//{
		//	if (MainProgressToken != null && token.CanBeCanceled)
		//	{
		//		token.Register(() => { MainProgressIsActive = IsRefreshing = false; });
		//		MainProgressToken.Cancel();
		//	}
		//}, canCancelProgress);

		//_keys = new AppKeys(this);
		//_keys.SaveDefaultKeybindings();

		Router = new RoutingState();

		//var progressIsActive = this.WhenAnyValue(x => x.Router.CurrentViewModel).OfType<IProgressBarViewModel>();
		this.WhenAnyValue(x => x.IsRefreshing, x => x.IsDragging)
			//.CombineLatest(progressIsActive)
			.Select(x => x.Item1 || x.Item2)
			.BindTo(this, x => x.IsLocked);
		this.WhenAnyValue(x => x.IsLocked, x => x.IsInitialized, (b1, b2) => !b1 && b2).BindTo(this, x => x.AllowDrop);

		interactionsService.ShowAlert.RegisterHandler(input =>
		{
			return Observable.Start(() =>
			{
				var data = input.Input;
				//ShowAlert(data.Message, data.AlertType, data.Timeout);
				input.SetOutput(true);
			}, RxApp.MainThreadScheduler);
		});

		interactionsService.DeleteMods.RegisterHandler(input =>
		{
			return Observable.Start(() =>
			{
				var data = input.Input;
				DeleteMods(data.TargetMods, data.IsDeletingDuplicates);
				input.SetOutput(true);
			}, RxApp.MainThreadScheduler);
		});

		DownloadBar = new DownloadActivityBarViewModel();

		_settings.WhenAnyValue(x => x.AppSettings).BindTo(this, x => x.AppSettings);
		_settings.WhenAnyValue(x => x.ManagerSettings).BindTo(this, x => x.Settings);

		exceptionHandler = new MainWindowExceptionHandler(this);
		RxApp.DefaultExceptionHandler = exceptionHandler;

		Version = environmentService.AppVersion.ToString();
		Title = $"{environmentService.AppProductName} {Version}";
		DivinityApp.Log($"{Title} initializing...");

		_nexusMods.WhenLimitsChange.Throttle(TimeSpan.FromMilliseconds(50)).Select(NexusModsLimitToText).ToUIProperty(this, x => x.NexusModsLimitsText);
		var whenNexusModsAvatar = _nexusMods.WhenAnyValue(x => x.ProfileAvatarUrl);
		whenNexusModsAvatar.Select(x => x.IsValid()).ToUIProperty(this, x => x.NexusModsProfileAvatarVisibility);
		whenNexusModsAvatar.ToUIProperty(this, x => x.NexusModsProfileBitmapUri);

		_nexusMods.WhenAnyValue(x => x.DownloadProgressValue, x => x.DownloadProgressText, x => x.CanCancel).Subscribe(x =>
		{
			DownloadBar.UpdateProgress(x.Item1, x.Item2);
			if (x.Item3)
			{
				DownloadBar.CancelAction = () => _nexusMods.CancelDownloads();
			}
			else
			{
				DownloadBar.CancelAction = null;
			}
		});

		this.WhenAnyValue(x => x.Settings.UpdateSettings.NexusModsAPIKey).BindTo(nexusModsService, x => x.ApiKey);

		IDisposable? importDownloadsTask = null;
		_nexusMods.DownloadResults
		.ToObservableChangeSet()
		.CountChanged()
		.ThrottleFirst(TimeSpan.FromMilliseconds(50))
		.Subscribe(f =>
		{
			importDownloadsTask?.Dispose();
			importDownloadsTask = RxApp.TaskpoolScheduler.ScheduleAsync(TimeSpan.FromMilliseconds(250), async (sch, token) =>
			{
				var files = _nexusMods.DownloadResults.ToList();
				_nexusMods.DownloadResults.Clear();

				var result = new ImportOperationResults()
				{
					TotalFiles = files.Count
				};
				var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
				foreach (var filePath in files)
				{
					await _importer.ImportModFromFile(builtinMods, result, filePath, token, false);
				}

				if (result.Mods.Count > 0 && result.Mods.Any(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START))
				{
					await _updater.NexusMods.Update(result.Mods, token);
					await _updater.NexusMods.SaveCacheAsync(false, Version, token);

					await Observable.Start(() =>
					{
						var total = result.Mods.Count;
						if (result.Success)
						{
							if (result.Mods.Count > 1)
							{
								_globalCommands.ShowAlert($"Successfully imported {total} downloaded mods", AlertType.Success, 20);
							}
							else if (total == 1)
							{
								var modFileName = result.Mods.First().FileName;
								var fileNames = String.Join(", ", files.Select(x => Path.GetFileName(x)));
								_globalCommands.ShowAlert($"Successfully imported '{modFileName}' from '{fileNames}'", AlertType.Success, 20);
							}
							else
							{
								_globalCommands.ShowAlert("Skipped importing mod - No .pak file found", AlertType.Success, 20);
							}
						}
						else
						{
							if (total == 0)
							{
								_globalCommands.ShowAlert("No mods imported. Does the file contain a .pak?", AlertType.Warning, 60);
							}
							else
							{
								_globalCommands.ShowAlert($"Only imported {total}/{result.TotalPaks} mods - Check the log", AlertType.Danger, 60);
							}
						}
					}, RxApp.MainThreadScheduler);
				}
			});
		});

		this.WhenAnyValue(x => x.AppSettings.Features.GitHub, x => x.Settings.UpdateSettings.UpdateGitHubMods).Select(x => x.Item1 && x.Item2).BindTo(_updater.GitHub, x => x.IsEnabled);
		this.WhenAnyValue(x => x.AppSettings.Features.NexusMods, x => x.Settings.UpdateSettings.UpdateNexusMods).Select(x => x.Item1 && x.Item2).BindTo(_updater.NexusMods, x => x.IsEnabled);
		this.WhenAnyValue(x => x.AppSettings.Features.Modio, x => x.Settings.UpdateSettings.UpdateModioMods).Select(x => x.Item1 && x.Item2).BindTo(_updater.Modio, x => x.IsEnabled);

		_updater.Modio.WhenAnyValue(x => x.IsEnabled).ToUIProperty(this, x => x.ModioSupportEnabled);
		_updater.NexusMods.WhenAnyValue(x => x.IsEnabled).ToUIProperty(this, x => x.NexusModsSupportEnabled);
		_updater.GitHub.WhenAnyValue(x => x.IsEnabled).ToUIProperty(this, x => x.GitHubModSupportEnabled);

		_updater.WhenAnyValue(x => x.GitHub.IsEnabled, x => x.NexusMods.IsEnabled, x => x.Modio.IsEnabled)
		.Throttle(TimeSpan.FromMilliseconds(250))
		.ObserveOn(RxApp.MainThreadScheduler)
		.Subscribe(x =>
		{
			foreach (var mod in _manager.AllMods)
			{
				mod.GitHubEnabled = x.Item1;
				mod.NexusModsEnabled = x.Item2;
				mod.ModioEnabled = x.Item3;
			}
		});

		var whenRefreshing = _updater.WhenAnyValue(x => x.IsRefreshing);
		whenRefreshing.ToUIProperty(this, x => x.UpdatingBusyIndicatorVisibility);
		whenRefreshing.Select(b => !b).ToUIProperty(this, x => x.UpdateCountVisibility);

		this.WhenAnyValue(x => x.Settings.DebugModeEnabled, x => x.Settings.ExtenderSettings.DeveloperMode)
		.Select(x => x.Item1 || x.Item2).ToUIProperty(this, x => x.DeveloperModeVisibility);

		#region Keys Setup

		//Keys.OpenCollectionDownloaderWindow.AddAction(() => App.WM.CollectionDownload.Toggle(true));

		//var canStartExport = this.WhenAny(x => x.MainProgressToken, (t) => t != null);
		//Keys.ExportOrderToZip.AddAsyncAction(ExportLoadOrderToArchiveAsync, canStartExport);
		//Keys.ExportOrderToArchiveAs.AddAsyncAction(ExportLoadOrderToArchiveAsAsync, canStartExport);

		/*Keys.OpenDonationLink.AddAction(() =>
		{
			FileUtils.TryOpenPath(DivinityApp.URL_DONATION);
		});

		Keys.OpenRepositoryPage.AddAction(() =>
		{
			FileUtils.TryOpenPath(DivinityApp.URL_REPO);
		});

		Keys.ToggleViewTheme.AddAction(() =>
		{
			Settings.DarkThemeEnabled = !Settings.DarkThemeEnabled;
		});

		Keys.ToggleFileNameDisplay.AddAction(() =>
		{
			Settings.DisplayFileNames = !Settings.DisplayFileNames;

			foreach (var m in _manager.AllMods)
			{
				m.DisplayFileForName = Settings.DisplayFileNames;
			}
		});

		var canDownloadNexusFiles = this.WhenAnyValue(x => x.Settings.UpdateSettings.NexusModsAPIKey, x => x.NexusModsSupportEnabled)
			.Select(x => !String.IsNullOrEmpty(x.Item1) && x.Item2);
		Keys.DownloadNXMLink.AddCanExecuteCondition(canDownloadNexusFiles);
		Keys.DownloadNXMLink.AddAction(() =>
		{
			//App.WM.NxmDownload.Toggle();
		});*/

		#endregion

		Router.CurrentViewModel.Select(x => x == ViewModelLocator.ModUpdates).ToUIProperty(this, x => x.UpdatesViewIsVisible, false);

		DivinityApp.Events.OrderNameChanged += OnOrderNameChanged;

		// Blinky animation on the tools/download buttons if the extender is required by mods and is missing
		if (AppSettings.Features.ScriptExtender)
		{
			_manager.ModsConnection.ObserveOn(RxApp.MainThreadScheduler).AutoRefresh(x => x.ExtenderModStatus).
				Filter(x => x.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING || x.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED).
				Select(x => x.Count).Subscribe(totalWithRequirements =>
				{
					if (totalWithRequirements > 0)
					{
						HighlightExtenderDownload = !Settings.ExtenderUpdaterSettings.UpdaterIsAvailable;
					}
					else
					{
						HighlightExtenderDownload = false;
					}
				});
		}

		var anyPakModSelectedObservable = _manager.SelectedPakMods.ToObservableChangeSet().CountChanged().Select(x => _manager.SelectedPakMods.Count > 0);
		//Keys.ExtractSelectedMods.AddAsyncAction(ExtractSelectedMods_Start, anyPakModSelectedObservable);

		_interactions.ConfirmModDeletion.RegisterHandler(async interaction =>
		{
			var sentenceStart = interaction.Input.PermanentlyDelete ? "Permanently delete" : "Delete";
			var msg = $"{sentenceStart} {interaction.Input.Total} mod file(s)?";

			var result = await _interactions.ShowMessageBox.Handle(new("Confirm Mod Deletion", msg, InteractionMessageBoxType.YesNo));
			interaction.SetOutput(result);
		});

		_interactions.OpenModProperties.RegisterHandler(interaction =>
		{
			var modPropertiesWindow = AppServices.Get<ModPropertiesWindow>();
			if(modPropertiesWindow?.ViewModel != null)
			{
				modPropertiesWindow.ViewModel.SetMod(interaction.Input);
				if (!modPropertiesWindow.IsVisible) modPropertiesWindow.Show(Window);
				interaction.SetOutput(true);
			}
			else
			{
				interaction.SetOutput(false);
			}
		});

		Views = new ViewManager(Router);

		Views.WhenAnyValue(x => x.CurrentView).Subscribe(vm =>
		{
			DivinityApp.Log($"VM: {vm} | {Router}");
		});
	}
}
