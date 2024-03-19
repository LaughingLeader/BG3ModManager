using AutoUpdaterDotNET;

using DivinityModManager.AppServices;
using DivinityModManager.Extensions;
using DivinityModManager.Models;
using DivinityModManager.Models.App;
using DivinityModManager.Models.Mod;
using DivinityModManager.Models.NexusMods;
using DivinityModManager.Models.Settings;
using DivinityModManager.Models.Updates;
using DivinityModManager.ModUpdater.Cache;
using DivinityModManager.Util;
using DivinityModManager.ViewModels.Main;
using DivinityModManager.Views.Main;
using DivinityModManager.Windows;

using DynamicData;
using DynamicData.Binding;

using Microsoft.Win32;

using Newtonsoft.Json.Linq;

using Reactive.Bindings.Extensions;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DivinityModManager.ViewModels;

public class MainWindowViewModel : BaseHistoryViewModel, IScreen
{
	private const int ARCHIVE_BUFFER = 128000;

    public RoutingState Router { get; }
	public ViewManager Views { get; }
	public DivinityPathwayData PathwayData { get; }
	public ModImportService ModImporter { get; }

    [Reactive] public MainWindow Window { get; private set; }
	[Reactive] public MainViewControl View { get; private set; }
	public DownloadActivityBarViewModel DownloadBar { get; private set; }

	private readonly IModUpdaterService _updater;
	private readonly ISettingsService _settings;

	[Reactive] public string Title { get; set; }
	[Reactive] public string Version { get; set; }

	private readonly AppKeys _keys;
	public AppKeys Keys => _keys;

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

	#region Progress
	[Reactive] public string MainProgressTitle { get; set; }
	[Reactive] public string MainProgressWorkText { get; set; }
	[Reactive] public bool MainProgressIsActive { get; set; }
	[Reactive] public double MainProgressValue { get; set; }

	public void IncreaseMainProgressValue(double val, string message = "")
	{
		RxApp.MainThreadScheduler.Schedule(_ =>
		{
			MainProgressValue += val;
			if (!String.IsNullOrEmpty(message)) MainProgressWorkText = message;
		});
	}

	public async Task<Unit> IncreaseMainProgressValueAsync(double val, string message = "")
	{
		return await Observable.Start(() =>
		{
			MainProgressValue += val;
			if (!String.IsNullOrEmpty(message)) MainProgressWorkText = message;
			return Unit.Default;
		}, RxApp.MainThreadScheduler);
	}

	public async Task<Unit> SetMainProgressTextAsync(string text)
	{
		return await Observable.Start(() =>
		{
			MainProgressWorkText = text;
			return Unit.Default;
		}, RxApp.MainThreadScheduler);
	}

	public void StartMainProgress(string title)
	{
		MainProgressToken?.Dispose();
		MainProgressToken = new CancellationTokenSource();
		MainProgressTitle = title;
		MainProgressWorkText = "";
		MainProgressValue = 0d;
		MainProgressIsActive = true;
	}

	public async Task StartMainProgressAsync(string title)
	{
		if (MainProgressIsActive) DivinityApp.Log("[Warning] Main progress is already active?");
		await Observable.Start(() =>
		{
			StartMainProgress(title);
		}, RxApp.MainThreadScheduler);
	}

	[Reactive] public CancellationTokenSource MainProgressToken { get; set; }
	[Reactive] public bool CanCancelProgress { get; set; }

	#endregion
	[ObservableAsProperty] public bool UpdatesViewIsVisible { get; }

	[Reactive] public Visibility StatusBarBusyIndicatorVisibility { get; set; }
	[ObservableAsProperty] public bool GitHubModSupportEnabled { get; }
	[ObservableAsProperty] public bool NexusModsSupportEnabled { get; }
	[ObservableAsProperty] public bool SteamWorkshopSupportEnabled { get; }
	[ObservableAsProperty] public string NexusModsLimitsText { get; }
	[ObservableAsProperty] public BitmapImage NexusModsProfileBitmapImage { get; }
	[ObservableAsProperty] public Visibility NexusModsProfileAvatarVisibility { get; }
	[ObservableAsProperty] public Visibility UpdatingBusyIndicatorVisibility { get; }
	[ObservableAsProperty] public Visibility UpdateCountVisibility { get; }
	[ObservableAsProperty] public Visibility DeveloperModeVisibility { get; }

	public ReactiveCommand<Unit, Task> RefreshCommand { get; }
	public ReactiveCommand<Unit, Unit> CancelMainProgressCommand { get; }
	public ICommand ToggleUpdatesViewCommand { get; private set; }
	public ICommand CheckForAppUpdatesCommand { get; set; }
	public ReactiveCommand<UpdateInfoEventArgs, Unit> OnAppUpdateCheckedCommand { get; set; }
	public ICommand RenameSaveCommand { get; private set; }
	public ICommand SaveSettingsSilentlyCommand { get; private set; }
	public ReactiveCommand<Unit, Unit> RefreshModUpdatesCommand { get; private set; }
	public ICommand CheckForGitHubModUpdatesCommand { get; private set; }
	public ICommand CheckForNexusModsUpdatesCommand { get; private set; }
	public ICommand CheckForSteamWorkshopUpdatesCommand { get; private set; }
	public EventHandler OnRefreshed { get; set; }

	public bool DebugMode { get; set; }

	private async Task RefreshAsync()
	{
		DivinityApp.Log("Refreshing...");
		await StartMainProgressAsync(!IsInitialized ? "Loading..." : "Refreshing...");

		var token = MainProgressToken.Token;

		await Observable.Start(() =>
		{
			IsRefreshing = true;
			CanCancelProgress = false;
			Services.Mods.Refresh();
			Views.ModUpdates.Clear();
			Services.Get<ModOrderView>()?.ModLayout.SaveLayout();
			ModUpdatesAvailable = false;
			Window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
			Window.TaskbarItemInfo.ProgressValue = 0;

			LoadAppConfig();
		}, RxApp.MainThreadScheduler);

		await Task.Delay(250, token);

		await Views.ModOrder.RefreshAsync(this, token);

		await _updater.LoadCacheAsync(Services.Mods.AllMods, Version, token);

		await Observable.Start(() =>
		{
			OnMainProgressComplete();

			if (AppSettings.Features.ScriptExtender)
			{
				LoadExtenderSettingsBackground();
			}

			if (!GameDirectoryFound)
			{
				ShowAlert("Game Data folder is not valid. Please set it in the preferences window and refresh", AlertType.Danger);
				App.WM.Settings.Toggle(true);
			}

			IsRefreshing = false;

			RefreshModUpdatesCommand.Execute().Subscribe();
		}, RxApp.MainThreadScheduler);
	}

	private void DownloadScriptExtender(string exeDir)
	{
		var isLoggingEnabled = Window.DebugLogListener != null;
		if (!isLoggingEnabled) Window.ToggleLogging(true);

		double taskStepAmount = 1.0 / 3;
		StartMainProgress("Setting up the Script Extender...");
		CanCancelProgress = true;

		string dllDestination = Path.Join(exeDir, DivinityApp.EXTENDER_UPDATER_FILE);

		RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
		{
			int successes = 0;
			System.IO.Stream webStream = null;
			System.IO.Stream unzippedEntryStream = null;
			try
			{
				await SetMainProgressTextAsync($"Downloading {PathwayData.ScriptExtenderLatestReleaseUrl}");
				webStream = await WebHelper.DownloadFileAsStreamAsync(PathwayData.ScriptExtenderLatestReleaseUrl, MainProgressToken.Token);
				if (webStream != null)
				{
					successes += 1;
					await IncreaseMainProgressValueAsync(taskStepAmount, $"Extracting zip to {exeDir}...");
					using var archive = new ZipArchive(webStream);
					foreach (var entry in archive.Entries)
					{
						if (MainProgressToken.IsCancellationRequested) break;
						if (entry.Name.Equals(DivinityApp.EXTENDER_UPDATER_FILE, StringComparison.OrdinalIgnoreCase))
						{
							unzippedEntryStream = entry.Open(); // .Open will return a stream
							using var fs = File.Create(dllDestination, ARCHIVE_BUFFER, System.IO.FileOptions.Asynchronous);
							await unzippedEntryStream.CopyToAsync(fs, ARCHIVE_BUFFER, MainProgressToken.Token);
							successes += 1;
							break;
						}
					}
					await IncreaseMainProgressValueAsync(taskStepAmount);
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error downloading the script extender: {ex}");
			}
			finally
			{
				await SetMainProgressTextAsync("Cleaning up...");
				webStream?.Dispose();
				unzippedEntryStream?.Dispose();
				successes += 1;
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}

			await Observable.Start(() =>
			{
				OnMainProgressComplete();
				if (successes >= 3)
				{
					ShowAlert($"Successfully installed the Extender updater {DivinityApp.EXTENDER_UPDATER_FILE} to '{exeDir}'", AlertType.Success, 20);
					HighlightExtenderDownload = false;
					Settings.ExtenderUpdaterSettings.UpdaterIsAvailable = true;
				}
				else
				{
					ShowAlert($"Error occurred when installing the Extender updater {DivinityApp.EXTENDER_UPDATER_FILE} - Check the log", AlertType.Danger, 30);
				}
			}, RxApp.MainThreadScheduler);

			if (Settings.ExtenderUpdaterSettings.UpdaterIsAvailable)
			{
				await LoadExtenderSettingsAsync(t);
				await Observable.Start(() => UpdateExtender(true), RxApp.TaskpoolScheduler);
			}

			if (!isLoggingEnabled) await Observable.Start(() => Window.ToggleLogging(false), RxApp.MainThreadScheduler);

			return Disposable.Empty;
		});
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
				string exeDir = Path.GetDirectoryName(Settings.GameExecutablePath);
				string messageText = String.Format(@"Download and install the Script Extender?
The Script Extender is used by mods to extend the scripting language of the game, allowing new functionality.
The extender needs to only be installed once, as it automatically updates when you launch the game.
Download url: 
{0}
Directory the zip will be extracted to:
{1}", PathwayData.ScriptExtenderLatestReleaseUrl, exeDir);

				var result = Xceed.Wpf.Toolkit.MessageBox.Show(Window,
				messageText,
				"Download & Install the Script Extender?",
				MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No, Window.MessageBoxStyle);

				if (result == MessageBoxResult.Yes)
				{
					DownloadScriptExtender(exeDir);
				}
			}
			else
			{
				ShowAlert("The 'Game Executable Path' is not set or is not valid", AlertType.Danger);
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
		string extenderUpdaterPath = Path.Join(Path.GetDirectoryName(Settings.GameExecutablePath), DivinityApp.EXTENDER_UPDATER_FILE);
		DivinityApp.Log($"Looking for Script Extender at '{extenderUpdaterPath}'.");
		if (File.Exists(extenderUpdaterPath))
		{
			DivinityApp.Log($"Checking {DivinityApp.EXTENDER_UPDATER_FILE} for Script Extender ASCII bytes.");
			try
			{
				FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(extenderUpdaterPath);
				if (fvi != null && fvi.ProductName.IndexOf("Script Extender", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					Settings.ExtenderUpdaterSettings.UpdaterIsAvailable = true;
					DivinityApp.Log($"Found the Extender at '{extenderUpdaterPath}'.");
					FileVersionInfo extenderInfo = FileVersionInfo.GetVersionInfo(extenderUpdaterPath);
					if (!String.IsNullOrEmpty(extenderInfo.FileVersion))
					{
						var version = extenderInfo.FileVersion.Split('.')[0];
						if (int.TryParse(version, out int intVersion))
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
			int majorVersion = -1;
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
			string latestReleaseZipUrl = "";
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
		var isLoggingEnabled = Window.DebugLogListener != null;
		if (!isLoggingEnabled) Window.ToggleLogging(true);

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
				Services.Get<IGameUtilitiesService>().CheckForGameProcess();
			});
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error starting game exe:\n{ex}");
			ShowAlert("Error occurred when trying to start the game - Check the log", AlertType.Danger);
		}

		if (!isLoggingEnabled) Window.ToggleLogging(false);
	}

	private void LaunchGame()
	{
		if (Settings.DisableLauncherTelemetry || Settings.DisableLauncherModWarnings)
		{
			RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, t) =>
			{
				await DivinityModDataLoader.UpdateLauncherPreferencesAsync(Services.Pathways.GetLarianStudiosAppDataFolder(), !Settings.DisableLauncherTelemetry, !Settings.DisableLauncherModWarnings);
			});
		}

		if (!Settings.LaunchThroughSteam)
		{
			if (!File.Exists(Settings.GameExecutablePath))
			{
				if (String.IsNullOrWhiteSpace(Settings.GameExecutablePath))
				{
					ShowAlert("No game executable path set", AlertType.Danger, 30);
				}
				else
				{
					ShowAlert($"Failed to find game exe at, \"{Settings.GameExecutablePath}\"", AlertType.Danger, 90);
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
			var isModded = Views.ModOrder.ActiveMods.Count > 0 ? 1 : 0;
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

		if (Settings.ActionOnGameLaunch != DivinityGameLaunchWindowAction.None)
		{
			switch (Settings.ActionOnGameLaunch)
			{
				case DivinityGameLaunchWindowAction.Minimize:
					Window.WindowState = WindowState.Minimized;
					break;
				case DivinityGameLaunchWindowAction.Close:
					App.Current.Shutdown();
					break;
			}
		}
	}

	private bool CanLaunchGameCheck(ValueTuple<string, bool, bool, bool> x) => x.Item1.IsExistingFile() && (!x.Item2 || !x.Item3 || x.Item4);

	private void InitSettingsBindings()
	{
		var canOpenWorkshopFolder = this.WhenAnyValue(x => x.SteamWorkshopSupportEnabled, x => x.Settings.WorkshopPath,
			(b, p) => (b && !String.IsNullOrEmpty(p) && Directory.Exists(p))).StartWith(false);

		var gameUtils = Services.Get<IGameUtilitiesService>();
		gameUtils.WhenAnyValue(x => x.GameIsRunning).BindTo(this, x => x.GameIsRunning);

		Settings.WhenAnyValue(x => x.GameExecutablePath).Subscribe(path =>
		{
			if (!String.IsNullOrEmpty(path)) gameUtils.AddGameProcessName(Path.GetFileNameWithoutExtension(path));
		});

		var whenGameExeProperties = this.WhenAnyValue(x => x.Settings.GameExecutablePath, x => x.Settings.LimitToSingleInstance, x => x.GameIsRunning, x => x.CanForceLaunchGame);
		var canOpenGameExe = whenGameExeProperties.Select(CanLaunchGameCheck);
		canOpenGameExe.ToUIProperty(this, x => x.CanLaunchGame);

		Keys.LaunchGame.AddAction(LaunchGame, canOpenGameExe);

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
		}, canOpenWorkshopFolder);

		Settings.WhenAnyValue(x => x.LogEnabled).Subscribe((logEnabled) =>
		{
			Window.ToggleLogging(logEnabled);
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
			if (View != null && View.MenuItems.TryGetValue("ToggleFileNameDisplay", out var menuItem))
			{
				if (b)
				{
					menuItem.Header = "Show Display Names for Mods";
				}
				else
				{
					menuItem.Header = "Show File Names for Mods";
				}
			}
		});

		Settings.WhenAnyValue(x => x.DocumentsFolderPathOverride).Skip(1).Subscribe((x) =>
		{
			if (!IsLocked)
			{
				Services.Pathways.SetGamePathways(Settings.GameDataPath, x);
				if (AppSettings.Features.ScriptExtender && IsInitialized && !IsRefreshing)
				{
					LoadExtenderSettingsBackground();
				}
				ShowAlert($"Larian folder changed to '{x}' - Make sure to refresh", AlertType.Warning, 60);
			}
		});

		Settings.WhenAnyValue(x => x.SaveWindowLocation).Subscribe(Window.ToggleWindowPositionSaving);
	}

	private void OnOrderNameChanged(object sender, OrderNameChangedArgs e)
	{
		if (Settings.LastOrder == e.LastName)
		{
			Settings.LastOrder = e.NewName;
			QueueSave();
		}
	}

	private bool LoadSettings()
	{
		var success = true;
		if (!_settings.TryLoadAll(out var errors))
		{
			var errorMessage = String.Join("\n", errors.Select(x => x.ToString()));
			ShowAlert($"Error saving settings: {errorMessage}", AlertType.Danger);
			success = false;
		}

		LoadAppConfig();

#if DOS2
		Settings.DefaultExtenderLogDirectory = Path.Join(Services.Pathways.GetLarianStudiosAppDataFolder(), "Divinity Original Sin 2 Definitive Edition", "Extender Logs");
#else
		Settings.DefaultExtenderLogDirectory = Path.Join(Services.Pathways.GetLarianStudiosAppDataFolder(), "Baldur's Gate 3", "Extender Logs");
#endif

		var githubSupportEnabled = AppSettings.Features.GitHub;
		var nexusModsSupportEnabled = AppSettings.Features.NexusMods;
		var workshopSupportEnabled = AppSettings.Features.SteamWorkshop;

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

		if (Settings.LogEnabled)
		{
			Window.ToggleLogging(true);
		}

		if(!Services.Pathways.SetGamePathways(Settings.GameDataPath, Settings.DocumentsFolderPathOverride))
		{
			GameDirectoryFound = false;

			if(!FileUtils.HasReadPermission(Settings.GameDataPath, Settings.DocumentsFolderPathOverride))
			{
				var message = $"BG3MM lacks permission to read one or both of the following paths:\nGame Data Path: ({Settings.GameDataPath})\nGame Executable Path: ({Settings.GameExecutablePath})";
				var result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, message, "File Permission Issue",
				MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, Window.MessageBoxStyle);
			}
			else
			{
				var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog()
				{
					Multiselect = false,
					Description = "Set the path to the Baldur's Gate 3 root installation folder",
					UseDescriptionForTitle = true,
					SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer)
				};

				if (dialog.ShowDialog(Window) == true)
				{
					var data = PathwayData;
					var dir = dialog.SelectedPath;
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
						DivinityApp.ShowAlert("Failed to find Data folder with given installation directory", AlertType.Danger);
					}
					if (File.Exists(exePath))
					{
						Settings.GameExecutablePath = exePath;
					}
					else
					{
						DivinityApp.ShowAlert("Failed to find bg3.exe path with given installation directory", AlertType.Danger);
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

	public bool SaveSettings()
	{
		var success = true;
		if (!_settings.TrySaveAll(out var errors))
		{
			var errorMessage = String.Join("\n", errors.Select(x => x.ToString()));
			ShowAlert($"Error saving settings: {errorMessage}", AlertType.Danger);
			success = false;
		}
		else
		{
			Settings.CanSaveSettings = false;
			if (!Keys.SaveKeybindings(out var errorMsg))
			{
				ShowAlert(errorMsg, AlertType.Danger);
				success = false;
			}
		}
		return success;
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

	private void MainWindowMessageBox_Closed_ResetColor(object sender, EventArgs e)
	{
		if (sender is Xceed.Wpf.Toolkit.MessageBox messageBox)
		{
			messageBox.WindowBackground = new SolidColorBrush(Color.FromRgb(78, 56, 201));
			messageBox.Closed -= MainWindowMessageBox_Closed_ResetColor;
		}
	}

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
		foreach (var mod in Services.Mods.AllMods)
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
		var settingsService = Services.Get<ISettingsService>();
		var minUpdateTime = Settings.UpdateSettings.MinimumUpdateTimePeriod;
		if (minUpdateTime > TimeSpan.Zero)
		{
			var now = DateTime.Now;
			return Services.Mods.UserMods.Where(x => CanUpdateMod(x, now, minUpdateTime, settingsService)).ToList();
		}
		return Services.Mods.UserMods;
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
				var modManager = Services.Mods;
				foreach (var kvp in results)
				{
					if (modManager.TryGetMod(kvp.Key, out var mod))
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
						Views.ModUpdates.Add(updateData);
					}
				}
			}, RxApp.MainThreadScheduler);
		}
		return Unit.Default;
	}

	private void RefreshGitHubModsUpdatesBackground()
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
			var isPremium = Services.Get<INexusModsService>().IsPremium;
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
					Views.ModUpdates.Add(updateData);
				}
			}, RxApp.MainThreadScheduler);
		}
		return Unit.Default;
	}

	private void RefreshNexusModsUpdatesBackground()
	{
		_updater.NexusMods.APIKey = Settings.UpdateSettings.NexusModsAPIKey;
		_updater.NexusMods.AppName = AutoUpdater.AppTitle;
		_updater.NexusMods.AppVersion = Version;

		_refreshNexusModsUpdatesBackgroundTask?.Dispose();
		_refreshNexusModsUpdatesBackgroundTask = RxApp.TaskpoolScheduler.ScheduleAsync(RefreshNexusModsUpdatesBackgroundAsync);
	}

	private IDisposable _refreshSteamWorkshopUpdatesBackgroundTask;

	private async Task<Unit> RefreshSteamWorkshopUpdatesBackgroundAsync(IScheduler sch, CancellationToken token)
	{
		var updates = await _updater.GetSteamWorkshopUpdatesAsync(Settings, GetUpdateableMods(), Version, token);
		await sch.Yield(token);
		if (!token.IsCancellationRequested && updates.Count > 0)
		{
			await Observable.Start(() =>
			{
				foreach (var mod in updates.Values)
				{
					var updateData = new DivinityModUpdateData()
					{
						Mod = mod,
						DownloadData = new ModDownloadData()
						{
							DownloadPath = mod.FilePath,
							DownloadPathType = ModDownloadPathType.FILE,
							DownloadSourceType = ModSourceType.STEAM,
							Version = mod.Version.Version,
							Date = mod.LastModified
						},
					};
					Views.ModUpdates.Add(updateData);
				}
			}, RxApp.MainThreadScheduler);
		}
		return Unit.Default;
	}

	private void RefreshSteamWorkshopUpdatesBackground()
	{
		_updater.SteamWorkshop.SteamAppID = AppSettings.DefaultPathways.Steam.AppID;

		_refreshSteamWorkshopUpdatesBackgroundTask?.Dispose();
		_refreshSteamWorkshopUpdatesBackgroundTask = RxApp.TaskpoolScheduler.ScheduleAsync(RefreshSteamWorkshopUpdatesBackgroundAsync);
	}

	private IDisposable _refreshAllModUpdatesBackgroundTask;

	private void RefreshAllModUpdatesBackground()
	{
		IsRefreshingModUpdates = true;
		_refreshAllModUpdatesBackgroundTask?.Dispose();
		_refreshAllModUpdatesBackgroundTask = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, token) =>
		{
			if(Settings.UpdateSettings.UpdateGitHubMods) await RefreshGitHubModsUpdatesBackgroundAsync(sch, token);
			if (Settings.UpdateSettings.UpdateNexusMods) await RefreshNexusModsUpdatesBackgroundAsync(sch, token);
			if (Settings.UpdateSettings.UpdateSteamWorkshopMods) await RefreshSteamWorkshopUpdatesBackgroundAsync(sch, token);

			IsRefreshingModUpdates = false;
		});
	}

	public void OnMainProgressComplete(double delay = 0)
	{
		DivinityApp.Log($"Main progress is complete.");

		MainProgressValue = 1d;
		MainProgressWorkText = "Finished.";

		MainProgressToken?.Dispose();

		if (delay > 0)
		{
			RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(delay), _ =>
			{
				MainProgressIsActive = false;
				CanCancelProgress = true;
			});
		}
		else
		{
			MainProgressIsActive = false;
			CanCancelProgress = true;
		}
	}

	public void AddImportedMod(DivinityModData mod, bool toActiveList = false)
	{
		var modManager = Services.Mods;
		mod.SteamWorkshopEnabled = SteamWorkshopSupportEnabled;
		mod.NexusModsEnabled = NexusModsSupportEnabled;

		if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
		{
			modManager.Add(mod);
			DivinityApp.Log($"Imported Override Mod: {mod}");
			return;
		}

		if (modManager.TryGetMod(mod.UUID, out var existingMod))
		{
			mod.IsSelected = existingMod.IsSelected;
			if (existingMod.IsActive)
			{
				mod.Index = existingMod.Index;
				Views.ModOrder.ActiveMods.ReplaceOrAdd(existingMod, mod);
			}
			else
			{
				if (toActiveList)
				{
					Views.ModOrder.InactiveMods.Remove(existingMod);
					mod.Index = Views.ModOrder.ActiveMods.Count;
					Views.ModOrder.ActiveMods.Add(mod);
				}
				else
				{
					Views.ModOrder.InactiveMods.ReplaceOrAdd(existingMod, mod);
				}
			}
		}
		else
		{
			if (toActiveList)
			{
				mod.Index = Views.ModOrder.ActiveMods.Count;
				Views.ModOrder.ActiveMods.Add(mod);
			}
			else
			{
				Views.ModOrder.InactiveMods.Add(mod);
			}
		}
		modManager.Add(mod);
		UpdateModExtenderStatus(mod);
		DivinityApp.Log($"Imported Mod: {mod}");
	}

	private async void ExportLoadOrderToArchive_Start()
	{
		//view.MainWindowMessageBox.Text = "Add active mods to a zip file?";
		//view.MainWindowMessageBox.Caption = "Depending on the number of mods, this may take some time.";
		MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, $"Save active mods to a zip file?{Environment.NewLine}Depending on the number of mods, this may take some time.", "Confirm Archive Creation",
			MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel, Window.MessageBoxStyle);
		if (result == MessageBoxResult.OK)
		{
			await StartMainProgressAsync("Adding active mods to zip...");
			MainProgressToken = new CancellationTokenSource();
			await Observable.Start(async () =>
			{
				await ModImporter.ExportLoadOrderToArchiveAsync(Views.ModOrder.SelectedProfile, Views.ModOrder.SelectedModOrder, "", MainProgressToken.Token);
				await Observable.Start(() => OnMainProgressComplete(), RxApp.MainThreadScheduler);
			}, RxApp.TaskpoolScheduler);
		}
	}

	private void ExportLoadOrderToArchiveAs(DivinityProfileData profile, DivinityLoadOrder order)
	{
		if (profile != null && order != null)
		{
			var dialog = new SaveFileDialog
			{
				AddExtension = true,
				DefaultExt = ".zip",
				Filter = "Archive file (*.zip)|*.zip",
				InitialDirectory = GetInitialStartingDirectory()
			};

			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
			var baseOrderName = order.Name;
			if (order.IsModSettings)
			{
				baseOrderName = $"{profile.Name}_{order.Name}";
			}
			var outputName = $"{baseOrderName}-{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.zip";

			//dialog.RestoreDirectory = true;
			dialog.FileName = DivinityModDataLoader.MakeSafeFilename(outputName, '_');
			dialog.CheckFileExists = false;
			dialog.CheckPathExists = false;
			dialog.OverwritePrompt = true;
			dialog.Title = "Export Load Order As...";

			if (dialog.ShowDialog(Window) == true)
			{
				RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
				{
					await StartMainProgressAsync("Adding active mods to zip...");
					await ModImporter.ExportLoadOrderToArchiveAsync(Views.ModOrder.SelectedProfile, Views.ModOrder.SelectedModOrder, dialog.FileName, MainProgressToken.Token);
					await ctrl.Yield(t);
					await Observable.Start(() => OnMainProgressComplete(), RxApp.MainThreadScheduler);
				});
			}
		}
		else
		{
			DivinityApp.ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
		}
	}

	private void RenameSave_Start()
	{
		string profileSavesDirectory = "";
		if (Views.ModOrder.SelectedProfile != null)
		{
			profileSavesDirectory = Path.GetFullPath(Path.Join(Views.ModOrder.SelectedProfile.FilePath, "Savegames"));
		}
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".lsv",
			Filter = "Larian Save file (*.lsv)|*.lsv",
			Title = "Pick Save to Rename..."
		};

		var startPath = "";
		if (Views.ModOrder.SelectedProfile != null)
		{
			string profilePath = Path.GetFullPath(Path.Join(Views.ModOrder.SelectedProfile.FilePath, "Savegames"));
			string storyPath = Path.Join(profilePath, "Story");
			if (Directory.Exists(storyPath))
			{
				startPath = storyPath;
			}
			else
			{
				startPath = profilePath;
			}
		}

		dialog.InitialDirectory = GetInitialStartingDirectory(startPath);

		if (dialog.ShowDialog(Window) == true)
		{
			string rootFolder = Path.GetDirectoryName(dialog.FileName);
			string rootFileName = Path.GetFileNameWithoutExtension(dialog.FileName);
			PathwayData.LastSaveFilePath = rootFolder;

			var renameDialog = new SaveFileDialog
			{
				CheckFileExists = false,
				CheckPathExists = false,
				DefaultExt = ".lsv",
				Filter = "Larian Save file (*.lsv)|*.lsv",
				Title = "Rename Save As...",
				InitialDirectory = rootFolder,
				FileName = rootFileName + "_1.lsv"
			};

			if (!Directory.Exists(renameDialog.InitialDirectory))
			{
				dialog.InitialDirectory = GetInitialStartingDirectory(startPath);
			}

			if (renameDialog.ShowDialog(Window) == true)
			{
				rootFolder = Path.GetDirectoryName(renameDialog.FileName);
				PathwayData.LastSaveFilePath = rootFolder;
				DivinityApp.Log($"Renaming '{dialog.FileName}' to '{renameDialog.FileName}'.");

				if (DivinitySaveTools.RenameSave(dialog.FileName, renameDialog.FileName))
				{
					try
					{
						string previewImage = Path.Join(rootFolder, rootFileName + ".WebP");
						string renamedImage = Path.Join(rootFolder, Path.GetFileNameWithoutExtension(renameDialog.FileName) + ".WebP");
						if (File.Exists(previewImage))
						{
							File.Move(previewImage, renamedImage);
							DivinityApp.Log($"Renamed save screenshot '{previewImage}' to '{renamedImage}'.");
						}

						string originalDirectory = Path.GetDirectoryName(dialog.FileName);
						string desiredDirectory = Path.GetDirectoryName(renameDialog.FileName);

						if (!String.IsNullOrEmpty(profileSavesDirectory) && FileUtils.IsSubdirectoryOf(profileSavesDirectory, desiredDirectory))
						{
							if (originalDirectory == desiredDirectory)
							{
								var dirInfo = new DirectoryInfo(originalDirectory);
								if (dirInfo.Name.Equals(Path.GetFileNameWithoutExtension(dialog.FileName)))
								{
									desiredDirectory = Path.Join(dirInfo.Parent.FullName, Path.GetFileNameWithoutExtension(renameDialog.FileName));
									RecycleBinHelper.DeleteFile(dialog.FileName, false, false);
									Directory.Move(originalDirectory, desiredDirectory);
									DivinityApp.Log($"Renamed save folder '{originalDirectory}' to '{desiredDirectory}'.");
								}
							}
						}

						ShowAlert($"Successfully renamed '{dialog.FileName}' to '{renameDialog.FileName}'", AlertType.Success, 15);
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Failed to rename '{dialog.FileName}' to '{renameDialog.FileName}':\n" + ex.ToString());
					}
				}
				else
				{
					DivinityApp.Log($"Failed to rename '{dialog.FileName}' to '{renameDialog.FileName}'");
				}
			}
		}
	}

	public void CheckForUpdates(bool force = false)
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
	}

	private bool _userInvokedUpdate = false;

	private void OnAppUpdate(UpdateInfoEventArgs e)
	{
		if (_userInvokedUpdate)
		{
			if (e.Error == null)
			{
				if (e.IsUpdateAvailable)
				{
					ShowAlert("Update found!", AlertType.Success, 30);
				}
				else
				{
					ShowAlert("Already up-to-date", AlertType.Info, 30);
				}
			}
			else
			{
				ShowAlert($"Error occurred when checking for updates: {e.Error.Message}", AlertType.Danger, 60);
			}
		}

		if (e.Error == null)
		{
			if (_userInvokedUpdate || e.IsUpdateAvailable)
			{
				App.WM.AppUpdate.Toggle();
				App.WM.AppUpdate.Window.ViewModel.CheckArgs(e);
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
	}

	public void OnViewActivated(MainWindow window, MainViewControl parentView)
	{
		Window = window;
		View = parentView;

		InitSettingsBindings();

		if (DebugMode)
		{
			string lastMessage = "";
			this.WhenAnyValue(x => x.MainProgressWorkText, x => x.MainProgressValue).Subscribe((ob) =>
			{
				if (!String.IsNullOrEmpty(ob.Item1) && lastMessage != ob.Item1)
				{
					DivinityApp.Log($"[{ob.Item2:P0}] {ob.Item1}");
					lastMessage = ob.Item1;
				}
			});
		}

		LoadSettings();
		Keys.LoadKeybindings(this);
		if (Settings.CheckForUpdates) CheckForUpdates();
		SaveSettings();

		if (Settings.SaveWindowLocation)
		{
			var win = Settings.Window;
			Window.WindowStartupLocation = WindowStartupLocation.Manual;

			var screens = System.Windows.Forms.Screen.AllScreens;
			var screen = screens.FirstOrDefault();
			if (screen != null)
			{
				if (win.Screen > -1 && win.Screen < screens.Length - 1)
				{
					screen = screens[win.Screen];
				}

				Window.Left = Math.Max(screen.WorkingArea.Left, Math.Min(screen.WorkingArea.Right, screen.WorkingArea.Left + win.X));
				Window.Top = Math.Max(screen.WorkingArea.Top, Math.Min(screen.WorkingArea.Bottom, screen.WorkingArea.Top + win.Y));
			}

			if (win.Maximized)
			{
				Window.WindowState = WindowState.Maximized;
			}
		}
	}

	public void LoadInitial()
	{
		if(!IsInitialized)
		{
			StartMainProgress("Loading...");
			Views.SwitchToModOrderView();
			RxApp.TaskpoolScheduler.ScheduleAsync(async (_,_) => {
				await RefreshAsync();
				await Observable.Start(() =>
				{
					View.Visibility = Visibility.Visible;
					IsInitialized = true;
				}, RxApp.MainThreadScheduler);
			});
		}
	}

	private readonly MainWindowExceptionHandler exceptionHandler;

	public void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0)
	{
		DivinityApp.Log(message);
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			if (timeout < 0) timeout = 0;
			switch (alertType)
			{
				case AlertType.Danger:
					View.AlertBar.SetDangerAlert(message, timeout);
					break;
				case AlertType.Warning:
					View.AlertBar.SetWarningAlert(message, timeout);
					break;
				case AlertType.Success:
					View.AlertBar.SetSuccessAlert(message, timeout);
					break;
				case AlertType.Info:
				default:
					View.AlertBar.SetInformationAlert(message, timeout);
					break;
			}
		});
	}

	private void DeleteMods(List<DivinityModData> targetMods, bool isDeletingDuplicates = false, IEnumerable<DivinityModData> loadedMods = null)
	{
		if (!IsDeletingFiles)
		{
			var targetUUIDs = targetMods.Select(x => x.UUID).ToHashSet();

			var deleteFilesData = targetMods.Select(x => ModFileDeletionData.FromMod(x, false, isDeletingDuplicates, loadedMods));
			Views.DeleteFiles.IsDeletingDuplicates = isDeletingDuplicates;
			Views.DeleteFiles.Files.AddRange(deleteFilesData);

			//TODO Replace with SteamWorkshopService function
			//var workshopMods = WorkshopMods.Where(wm => targetUUIDs.Contains(wm.UUID) && File.Exists(wm.FilePath)).Select(x => ModFileDeletionData.FromMod(x, true));
			//this.View.DeleteFilesView.ViewModel.Files.AddRange(workshopMods);

			Views.DeleteFiles.IsVisible = true;
		}
	}

	private void ExtractSelectedMods_ChooseFolder()
	{
		var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
		{
			ShowNewFolderButton = true,
			UseDescriptionForTitle = true,
			Description = "Select folder to extract mod(s) to...",
			SelectedPath = GetInitialStartingDirectory(Settings.LastExtractOutputPath)
		};

		if (dialog.ShowDialog(Window) == true)
		{
			Settings.LastExtractOutputPath = dialog.SelectedPath;
			SaveSettings();

			string outputDirectory = dialog.SelectedPath;
			DivinityApp.Log($"Extracting selected mods to '{outputDirectory}'.");

			var targetMods = Services.Mods.SelectedPakMods.ToImmutableList();

			int totalWork = targetMods.Count;
			double taskStepAmount = 1.0 / totalWork;
			MainProgressTitle = $"Extracting {totalWork} mods...";
			MainProgressValue = 0d;
			MainProgressToken = new CancellationTokenSource();
			CanCancelProgress = true;
			MainProgressIsActive = true;

			var openOutputPath = dialog.SelectedPath;

			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				int successes = 0;
				foreach (var path in targetMods.Select(x => x.FilePath))
				{
					if (MainProgressToken.IsCancellationRequested) break;
					try
					{
						//Put each pak into its own folder
						string pakName = Path.GetFileNameWithoutExtension(path);
						RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Extracting {pakName}...");
						string destination = Path.Join(outputDirectory, pakName);

						//In case the foldername == the pak name and we're only extracting one pak
						if (totalWork == 1 && Path.GetDirectoryName(outputDirectory).Equals(pakName))
						{
							destination = outputDirectory;
						}
						var success = await FileUtils.ExtractPackageAsync(path, destination, MainProgressToken.Token);
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
					IncreaseMainProgressValue(taskStepAmount);
				}

				await ctrl.Yield();
				RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());

				RxApp.MainThreadScheduler.Schedule(() =>
				{
					if (successes >= totalWork)
					{
						ShowAlert($"Successfully extracted all selected mods to '{dialog.SelectedPath}'", AlertType.Success, 20);
						FileUtils.TryOpenPath(openOutputPath);
					}
					else
					{
						ShowAlert($"Error occurred when extracting selected mods to '{dialog.SelectedPath}'", AlertType.Danger, 30);
					}
				});

				return Disposable.Empty;
			});
		}
	}

	private void ExtractSelectedMods_Start()
	{
		//var selectedMods = Mods.Where(x => x.IsSelected && !x.IsEditorMod).ToList();

		if (Services.Mods.SelectedPakMods.Count == 1)
		{
			ExtractSelectedMods_ChooseFolder();
		}
		else
		{
			MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, $"Extract the following mods?\n'{String.Join("\n", Services.Mods.SelectedPakMods.Select(x => $"{x.DisplayName}"))}", "Extract Mods?",
			MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, Window.MessageBoxStyle);
			if (result == MessageBoxResult.Yes)
			{
				ExtractSelectedMods_ChooseFolder();
			}
		}
	}

	private void ExtractSelectedAdventure()
	{
		var mod = Views.ModOrder.SelectedAdventureMod;

		if (mod == null || mod.IsEditorMod || mod.IsLarianMod || !File.Exists(mod.FilePath))
		{
			var displayName = mod != null ? mod.DisplayName : "";
			ShowAlert($"Current adventure mod '{displayName}' is not extractable", AlertType.Warning, 30);
			return;
		}

		var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
		{
			ShowNewFolderButton = true,
			UseDescriptionForTitle = true,
			Description = "Select folder to extract mod to...",
			SelectedPath = GetInitialStartingDirectory(Settings.LastExtractOutputPath)
		};

		if (dialog.ShowDialog(Window) == true)
		{
			Settings.LastExtractOutputPath = dialog.SelectedPath;
			SaveSettings();

			string outputDirectory = dialog.SelectedPath;
			DivinityApp.Log($"Extracting adventure mod to '{outputDirectory}'.");

			MainProgressTitle = $"Extracting {mod.DisplayName}...";
			MainProgressValue = 0d;
			MainProgressToken = new CancellationTokenSource();
			CanCancelProgress = true;
			MainProgressIsActive = true;

			var openOutputPath = dialog.SelectedPath;

			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				if (MainProgressToken.IsCancellationRequested) return Disposable.Empty;
				var path = mod.FilePath;
				var success = false;
				try
				{
					string pakName = Path.GetFileNameWithoutExtension(path);
					RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Extracting {pakName}...");
					string destination = Path.Join(outputDirectory, pakName);
					if (Path.GetDirectoryName(outputDirectory).Equals(pakName))
					{
						destination = outputDirectory;
					}
					openOutputPath = destination;
					success = await FileUtils.ExtractPackageAsync(path, destination, MainProgressToken.Token);
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error extracting package: {ex}");
				}
				IncreaseMainProgressValue(1);

				await ctrl.Yield();
				RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());

				RxApp.MainThreadScheduler.Schedule(() =>
				{
					if (success)
					{
						ShowAlert($"Successfully extracted adventure mod to '{dialog.SelectedPath}'", AlertType.Success, 20);
						FileUtils.TryOpenPath(openOutputPath);
					}
					else
					{
						ShowAlert($"Error occurred when extracting adventure mod to '{dialog.SelectedPath}'", AlertType.Danger, 30);
					}
				});

				return Disposable.Empty;
			});
		}
	}

	private void LoadAppConfig()
	{
		AppSettingsLoaded = false;
		if (!_settings.TryLoadAppSettings(out var ex))
		{
			ShowAlert($"Error loading app settings: {ex.Message}", AlertType.Danger);
			return;
		}
		AppSettingsLoaded = true;
	}

	private static string NexusModsLimitToText(NexusModsObservableApiLimits limits)
	{
		return $"NexusMods Limits [Hourly ({limits.HourlyRemaining}/{limits.HourlyLimit}) Daily ({limits.DailyRemaining}/{limits.DailyLimit})]";
	}

	public MainWindowViewModel() : base()
	{
		var modManager = Services.Mods;
		ModImporter = new(this);
		Services.RegisterSingleton(ModImporter);

		RefreshCommand = ReactiveCommand.CreateRunInBackground(RefreshAsync, this.WhenAnyValue(x => x.IsLocked, b => !b));

		this.WhenAnyValue(x => x.IsRefreshing, x => x.MainProgressIsActive, x => x.IsDragging).Select(PropertyConverters.AnyBool).BindTo(this, x => x.IsLocked);
		this.WhenAnyValue(x => x.IsLocked, x => x.IsInitialized, (b1, b2) => !b1 && b2).BindTo(this, x => x.AllowDrop);

		var canCancelProgress = RefreshCommand.IsExecuting.CombineLatest(this.WhenAnyValue(x => x.CanCancelProgress)).Select(x => x.First && x.Second);

		CancelMainProgressCommand = ReactiveCommand.Create(() =>
		{
			if (MainProgressToken != null && MainProgressToken.Token.CanBeCanceled)
			{
				MainProgressToken.Token.Register(() => { MainProgressIsActive = IsRefreshing = false; });
				MainProgressToken.Cancel();
			}
		}, canCancelProgress);

		_keys = new AppKeys(this);
		_keys.SaveDefaultKeybindings();

		Router = new RoutingState();

		PathwayData = Services.Pathways.Data;

		DivinityInteractions.ShowMessageBox.RegisterHandler(input =>
		{
			return Observable.Start(() =>
			{
				var data = input.Input;
				var result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, data.Message, data.Title, data.Button, data.Image, data.DefaultResult, Window.MessageBoxStyle);
				input.SetOutput(result);
			}, RxApp.MainThreadScheduler);
		});

		DivinityInteractions.ShowAlert.RegisterHandler(input =>
		{
			return Observable.Start(() =>
			{
				var data = input.Input;
				ShowAlert(data.Message, data.AlertType, data.Timeout);
				input.SetOutput(true);
			}, RxApp.MainThreadScheduler);
		});

		DivinityInteractions.DeleteMods.RegisterHandler(input =>
		{
			return Observable.Start(() =>
			{
				var data = input.Input;
				DeleteMods(data.TargetMods, data.IsDeletingDuplicates, data.LoadedMods);
				input.SetOutput(true);
			}, RxApp.MainThreadScheduler);
		});

		DivinityInteractions.OpenFolderBrowserDialog.RegisterHandler(input =>
		{
			return Observable.Start(() =>
			{
				var data = input.Input;
				var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog()
				{
					Multiselect = data.MultiSelect,
					Description = data.Description,
					UseDescriptionForTitle = String.IsNullOrEmpty(data.Title),
					SelectedPath = !String.IsNullOrEmpty(data.StartingPath) ? data.StartingPath : GetInitialStartingDirectory()
				};

				if (dialog.ShowDialog(Window) == true)
				{
					input.SetOutput(new OpenFolderBrowserDialogResults(true, dialog.SelectedPath, dialog.SelectedPaths, dialog.SelectedPaths.Length <= 1));
				}
				else
				{
					input.SetOutput(new OpenFolderBrowserDialogResults(false, "", [], true));
				}
			}, RxApp.MainThreadScheduler);
		});

        MainProgressValue = 0d;
		MainProgressIsActive = true;
		StatusBarBusyIndicatorVisibility = Visibility.Collapsed;

		DownloadBar = new DownloadActivityBarViewModel();

		_updater = Services.Get<IModUpdaterService>();
		_settings = Services.Get<ISettingsService>();

		_settings.WhenAnyValue(x => x.AppSettings).BindTo(this, x => x.AppSettings);
		_settings.WhenAnyValue(x => x.ManagerSettings).BindTo(this, x => x.Settings);

		exceptionHandler = new MainWindowExceptionHandler(this);
		RxApp.DefaultExceptionHandler = exceptionHandler;

		var assembly = Assembly.GetExecutingAssembly();
		var productName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false)).Product;
		Version = assembly.GetName().Version.ToString();
		Title = $"{productName} {this.Version}";
		DivinityApp.Log($"{Title} initializing...");
		AutoUpdater.AppTitle = productName;

		var nexusModsService = Services.Get<INexusModsService>();
		nexusModsService.WhenLimitsChange.Throttle(TimeSpan.FromMilliseconds(50)).Select(NexusModsLimitToText).ToUIProperty(this, x => x.NexusModsLimitsText);
		var whenNexusModsAvatar = nexusModsService.WhenAnyValue(x => x.ProfileAvatarUrl);
		whenNexusModsAvatar.Select(x => x != null ? Visibility.Visible : Visibility.Collapsed).ToUIProperty(this, x => x.NexusModsProfileAvatarVisibility);
		whenNexusModsAvatar.Select(PropertyHelpers.UriToImage).ToUIProperty(this, x => x.NexusModsProfileBitmapImage);

		nexusModsService.WhenAnyValue(x => x.DownloadProgressValue, x => x.DownloadProgressText, x => x.CanCancel).Subscribe(x =>
		{
			DownloadBar.UpdateProgress(x.Item1, x.Item2);
			if (x.Item3)
			{
				DownloadBar.CancelAction = () => nexusModsService.CancelDownloads();
			}
			else
			{
				DownloadBar.CancelAction = null;
			}
		});

		this.WhenAnyValue(x => x.Settings.UpdateSettings.NexusModsAPIKey).BindTo(nexusModsService, x => x.ApiKey);

		IDisposable importDownloadsTask = null;
		nexusModsService.DownloadResults.ObserveAddChanged().Subscribe(f =>
		{
			importDownloadsTask?.Dispose();
			importDownloadsTask = RxApp.TaskpoolScheduler.ScheduleAsync(TimeSpan.FromMilliseconds(250), async (sch, token) =>
			{
				var files = nexusModsService.DownloadResults.ToList();
				nexusModsService.DownloadResults.Clear();

				var result = new ImportOperationResults()
				{
					TotalFiles = files.Count
				};
				var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
				foreach (var filePath in files)
				{
					await ModImporter.ImportModFromFile(builtinMods, result, filePath, token, false);
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
								ShowAlert($"Successfully imported {total} downloaded mods", AlertType.Success, 20);
							}
							else if (total == 1)
							{
								var modFileName = result.Mods.First().FileName;
								var fileNames = String.Join(", ", files.Select(x => Path.GetFileName(x)));
								ShowAlert($"Successfully imported '{modFileName}' from '{fileNames}'", AlertType.Success, 20);
							}
							else
							{
								ShowAlert("Skipped importing mod - No .pak file found", AlertType.Success, 20);
							}
						}
						else
						{
							if (total == 0)
							{
								ShowAlert("No mods imported. Does the file contain a .pak?", AlertType.Warning, 60);
							}
							else
							{
								ShowAlert($"Only imported {total}/{result.TotalPaks} mods - Check the log", AlertType.Danger, 60);
							}
						}
					}, RxApp.MainThreadScheduler);
				}
			});
		});

		this.WhenAnyValue(x => x.AppSettings.Features.GitHub, x => x.Settings.UpdateSettings.UpdateGitHubMods).Select(x => x.Item1 && x.Item2).BindTo(_updater.GitHub, x => x.IsEnabled);
		this.WhenAnyValue(x => x.AppSettings.Features.NexusMods, x => x.Settings.UpdateSettings.UpdateNexusMods).Select(x => x.Item1 && x.Item2).BindTo(_updater.NexusMods, x => x.IsEnabled);
		this.WhenAnyValue(x => x.AppSettings.Features.SteamWorkshop, x => x.Settings.UpdateSettings.UpdateSteamWorkshopMods).Select(x => x.Item1 && x.Item2).BindTo(_updater.SteamWorkshop, x => x.IsEnabled);

		_updater.SteamWorkshop.WhenAnyValue(x => x.IsEnabled).ToUIProperty(this, x => x.SteamWorkshopSupportEnabled);
		_updater.NexusMods.WhenAnyValue(x => x.IsEnabled).ToUIProperty(this, x => x.NexusModsSupportEnabled);
		_updater.GitHub.WhenAnyValue(x => x.IsEnabled).ToUIProperty(this, x => x.GitHubModSupportEnabled);

		_updater.WhenAnyValue(x => x.GitHub.IsEnabled, x => x.NexusMods.IsEnabled, x => x.SteamWorkshop.IsEnabled)
		.Throttle(TimeSpan.FromMilliseconds(250))
		.ObserveOn(RxApp.MainThreadScheduler)
		.Subscribe(x =>
		{
			foreach (var mod in modManager.AllMods)
			{
				mod.GitHubEnabled = x.Item1;
				mod.NexusModsEnabled = x.Item2;
				mod.SteamWorkshopEnabled = x.Item3;
			}
		});

		var whenRefreshing = _updater.WhenAnyValue(x => x.IsRefreshing);
		whenRefreshing.Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.UpdatingBusyIndicatorVisibility);
		whenRefreshing.Select(PropertyConverters.BoolToVisibilityReversed).ToUIProperty(this, x => x.UpdateCountVisibility);

		this.WhenAnyValue(x => x.Settings.DebugModeEnabled, x => x.Settings.ExtenderSettings.DeveloperMode)
		.Select(x => PropertyConverters.BoolToVisibility(x.Item1 || x.Item2)).ToUIProperty(this, x => x.DeveloperModeVisibility);

		#region Keys Setup
		

		Keys.Refresh.AddAction(() => RefreshCommand.Execute(Unit.Default).Subscribe(), RefreshCommand.CanExecute);

		var canRefreshModUpdates = this.WhenAnyValue(x => x.IsRefreshing, x => x.IsRefreshingModUpdates, x => x.AppSettingsLoaded,
		(b1, b2, b3) => !b1 && !b2 && b3)
		.ObserveOn(RxApp.MainThreadScheduler).StartWith(false);

		RefreshModUpdatesCommand = ReactiveCommand.Create(() =>
		{
			Views.ModUpdates?.Clear();
			ModUpdatesAvailable = false;
			RefreshAllModUpdatesBackground();
		}, canRefreshModUpdates, RxApp.MainThreadScheduler);

		Keys.RefreshModUpdates.AddAction(() => RefreshModUpdatesCommand.Execute().Subscribe(), canRefreshModUpdates);

		Keys.OpenCollectionDownloaderWindow.AddAction(() => App.WM.CollectionDownload.Toggle(true));

		CheckForGitHubModUpdatesCommand = ReactiveCommand.Create(RefreshGitHubModsUpdatesBackground, this.WhenAnyValue(x => x.GitHubModSupportEnabled), RxApp.MainThreadScheduler);
		CheckForNexusModsUpdatesCommand = ReactiveCommand.Create(RefreshNexusModsUpdatesBackground, this.WhenAnyValue(x => x.NexusModsSupportEnabled), RxApp.MainThreadScheduler);
		CheckForSteamWorkshopUpdatesCommand = ReactiveCommand.Create(RefreshSteamWorkshopUpdatesBackground, this.WhenAnyValue(x => x.SteamWorkshopSupportEnabled), RxApp.MainThreadScheduler);

		IObservable<bool> canStartExport = this.WhenAny(x => x.MainProgressToken, (t) => t != null).StartWith(false);
		Keys.ExportOrderToZip.AddAction(ExportLoadOrderToArchive_Start, canStartExport);
		Keys.ExportOrderToArchiveAs.AddAction(() => ExportLoadOrderToArchiveAs(Views.ModOrder.SelectedProfile, Views.ModOrder.SelectedModOrder), canStartExport);

		Keys.OpenDonationLink.AddAction(() =>
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

			foreach (var m in modManager.AllMods)
			{
				m.DisplayFileForName = Settings.DisplayFileNames;
			}
		});

		var canDownloadNexusFiles = this.WhenAnyValue(x => x.Settings.UpdateSettings.NexusModsAPIKey, x => x.NexusModsSupportEnabled)
			.Select(x => !String.IsNullOrEmpty(x.Item1) && x.Item2);
		Keys.DownloadNXMLink.AddCanExecuteCondition(canDownloadNexusFiles);
		Keys.DownloadNXMLink.AddAction(() =>
		{
			App.WM.NxmDownload.Toggle();
		});

		#endregion

		Router.CurrentViewModel.Select(x => x == Views.ModUpdates).ToUIProperty(this, x => x.UpdatesViewIsVisible, false);

		var canToggleUpdatesView = this.WhenAnyValue(x => x.ModUpdatesAvailable);
		void toggleUpdatesView()
		{
			if(Router.GetCurrentViewModel() != Views.ModUpdates)
			{
				Views.SwitchToModUpdates();
			}
			else
			{
				Views.SwitchToModOrderView();
			}
		};
		Keys.ToggleUpdatesView.AddAction(toggleUpdatesView, canToggleUpdatesView);
		ToggleUpdatesViewCommand = ReactiveCommand.Create(toggleUpdatesView, canToggleUpdatesView);

		RenameSaveCommand = ReactiveCommand.Create(RenameSave_Start, this.WhenAnyValue(x => x.IsLocked, b => !b));

		DivinityApp.Events.OrderNameChanged += OnOrderNameChanged;

		var canCheckForUpdates = this.WhenAnyValue(x => x.MainProgressIsActive, b => b == false);
		void checkForUpdatesAction()
		{
			ShowAlert("Checking for updates...", AlertType.Info, 30);
			_userInvokedUpdate = true;
			CheckForUpdates(true);
			SaveSettings();
		}
		CheckForAppUpdatesCommand = ReactiveCommand.Create(checkForUpdatesAction, canCheckForUpdates);
		Keys.CheckForUpdates.AddAction(checkForUpdatesAction, canCheckForUpdates);

		OnAppUpdateCheckedCommand = ReactiveCommand.Create<UpdateInfoEventArgs>(OnAppUpdate);

		Observable.FromEvent<AutoUpdater.CheckForUpdateEventHandler, UpdateInfoEventArgs>(
		e => AutoUpdater.CheckForUpdateEvent += e,
		e => AutoUpdater.CheckForUpdateEvent -= e)
		.InvokeCommand(OnAppUpdateCheckedCommand);

		// Blinky animation on the tools/download buttons if the extender is required by mods and is missing
		if (AppSettings.Features.ScriptExtender)
		{
			modManager.ModsConnection.ObserveOn(RxApp.MainThreadScheduler).AutoRefresh(x => x.ExtenderModStatus).
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

		var anyPakModSelectedObservable = modManager.SelectedPakMods.ToObservableChangeSet().CountChanged().Select(x => modManager.SelectedPakMods.Count > 0);
		Keys.ExtractSelectedMods.AddAction(ExtractSelectedMods_Start, anyPakModSelectedObservable);

		SaveSettingsSilentlyCommand = ReactiveCommand.Create(SaveSettings);

		DivinityInteractions.ConfirmModDeletion.RegisterHandler(async interaction =>
		{
			var sentenceStart = interaction.Input.PermanentlyDelete ? "Permanently delete" : "Delete";
			var msg = $"{sentenceStart} {interaction.Input.Total} mod file(s)?";

			var confirmed = await Observable.Start(() =>
			{
				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, msg, "Confirm Mod Deletion",
				MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, Window.MessageBoxStyle);
				if (result == MessageBoxResult.Yes)
				{
					return true;
				}
				return false;
			}, RxApp.MainThreadScheduler);
			interaction.SetOutput(confirmed);
		});

		Views = new ViewManager(Router, this);

		var canExtractAdventure = Views.ModOrder.WhenAnyValue(x => x.SelectedAdventureMod).Select(x => x != null && !x.IsEditorMod && !x.IsLarianMod);
		Keys.ExtractSelectedAdventure.AddAction(ExtractSelectedAdventure, canExtractAdventure);

		Views.DeleteFiles.WhenAnyValue(x => x.IsVisible).ToUIProperty(this, x => x.IsDeletingFiles);
		Views.ModUpdates.WhenAnyValue(x => x.TotalUpdates, total => total > 0).BindTo(this, x => x.ModUpdatesAvailable);

		Views.ModUpdates.CloseView = new Action<bool>((bool refresh) =>
		{
			Views.ModUpdates.Clear();
			//TODO Replace with reloading the individual mods that changed
			if (refresh) RefreshCommand.Execute(Unit.Default).Subscribe();
			Window.Activate();
		});
	}
}
