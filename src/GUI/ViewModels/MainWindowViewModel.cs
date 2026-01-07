
using AutoUpdaterDotNET;

using DivinityModManager.AppServices;
using DivinityModManager.Extensions;
using DivinityModManager.Models;
using DivinityModManager.Models.App;
using DivinityModManager.Models.Extender;
using DivinityModManager.Models.NexusMods;
using DivinityModManager.ModUpdater;
using DivinityModManager.ModUpdater.Cache;
using DivinityModManager.Util;
using DivinityModManager.Util.ScreenReader;
using DivinityModManager.Views;

using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers;
using SharpCompress.Writers;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ZstdSharp;

namespace DivinityModManager.ViewModels;

public class MainWindowViewModel : BaseHistoryViewModel, IActivatableViewModel, IDivinityAppViewModel
{
	[Reactive] public MainWindow Window { get; private set; }
	[Reactive] public MainViewControl View { get; private set; }

	public IModViewLayout Layout { get; set; }

	private ModListDropHandler dropHandler;

	public ModListDropHandler DropHandler
	{
		get => dropHandler;
		set { this.RaiseAndSetIfChanged(ref dropHandler, value); }
	}

	private ModListDragHandler dragHandler;

	public ModListDragHandler DragHandler
	{
		get => dragHandler;
		set { this.RaiseAndSetIfChanged(ref dragHandler, value); }
	}

	[Reactive] public string AppTitle { get; set; }
	[Reactive] public Version Version { get; set; }
	[Reactive] public string Title { get; set; }

	private readonly AppKeys _keys;
	public AppKeys Keys => _keys;

	[Reactive] public bool IsInitialized { get; private set; }

	protected readonly SourceCache<DivinityModData, string> mods = new(mod => mod.UUID);

	public bool ModExists(string uuid)
	{
		return mods.Lookup(uuid) != null;
	}

	public bool TryGetMod(string guid, out DivinityModData mod)
	{
		mod = null;
		var modResult = mods.Lookup(guid);
		if (modResult.HasValue)
		{
			mod = modResult.Value;
			return true;
		}
		return false;
	}

	public bool IsModActive(string guid)
	{
		if(TryGetMod(guid, out var mod))
		{
			return mod.IsActive;
		}
		return false;
	}

	public string GetModType(string guid)
	{
		if (TryGetMod(guid, out var mod))
		{
			return mod.ModType;
		}
		return "";
	}

	protected ReadOnlyObservableCollection<DivinityModData> addonMods;
	public ReadOnlyObservableCollection<DivinityModData> Mods => addonMods;

	protected ReadOnlyObservableCollection<DivinityModData> adventureMods;
	public ReadOnlyObservableCollection<DivinityModData> AdventureMods => adventureMods;

	private int selectedAdventureModIndex = 0;

	public int SelectedAdventureModIndex
	{
		get => selectedAdventureModIndex;
		set
		{
			this.RaiseAndSetIfChanged(ref selectedAdventureModIndex, value);
			this.RaisePropertyChanged("SelectedAdventureMod");
		}
	}

	private readonly ObservableAsPropertyHelper<DivinityModData> _selectedAdventureMod;
	public DivinityModData SelectedAdventureMod => _selectedAdventureMod.Value;

	protected ReadOnlyObservableCollection<DivinityModData> selectedPakMods;
	public ReadOnlyObservableCollection<DivinityModData> SelectedPakMods => selectedPakMods;

	private readonly ModUpdateHandler _updateHandler;
	public ModUpdateHandler UpdateHandler => _updateHandler;

	public DivinityPathwayData PathwayData { get; private set; } = new DivinityPathwayData();

	public ModUpdatesViewData ModUpdatesViewData { get; private set; }

	private IgnoredModsData ignoredModsData;

	public IgnoredModsData IgnoredMods => ignoredModsData;

	private readonly AppSettings appSettings = new();

	public AppSettings AppSettings => appSettings;

	private readonly DivinityModManagerSettings _settings = new();
	public DivinityModManagerSettings Settings => _settings;

	private readonly ObservableCollectionExtended<DivinityModData> _activeMods = new();
	public ObservableCollectionExtended<DivinityModData> ActiveMods => _activeMods;

	private readonly ObservableCollectionExtended<DivinityModData> _inactiveMods = new();
	public ObservableCollectionExtended<DivinityModData> InactiveMods => _inactiveMods;

	private readonly ReadOnlyObservableCollection<DivinityModData> _forceLoadedMods;
	public ReadOnlyObservableCollection<DivinityModData> ForceLoadedMods => _forceLoadedMods;

	private readonly ReadOnlyObservableCollection<DivinityModData> _userMods;
	public ReadOnlyObservableCollection<DivinityModData> UserMods => _userMods;

	IEnumerable<DivinityModData> IDivinityAppViewModel.ActiveMods => this.ActiveMods;
	IEnumerable<DivinityModData> IDivinityAppViewModel.InactiveMods => this.InactiveMods;

	public ObservableCollectionExtended<DivinityProfileData> Profiles { get; set; } = new ObservableCollectionExtended<DivinityProfileData>();

	private readonly ObservableAsPropertyHelper<int> _activeSelected;
	public int ActiveSelected => _activeSelected.Value;

	private readonly ObservableAsPropertyHelper<int> _inactiveSelected;
	public int InactiveSelected => _inactiveSelected.Value;

	private readonly ObservableAsPropertyHelper<string> _activeSelectedText;
	public string ActiveSelectedText => _activeSelectedText.Value;

	private readonly ObservableAsPropertyHelper<string> _inactiveSelectedText;
	public string InactiveSelectedText => _inactiveSelectedText.Value;

	private readonly ObservableAsPropertyHelper<string> _activeModsFilterResultText;
	public string ActiveModsFilterResultText => _activeModsFilterResultText.Value;

	private readonly ObservableAsPropertyHelper<string> _inactiveModsFilterResultText;
	public string InactiveModsFilterResultText => _inactiveModsFilterResultText.Value;

	[Reactive] public string ActiveModFilterText { get; set; }
	[Reactive] public string InactiveModFilterText { get; set; }

	[Reactive] public int SelectedProfileIndex { get; set; }

	private readonly ObservableAsPropertyHelper<DivinityProfileData> _selectedProfile;
	public DivinityProfileData SelectedProfile => _selectedProfile.Value;

	private readonly ObservableAsPropertyHelper<bool> _hasProfile;
	public bool HasProfile => _hasProfile.Value;

	public ObservableCollectionExtended<DivinityLoadOrder> ModOrderList { get; set; } = new ObservableCollectionExtended<DivinityLoadOrder>();

	[Reactive] public int SelectedModOrderIndex { get; set; }

	private readonly ObservableAsPropertyHelper<DivinityLoadOrder> _selectedModOrder;
	public DivinityLoadOrder SelectedModOrder => _selectedModOrder.Value;

	private readonly ObservableAsPropertyHelper<string> _selectedModOrderName;
	public string SelectedModOrderName => _selectedModOrderName.Value;

	private readonly ObservableAsPropertyHelper<bool> _isBaseLoadOrder;
	public bool IsBaseLoadOrder => _isBaseLoadOrder.Value;

	public List<DivinityLoadOrder> SavedModOrderList { get; set; } = new List<DivinityLoadOrder>();

	private bool HasExported { get; set; }

	[Reactive] public int LayoutMode { get; set; }
	[Reactive] public bool CanSaveOrder { get; set; }
	[Reactive] public bool IsLoadingOrder { get; set; }
	[Reactive] public bool OrderJustLoaded { get; set; }
	[Reactive] public bool IsDragging { get; set; }
	[Reactive] public bool AppSettingsLoaded { get; set; }
	[Reactive] public bool IsRefreshing { get; private set; }
	[Reactive] public bool IsRefreshingModUpdates { get; private set; }

	private readonly ObservableAsPropertyHelper<bool> _isLocked;

	/// <summary>Used to locked certain functionality when data is loading or the user is dragging an item.</summary>
	public bool IsLocked => _isLocked.Value;

	private readonly ObservableAsPropertyHelper<bool> _allowDrop;
	public bool AllowDrop => _allowDrop.Value;

	[Reactive] public string StatusText { get; set; }
	[Reactive] public string StatusBarRightText { get; set; }
	[Reactive] public bool ModUpdatesAvailable { get; set; }
	[Reactive] public bool ModUpdatesViewVisible { get; set; }
	[Reactive] public bool HighlightExtenderDownload { get; set; }
	[Reactive] public bool GameDirectoryFound { get; set; }

	private readonly ObservableAsPropertyHelper<bool> _hideModList;
	public bool HideModList => _hideModList.Value;

	private readonly ObservableAsPropertyHelper<bool> _hasForceLoadedMods;
	public bool HasForceLoadedMods => _hasForceLoadedMods.Value;

	private readonly ObservableAsPropertyHelper<bool> _isDeletingFiles;
	public bool IsDeletingFiles => _isDeletingFiles.Value;

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

	[Reactive] public CancellationTokenSource MainProgressToken { get; set; }
	[Reactive] public bool CanCancelProgress { get; set; }

	#endregion
	[Reactive] public bool IsRenamingOrder { get; set; }
	[Reactive] public Visibility StatusBarBusyIndicatorVisibility { get; set; }
	[Reactive] public bool CanMoveSelectedMods { get; set; }

	private readonly ObservableAsPropertyHelper<Visibility> _updatingBusyIndicatorVisibility;
	public Visibility UpdatingBusyIndicatorVisibility => _updatingBusyIndicatorVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _updateCountVisibility;
	public Visibility UpdateCountVisibility => _updateCountVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _updatesViewVisibility;
	public Visibility UpdatesViewVisibility => _updatesViewVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _developerModeVisibility;
	public Visibility DeveloperModeVisibility => _developerModeVisibility.Value;

	private readonly ObservableAsPropertyHelper<Visibility> _logFolderShortcutButtonVisibility;
	public Visibility LogFolderShortcutButtonVisibility => _logFolderShortcutButtonVisibility.Value;

	public ICommand ToggleUpdatesViewCommand { get; private set; }
	public ICommand CheckForAppUpdatesCommand { get; set; }
	public ICommand CancelMainProgressCommand { get; set; }
	public ICommand CopyPathToClipboardCommand { get; set; }
	public ICommand RenameSaveCommand { get; private set; }
	public ICommand CopyOrderToClipboardCommand { get; private set; }
	public ICommand OpenAdventureModInFileExplorerCommand { get; private set; }
	public ICommand CopyAdventureModPathToClipboardCommand { get; private set; }
	public ICommand ConfirmCommand { get; set; }
	public ICommand FocusFilterCommand { get; set; }
	public ICommand SaveSettingsSilentlyCommand { get; private set; }
	public ReactiveCommand<DivinityLoadOrder, Unit> DeleteOrderCommand { get; private set; }
	public ReactiveCommand<object, Unit> ToggleOrderRenamingCommand { get; set; }
	public RxCommandUnit RefreshCommand { get; private set; }
	public RxCommandUnit RefreshModUpdatesCommand { get; private set; }
	public ICommand UpdateNexusModsLimitsCommand { get; private set; }
	public EventHandler OnRefreshed { get; set; }

	private AppServices.IFileWatcherWrapper _modSettingsWatcher;

	public bool DebugMode { get; set; }

	private bool _justDownloadedScriptExtender;

	private void DownloadScriptExtender(string exeDir)
	{
		var isLoggingEnabled = Window.DebugLogListener != null;
		if (!isLoggingEnabled) Window.ToggleLogging(true);

		double taskStepAmount = 1.0 / 3;
		MainProgressTitle = $"Setting up the Script Extender...";
		MainProgressValue = 0d;
		MainProgressToken = new CancellationTokenSource();
		CanCancelProgress = true;
		MainProgressIsActive = true;

		string dllDestination = Path.Combine(exeDir, DivinityApp.EXTENDER_UPDATER_FILE);

		RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
		{
			int successes = 0;
			Stream webStream = null;
			Stream unzippedEntryStream = null;
			try
			{
				await SetMainProgressTextAsync($"Downloading {PathwayData.ScriptExtenderLatestReleaseUrl}...");
				webStream = await WebHelper.DownloadFileAsStreamAsync(PathwayData.ScriptExtenderLatestReleaseUrl, MainProgressToken.Token);
				if (webStream != null)
				{
					successes += 1;
					await IncreaseMainProgressValueAsync(taskStepAmount, $"Extracting zip to {exeDir}...");
					ZipArchive archive = new ZipArchive(webStream);
					foreach (ZipArchiveEntry entry in archive.Entries)
					{
						if (MainProgressToken.IsCancellationRequested) break;
						if (entry.Name.Equals(DivinityApp.EXTENDER_UPDATER_FILE, StringComparison.OrdinalIgnoreCase))
						{
							unzippedEntryStream = entry.Open(); // .Open will return a stream
							using var fs = File.Create(dllDestination, 4096, System.IO.FileOptions.Asynchronous);
							await unzippedEntryStream.CopyToAsync(fs, 4096, MainProgressToken.Token);
							successes += 1;
							break;
						}
					}
					await IncreaseMainProgressValueAsync(taskStepAmount);
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error extracting package: {ex}");
			}
			finally
			{
				await SetMainProgressTextAsync("Cleaning up...");
				webStream?.Close();
				unzippedEntryStream?.Close();
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
					_justDownloadedScriptExtender = true;
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
		if (AppSettings.FeatureEnabled("ScriptExtender"))
		{
			var exeDir = Path.GetDirectoryName(Settings.GameExecutablePath);
			var extenderUpdaterPath = Path.Combine(exeDir, DivinityApp.EXTENDER_UPDATER_FILE);
			var toolboxPath = DivinityApp.GetAppDirectory("Tools", "Toolbox.exe");

			if (File.Exists(toolboxPath) && File.Exists(extenderUpdaterPath) && Settings.ExtenderUpdaterSettings.UpdaterVersion >= 4)
			{
				try
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
				catch (Exception ex)
				{
					DivinityApp.Log($"Error running Toolbox.exe:\n{ex}");
				}
			}

			if (IsInitialized && !IsRefreshing)
			{
				CheckExtenderInstalledVersion(t);
				if (updateMods)
				{
					RxApp.MainThreadScheduler.Schedule(() =>
					{
						UpdateExtenderVersionForAllMods();
					});
				}
			}
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
			ProcessHelper.TryOpenUrl(DivinityApp.EXTENDER_LATEST_URL);
		}
	}

	private void CheckExtenderUpdaterVersion()
	{
		string extenderUpdaterPath = Path.Combine(Path.GetDirectoryName(Settings.GameExecutablePath), DivinityApp.EXTENDER_UPDATER_FILE);
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

	private IDisposable _warnExtenderUpdateFailureTask = null;

	public bool CheckExtenderInstalledVersion(CancellationToken? t)
	{
		var appDataDirectory = GetLarianStudiosAppDataFolder();
		string extenderAppDataDir = "";
		if (Directory.GetParent(appDataDirectory) is DirectoryInfo localAppDataDir)
		{
			extenderAppDataDir = Path.Join(localAppDataDir.FullName, DivinityApp.EXTENDER_APPDATA_DIRECTORY);
		}
		if(string.IsNullOrEmpty(extenderAppDataDir))
		{
			extenderAppDataDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), DivinityApp.EXTENDER_APPDATA_DIRECTORY);
		}
		if (extenderAppDataDir.IsExistingDirectory())
		{
			var files = DivinityFileUtils.EnumerateFiles(extenderAppDataDir, DivinityFileUtils.RecursiveOptions, f =>
			{
				var name = Path.GetFileName(f);
				return name.Equals(DivinityApp.EXTENDER_APPDATA_DLL, StringComparison.OrdinalIgnoreCase);
			});
			var fullExtenderVersion = "";
			int majorVersion = -1;
			var targetVersion = Settings.ExtenderUpdaterSettings.TargetVersion;

			foreach (var f in files)
			{
				try
				{
					if (t?.IsCancellationRequested == true) return false;

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
				Settings.ExtenderSettings.ExtenderIsAvailable = true;
				Settings.ExtenderSettings.ExtenderVersion = fullExtenderVersion;
				Settings.ExtenderSettings.ExtenderMajorVersion = majorVersion;
				return true;
			}
		}
		else
		{
			DivinityApp.Log($"Extender Local AppData folder not found at '{extenderAppDataDir}'. Skipping.");
		}
		//Recently downloaded DWrite.dll, but Toolbox may have failed to invoke an update
		if (t?.IsCancellationRequested == false && _justDownloadedScriptExtender)
		{
			_warnExtenderUpdateFailureTask?.Dispose();
			_warnExtenderUpdateFailureTask = RxApp.MainThreadScheduler.Schedule(() =>
			{
				_justDownloadedScriptExtender = false;
				Xceed.Wpf.Toolkit.MessageBox.Show(Window,
				"The Script Extender has been successfully downloaded.\n\nPlease start the game once to complete the installation process.",
				"Script Extender Installation",
				MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, Window.MessageBoxStyle);
			});
		}
		return false;
	}

	private async Task<bool> CheckForLatestExtenderUpdaterRelease(CancellationToken t)
	{
		try
		{
			string latestReleaseZipUrl = "";
			DivinityApp.Log($"Checking for latest {DivinityApp.EXTENDER_UPDATER_FILE} release at 'https://github.com/{DivinityApp.EXTENDER_REPO_URL}'.");
			var latestReleaseData = await GithubHelper.GetLatestReleaseDataAsync(DivinityApp.EXTENDER_REPO_URL, t);
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

	private async Task<Unit> LoadExtenderSettingsAsync(CancellationToken t)
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
			CheckExtenderInstalledVersion(t);

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
			await Observable.Start(() => UpdateExtender(true, t), RxApp.TaskpoolScheduler);
			return Disposable.Empty;
		});
	}

	private bool FilterDependencies(ModuleShortDesc x, bool devMode)
	{
		if (!devMode)
		{
			return !DivinityModDataLoader.IgnoreModDependency(x.UUID);
		}
		return true;
	}

	private Func<ModuleShortDesc, bool> MakeDependencyFilter(bool b)
	{
		return (x) => FilterDependencies(x, b);
	}

	private void TryStartGameExe(string exePath, string workingDirectory, string launchParams = "")
	{
		var isLoggingEnabled = Window.DebugLogListener != null;
		if (!isLoggingEnabled) Window.ToggleLogging(true);

		if (!ProcessHelper.TryOpenPath(exePath, File.Exists, launchParams, workingDirectory))
		{
			ShowAlert($"Failed to start game exe '{exePath}' - Check the 'Game Executable Path' in the preferences", AlertType.Danger);
		}

		if (!isLoggingEnabled) Window.ToggleLogging(false);
	}

	private void CreateSteamApiTextFile(string exePath)
	{
		var binFolder = Path.GetDirectoryName(exePath);
		// Steam folder check essentially
		var steamAppiDll = Path.Join(binFolder, "steam_api64.dll");
		var steamAppidPath = Path.Join(binFolder, "steam_appid.txt");
		if (!File.Exists(steamAppidPath) && File.Exists(steamAppiDll))
		{
			File.WriteAllText(steamAppidPath, "1086940");

			if(Settings.SettingsWindowIsOpen && Services.Get<SettingsWindowViewModel>() is SettingsWindowViewModel settingsWindowViewModel)
			{
				settingsWindowViewModel.ShowAlert($"Skip Launcher - Created '{steamAppidPath}'", AlertType.Success, 10);
			}
			else
			{
				ShowAlert($"Skip Launcher - Created '{steamAppidPath}'", AlertType.Success, 10);
			}
		}
	}

	private void RemoveSteamApiTextFile(string exePath)
	{
		var binFolder = Path.GetDirectoryName(exePath);
		var steamAppidPath = Path.Join(binFolder, "steam_appid.txt");
		if (File.Exists(steamAppidPath))
		{
			try
			{
				File.Delete(steamAppidPath);

				if (Settings.SettingsWindowIsOpen && Services.Get<SettingsWindowViewModel>() is SettingsWindowViewModel settingsWindowViewModel)
				{
					settingsWindowViewModel.ShowAlert($"Skip Launcher - Deleted '{steamAppidPath}'", AlertType.Danger, 10);
				}
				else
				{
					ShowAlert($"Skip Launcher - Deleted '{steamAppidPath}'", AlertType.Danger, 10);
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Failed to delete '{steamAppidPath}':\n{ex}");
			}
		}
	}

	private bool _saveInsanityCheck;

	private void WriteInsanityCheck()
	{
		Settings.ExtenderSettings.InsanityCheck = true;
		var gameExeDirectory = Path.GetDirectoryName(Settings.GameExecutablePath.ToRealPath());
		var outputFile = Path.Join(gameExeDirectory, DivinityApp.EXTENDER_CONFIG_FILE);
		try
		{
			JsonSerializerSettings exportSettings = new()
			{
				DefaultValueHandling = Settings.ExtenderSettings.ExportDefaultExtenderSettings ? DefaultValueHandling.Include : DefaultValueHandling.Ignore,
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.Indented
			};
			var contents = JsonConvert.SerializeObject(Settings.ExtenderSettings, exportSettings);
			File.WriteAllText(outputFile, contents);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error saving Script Extender settings to '{outputFile}':\n{ex}");
		}
	}

	private void InitSettingsBindings()
	{
		DivinityApp.DependencyFilter = Settings.WhenAnyValue(x => x.DebugModeEnabled).Select(MakeDependencyFilter);

		var canOpenGameExe = Settings.WhenAnyValue(x => x.GameExecutablePath, x => x.FileExists());

		var canDownloadScriptExtender = this.WhenAnyValue(x => x.PathwayData.ScriptExtenderLatestReleaseUrl, (p) => !String.IsNullOrEmpty(p));
		Keys.DownloadScriptExtender.AddAction(() => AskToDownloadScriptExtender(), canDownloadScriptExtender);

		var canOpenModsFolder = this.WhenAnyValue(x => x.PathwayData.AppDataModsPath, x => x.DirectoryExists());
		Keys.OpenModsFolder.AddAction(() =>
		{
			ProcessHelper.TryOpenPath(PathwayData.AppDataModsPath, Directory.Exists);
		}, canOpenModsFolder);

		var canOpenGameFolder = Settings.WhenAnyValue(x => x.GameExecutablePath, x => x.FileExists());
		Keys.OpenGameFolder.AddAction(() =>
		{
			ProcessHelper.TryOpenPath(Path.GetDirectoryName(Settings.GameExecutablePath), Directory.Exists);
		}, canOpenGameFolder);

		//var canOpenLogsFolder = Settings.WhenAnyValue(x => x.ExtenderLogDirectory).Select(StringExtensions.DirectoryExists);
		Keys.OpenLogsFolder.AddAction(() =>
		{
			if (!string.IsNullOrWhiteSpace(Settings.ExtenderLogDirectory) && !Directory.Exists(Settings.ExtenderLogDirectory))
			{
				try
				{
					Directory.CreateDirectory(Settings.ExtenderLogDirectory);
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error creating logs directory at '{Settings.ExtenderLogDirectory}':\n{ex}");
				}
			}
			ProcessHelper.TryOpenPath(Settings.ExtenderLogDirectory, Directory.Exists);
		});

		Keys.LaunchGame.AddAction(() =>
		{
			DeleteModCrashSanityCheck();

			if (Settings.DisableLauncherTelemetry || Settings.DisableLauncherModWarnings)
			{
				RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, t) =>
				{
					await DivinityModDataLoader.UpdateLauncherPreferencesAsync(GetLarianStudiosAppDataFolder(), !Settings.DisableLauncherTelemetry, !Settings.DisableLauncherModWarnings);
				});
			}

			var launchParams = !String.IsNullOrEmpty(Settings.GameLaunchParams) ? Settings.GameLaunchParams : "";

			if (Settings.GameStoryLogEnabled && launchParams.IndexOf("storylog") < 0)
			{
				if (String.IsNullOrWhiteSpace(launchParams))
				{
					launchParams = "-storylog 1";
				}
				else
				{
					launchParams = launchParams + " " + "-storylog 1";
				}
			}

			/*if (Settings.SkipLauncher && launchParams.IndexOf("skip-launcher") < 0)
			{
				if (String.IsNullOrWhiteSpace(launchParams))
				{
					launchParams = "--skip-launcher";
				}
				else
				{
					launchParams = "--skip-launcher " + launchParams;
				}
			}*/

			if (Settings.LaunchType == LaunchGameType.Exe)
			{
				var exePath = Environment.ExpandEnvironmentVariables(Settings.GameExecutablePath);
				var exeDir = Path.GetDirectoryName(exePath);

				if (Settings.LaunchDX11)
				{
					var nextExe = Path.Combine(exeDir, "bg3_dx11.exe");
					if (File.Exists(nextExe))
					{
						exePath = nextExe;
					}
				}

				if (!exePath.FileExists())
				{
					if (string.IsNullOrWhiteSpace(exePath))
					{
						ShowAlert("No game executable path set", AlertType.Danger, 30);
					}
					else
					{
						ShowAlert($"Failed to find game exe at, \"{exePath}\"", AlertType.Danger, 90);
					}
					return;
				}

				DivinityApp.Log($"Opening game exe at: {exePath} with args {launchParams}");
				TryStartGameExe(exePath, exeDir, launchParams);
			}
			else if(Settings.LaunchType == LaunchGameType.Steam)
			{
				var appid = AppSettings.DefaultPathways.Steam.AppID ?? "1086940";
				var steamUrl = $"steam://run/{appid}//{launchParams}";
				DivinityApp.Log($"Opening game through steam via '{steamUrl}'");
				ProcessHelper.TryOpenUrl(steamUrl);
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(Settings.CustomLaunchAction))
				{
					var args = Settings.CustomLaunchArgs;
					DivinityApp.Log($"Running custom launch action '{Settings.CustomLaunchAction}' with args ({args})");
					try
					{
						ProcessHelper.TryRunCommand(Settings.CustomLaunchAction, Settings.CustomLaunchArgs);
					}
					catch (Exception ex)
					{
						var msg = $"Error running custom launch '{Settings.CustomLaunchAction}' with args '{Settings.CustomLaunchArgs}':\n{ex}";
						DivinityApp.Log(msg);
						var result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, msg, "Custom Launch Error", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, Window.MessageBoxStyle);
					}
				}
				else
				{
					ShowAlert("The 'Launch - Custom Action' is empty. Set it in the preferences.", AlertType.Warning, 30);
				}
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

		}, canOpenGameExe);

		Settings.WhenAnyValue(x => x.LogEnabled).Subscribe(Window.ToggleLogging);

		Settings.WhenAnyValue(x => x.DarkThemeEnabled).Skip(1).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
		{
			View.UpdateColorTheme(b);
			if (IsInitialized) SaveSettings();
		});

		// Updating extender requirement display
		Settings.WhenAnyValue(x => x.ExtenderSettings.EnableExtensions).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
		{
			UpdateExtenderVersionForAllMods();
		});

		var actionLaunchChanged = Settings.WhenAnyValue(x => x.ActionOnGameLaunch).Skip(1).ObserveOn(RxApp.MainThreadScheduler);
		actionLaunchChanged.Subscribe((action) =>
		{
			if (!Window.SettingsWindow.IsVisible && IsInitialized)
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

		Settings.WhenAnyValue(x => x.DocumentsFolderPathOverride).Subscribe((x) =>
		{
			if (!IsLocked && IsInitialized)
			{
				SetGamePathways(Settings.GameDataPath, x);
			}
		});

		Settings.WhenAnyValue(x => x.NexusModsAPIKey).Subscribe((key) =>
		{
			if (String.IsNullOrEmpty(key))
			{
				NexusModsDataLoader.Dispose();
			}
			else
			{
				NexusModsDataLoader.Init(key, AppTitle, Version.ToString());
			}
		});

		Settings.WhenAnyValue(x => x.SaveWindowLocation).Subscribe(Window.ToggleWindowPositionSaving);

		Settings.WhenAnyValue(x => x.EnableColorblindSupport).Skip(1).ObserveOn(RxApp.MainThreadScheduler).Subscribe(b =>
		{
			foreach(var mod in mods.Items)
			{
				mod.HasColorblindSupport = b;
			}
		});

		Settings.WhenAnyValue(x => x.LaunchType, x => x.GameExecutablePath)
		.Throttle(TimeSpan.FromMilliseconds(250))
		.ObserveOn(RxApp.MainThreadScheduler)
		.Subscribe(x =>
		{
			var exePath = x.Item2.ToRealPath();

			if (File.Exists(exePath))
			{
				if (x.Item1 == LaunchGameType.Exe)
				{
					CreateSteamApiTextFile(exePath);
				}
				else if (Settings.SettingsWindowIsOpen)
				{
					//RemoveSteamApiTextFile(exePath);
				}
			}
		});

		Settings.WhenAnyValue(x => x.DeleteModCrashSanityCheck, x => x.GameExecutablePath).ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
		{
			if (x.Item1 && !string.IsNullOrEmpty(x.Item2) && Settings.ExtenderSettings.InsanityCheck != true && !Settings.SettingsWindowIsOpen)
			{
				if (!IsInitialized)
				{
					_saveInsanityCheck = true;
					return;
				}
				WriteInsanityCheck();
			}
		});
	}

	private bool LoadSettings()
	{
		var loaded = false;
		var settingsFile = DivinityApp.GetAppDirectory("Data", "settings.json");
		try
		{
			if (File.Exists(settingsFile))
			{
				DivinityApp.Log($"Loading settings file: '{settingsFile}'");
				using var reader = File.OpenText(settingsFile);
				var fileText = reader.ReadToEnd();
				var settings = DivinityJsonUtils.SafeDeserialize<DivinityModManagerSettings>(fileText, _managerSerializerSettings);
				if (settings != null)
				{
					loaded = true;
					Settings.SetFrom<DivinityModManagerSettings, ReactiveAttribute>(settings);
					Settings.ExtenderSettings.SetFrom(settings.ExtenderSettings);
					Settings.ExtenderUpdaterSettings.SetFrom(settings.ExtenderUpdaterSettings);
				}
			}
		}
		catch (Exception ex)
		{
			ShowAlert($"Error loading settings at '{settingsFile}': {ex}", AlertType.Danger);
		}

		LoadAppConfig();

		Settings.DefaultExtenderLogDirectory = Path.Combine(GetLarianStudiosAppDataFolder(), "Baldur's Gate 3", "Script Extender Logs");

		var nexusModsSupportEnabled = AppSettings.FeatureEnabled("NexusMods");

		if (DivinityApp.NexusModsEnabled != nexusModsSupportEnabled)
		{
			DivinityApp.NexusModsEnabled = nexusModsSupportEnabled;

			foreach (var mod in mods.Items)
			{
				mod.NexusModsEnabled = DivinityApp.NexusModsEnabled;
			}
		}

		if (Settings.LogEnabled)
		{
			Window.ToggleLogging(true);
		}

		SetGamePathways(Settings.GameDataPath, Settings.DocumentsFolderPathOverride);

		if (loaded)
		{
			Settings.CanSaveSettings = false;
			this.RaisePropertyChanged(nameof(Settings));
		}
		else
		{
			//Try to load initial settings from existing files

			var gameExeDirectory = Path.GetDirectoryName(Settings.GameExecutablePath.ToRealPath());
			var extenderConfigPath = Path.Join(gameExeDirectory, DivinityApp.EXTENDER_CONFIG_FILE);
			var extenderUpdaterConfigPath = Path.Join(gameExeDirectory, DivinityApp.EXTENDER_UPDATER_CONFIG_FILE);

			if (extenderConfigPath.FileExists())
			{
				try
				{
					var extenderConfig = DivinityJsonUtils.SafeDeserialize<ScriptExtenderSettings>(extenderConfigPath);
					if(extenderConfig != null)
					{
						Settings.ExtenderSettings.SetFrom(extenderConfig);
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error reading Script Extender Config at '{extenderConfigPath}':\n{ex}");
				}
			}

			if(extenderUpdaterConfigPath.FileExists())
			{
				try
				{
					var extenderUpdaterConfig = DivinityJsonUtils.SafeDeserialize<ScriptExtenderUpdateConfig>(extenderUpdaterConfigPath);
					if(extenderUpdaterConfig != null)
					{
						Settings.ExtenderUpdaterSettings.SetFrom(extenderUpdaterConfig);
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error reading Script Extender Updater Config at '{extenderConfigPath}':\n{ex}");
				}
			}

			SaveSettings();
		}

		if (!string.IsNullOrEmpty(Settings.GameExecutablePath) && Settings.LaunchType == LaunchGameType.Exe)
		{
			CreateSteamApiTextFile(Settings.GameExecutablePath.ToRealPath());
		}

		if(_saveInsanityCheck)
		{
			WriteInsanityCheck();
			_saveInsanityCheck = false;
		}

		return loaded;
	}

	private void OnOrderNameChanged(object sender, OrderNameChangedArgs e)
	{
		if (Settings.LastOrder == e.LastName && IsInitialized)
		{
			Settings.LastOrder = e.NewName;
			SaveSettings();
		}
	}

	private static readonly JsonSerializerSettings _managerSerializerSettings = new()
	{
		Formatting = Formatting.Indented,
		DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
		{
			DivinityApp.Log(args.ErrorContext.Error.Message);
			args.ErrorContext.Handled = true;
		},
		Converters = [ new Newtonsoft.Json.Converters.StringEnumConverter() ]
	};

	public bool SaveSettings()
	{
		string settingsFile = DivinityApp.GetAppDirectory("Data", "settings.json");

		try
		{
#if DEBUG
			DivinityApp.Log($"Saving settings to '{settingsFile}'");
#endif
			Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));
			string contents = JsonConvert.SerializeObject(Settings, Formatting.Indented, _managerSerializerSettings);
			File.WriteAllText(settingsFile, contents);
			Settings.CanSaveSettings = false;
			if (!Keys.SaveKeybindings(out var errorMsg))
			{
				ShowAlert(errorMsg, AlertType.Danger);
			}
			return true;
		}
		catch (Exception ex)
		{
			ShowAlert($"Error saving settings at '{settingsFile}': {ex}", AlertType.Danger);
		}
		return false;
	}

	private IDisposable _deferSave;

	public void QueueSave()
	{
		_deferSave?.Dispose();
		if (!IsInitialized && IsRefreshing) return;
		_deferSave = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () => SaveSettings());
	}

	private string GetLarianStudiosAppDataFolder()
	{
		if(!string.IsNullOrEmpty(Settings.DocumentsFolderPathOverride))
		{
			var appDataPath = Settings.DocumentsFolderPathOverride;
			if (!Path.IsPathRooted(appDataPath))
			{
				appDataPath = DivinityApp.GetAppDirectory(appDataPath);
			}
			if(appDataPath.IsExistingDirectory() && DivinityFileUtils.TryGetParent(appDataPath, out var parentDir))
			{
				return parentDir;
			}
		}

		if (PathwayData.AppDataGameFolder.IsExistingDirectory() && DivinityFileUtils.TryGetParent(PathwayData.AppDataGameFolder, out var parentPathwayDir))
		{
			return parentPathwayDir;
		}

		var appDataEnvFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
		if (!appDataEnvFolder.IsExistingDirectory())
		{
			var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
			if (userFolder.IsExistingDirectory())
			{
				appDataEnvFolder = Path.Join(userFolder, "AppData", "Local", "Larian Studios");
			}
		}
		else
		{
			appDataEnvFolder = Path.Join(appDataEnvFolder, "Larian Studios");
		}
		return appDataEnvFolder;
	}

	private void SetGamePathways(string currentGameDataPath, string appDataGameFolderOverride = "")
	{
		try
		{
			string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

			if (!string.IsNullOrEmpty(currentGameDataPath)) currentGameDataPath = Environment.ExpandEnvironmentVariables(currentGameDataPath);

			if (String.IsNullOrWhiteSpace(AppSettings.DefaultPathways.DocumentsGameFolder))
			{
				AppSettings.DefaultPathways.DocumentsGameFolder = "Larian Studios\\Baldur's Gate 3";
			}

			//Make path relative if the path isn't rooted
			if (!String.IsNullOrWhiteSpace(appDataGameFolderOverride) && !Path.IsPathRooted(appDataGameFolderOverride))
			{
				appDataGameFolderOverride = DivinityApp.GetAppDirectory(appDataGameFolderOverride);
				if (!Directory.Exists(appDataGameFolderOverride)) Directory.CreateDirectory(appDataGameFolderOverride);
			}

			string appDataGameFolder = Path.Combine(localAppDataFolder, AppSettings.DefaultPathways.DocumentsGameFolder);

			if (appDataGameFolderOverride.DirectoryExists() && DivinityFileUtils.TryGetParent(appDataGameFolderOverride, out var parentDir))
			{
				appDataGameFolder = appDataGameFolderOverride;
				localAppDataFolder = parentDir;
			}
			else if (!appDataGameFolder.IsExistingDirectory())
			{
				var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
				if (userFolder.IsExistingDirectory())
				{
					localAppDataFolder = Path.Combine(userFolder, "AppData", "Local");
					appDataGameFolder = Path.Combine(localAppDataFolder, AppSettings.DefaultPathways.DocumentsGameFolder);
				}
			}

			string modPakFolder = Path.Combine(appDataGameFolder, "Mods");
			string profileFolder = Path.Combine(appDataGameFolder, "PlayerProfiles");

			PathwayData.AppDataGameFolder = appDataGameFolder;
			PathwayData.AppDataModsPath = modPakFolder;
			PathwayData.AppDataProfilesPath = profileFolder;

			if (Directory.Exists(localAppDataFolder))
			{
				Directory.CreateDirectory(appDataGameFolder);
				DivinityApp.Log($"Larian folder set to '{appDataGameFolder}'.");

				if (!Directory.Exists(modPakFolder))
				{
					DivinityApp.Log($"No mods folder found at '{modPakFolder}'. Creating folder.");
					Directory.CreateDirectory(modPakFolder);
				}

				if (!Directory.Exists(profileFolder))
				{
					DivinityApp.Log($"No PlayerProfiles folder found at '{profileFolder}'. Creating folder.");
					Directory.CreateDirectory(profileFolder);
				}
			}
			else
			{
				ShowAlert("Failed to find %LOCALAPPDATA% folder - This is weird", AlertType.Danger);
				DivinityApp.Log($"Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify) return a non-existent path?\nResult({Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify)})");
			}

			if (string.IsNullOrWhiteSpace(currentGameDataPath) || !Directory.Exists(currentGameDataPath))
			{
				string installPath = DivinityRegistryHelper.GetGameInstallPath(AppSettings.DefaultPathways.Steam.RootFolderName,
					AppSettings.DefaultPathways.GOG.Registry_32, AppSettings.DefaultPathways.GOG.Registry_64);

				if (!String.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
				{
					PathwayData.InstallPath = installPath;
					if (!File.Exists(Settings.GameExecutablePath))
					{
						string exePath = "";
						if (!DivinityRegistryHelper.IsGOG)
						{
							exePath = Path.Combine(installPath, AppSettings.DefaultPathways.Steam.ExePath);
						}
						else
						{
							exePath = Path.Combine(installPath, AppSettings.DefaultPathways.GOG.ExePath);
						}
						if (File.Exists(exePath))
						{
							Settings.GameExecutablePath = exePath.Replace("\\", "/");
							DivinityApp.Log($"Exe path set to '{exePath}'.");
						}
					}

					string gameDataPath = Path.Combine(installPath, AppSettings.DefaultPathways.GameDataFolder).Replace("\\", "/");
					if (Directory.Exists(gameDataPath))
					{
						DivinityApp.Log($"Set game data path to '{gameDataPath}'.");
						Settings.GameDataPath = gameDataPath;
						currentGameDataPath = gameDataPath;
					}
					else
					{
						DivinityApp.Log($"Failed to find game data path at '{gameDataPath}'.");
					}
				}
			}
			else
			{
				var installPath = Path.GetFullPath(Path.Combine(currentGameDataPath, @"..\..\"));
				PathwayData.InstallPath = installPath;
				if (!File.Exists(currentGameDataPath))
				{
					string exePath = "";
					if (!DivinityRegistryHelper.IsGOG)
					{
						exePath = Path.Combine(installPath, AppSettings.DefaultPathways.Steam.ExePath);
					}
					else
					{
						exePath = Path.Combine(installPath, AppSettings.DefaultPathways.GOG.ExePath);
					}
					if (File.Exists(exePath))
					{
						Settings.GameExecutablePath = exePath.Replace("\\", "/");
						DivinityApp.Log($"Exe path set to '{exePath}'.");
					}
				}
			}

			if (!Directory.Exists(currentGameDataPath) || !File.Exists(Settings.GameExecutablePath))
			{
				DivinityApp.Log("Failed to find game data path. Asking user for help.");

				var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog()
				{
					Multiselect = false,
					Description = "Set the path to the Baldur's Gate 3 root installation folder",
					UseDescriptionForTitle = true,
					SelectedPath = GetInitialStartingDirectory()
				};

				if (dialog.ShowDialog(Window) == true)
				{
					var dataDirectory = Path.Combine(dialog.SelectedPath, AppSettings.DefaultPathways.GameDataFolder);
					var exePath = Path.Combine(dialog.SelectedPath, AppSettings.DefaultPathways.Steam.ExePath);
					if (!File.Exists(exePath))
					{
						exePath = Path.Combine(dialog.SelectedPath, AppSettings.DefaultPathways.GOG.ExePath);
					}
					if (Directory.Exists(dataDirectory))
					{
						Settings.GameDataPath = dataDirectory;
					}
					else
					{
						ShowAlert("Failed to find Data folder with given installation directory", AlertType.Danger);
					}
					if (File.Exists(exePath))
					{
						Settings.GameExecutablePath = exePath;
					}
					PathwayData.InstallPath = dialog.SelectedPath;
				}
			}

			if (AppSettings.FeatureEnabled("ScriptExtender") && IsInitialized && !IsRefreshing)
			{
				LoadExtenderSettingsBackground();
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error setting up game pathways: {ex}");
		}
	}

	private void SetLoadedMods(IEnumerable<DivinityModData> loadedMods)
	{
		var uuids = loadedMods.Select(x => x.UUID).ToHashSet();
		mods.Clear();
		foreach (var mod in loadedMods)
		{
			mod.NexusModsEnabled = DivinityApp.NexusModsEnabled;
			mod.HasColorblindSupport = Settings.EnableColorblindSupport;

			if (mod.IsLarianMod)
			{
				var existingIgnoredMod = DivinityApp.IgnoredMods.Lookup(mod.UUID);
				if (existingIgnoredMod.HasValue && existingIgnoredMod.Value != mod)
				{
					DivinityApp.IgnoredMods.Remove(existingIgnoredMod.Value);
				}
				DivinityApp.IgnoredMods.AddOrUpdate(mod);
			}

			if (TryGetMod(mod.UUID, out var existingMod))
			{
				if (mod.Version.VersionInt > existingMod.Version.VersionInt)
				{
					mods.AddOrUpdate(mod);
					DivinityApp.Log($"Updated mod data from pak: Name({mod.Name}) UUID({mod.UUID}) Type({mod.ModType}) Version({mod.Version.VersionInt})");
				}
			}
			else
			{
				mods.AddOrUpdate(mod);
			}

			mod.MissingDependencies.Clear();
			foreach (var dep in mod.Dependencies.Items)
			{
				if(!uuids.Contains(dep.UUID) && !DivinityModDataLoader.IgnoreModDependency(dep.UUID))
				{
					mod.MissingDependencies.AddOrUpdate(dep);
				}
			}
		}
	}

	private void MergeModLists(List<DivinityModData> finalMods, List<DivinityModData> newMods)
	{
		foreach (var mod in newMods)
		{
			var existing = finalMods.FirstOrDefault(x => x.UUID == mod.UUID);
			if (existing != null)
			{
				if (existing.Version.VersionInt < mod.Version.VersionInt || mod.IsEditorMod)
				{
					finalMods.Remove(existing);
					finalMods.Add(mod);
				}
			}
			else
			{
				finalMods.Add(mod);
			}
		}
	}

	private CancellationTokenSource GetCancellationToken(int delay, CancellationTokenSource last = null)
	{
		CancellationTokenSource token = new CancellationTokenSource();
		if (last != null && last.IsCancellationRequested)
		{
			last.Dispose();
		}
		token.CancelAfter(delay);
		return token;
	}

	private static async Task<TResult> RunTask<TResult>(Task<TResult> task, TResult defaultValue)
	{
		try
		{
			return await task;
		}
		catch (OperationCanceledException)
		{
			DivinityApp.Log("Operation timed out/canceled.");
		}
		catch (TimeoutException)
		{
			DivinityApp.Log("Operation timed out.");
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error awaiting task:\n{ex}");
		}
		return defaultValue;
	}

	private static async Task<TResult> RunTaskStep<TResult>(Func<double, Task<TResult>> taskFunc, double stepAmount, TResult defaultValue)
	{
		try
		{
			var result = await taskFunc.Invoke(stepAmount);
			return result;
		}
		catch (OperationCanceledException)
		{
			DivinityApp.Log("Operation timed out/canceled.");
		}
		catch (TimeoutException)
		{
			DivinityApp.Log("Operation timed out.");
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error awaiting task:\n{ex}");
		}
		return defaultValue;
	}

	public async Task<List<DivinityModData>> LoadModsAsync(double taskStepAmount = 0.1d)
	{
		List<DivinityModData> finalMods = [];
		ModLoadingResults modLoadingResults = null;
		List<DivinityModData> projects = null;
		List<DivinityModData> baseMods = null;

		var cancelTokenSource = GetCancellationToken(int.MaxValue);
		CanCancelProgress = false;

		GameDirectoryFound = !String.IsNullOrWhiteSpace(Settings.GameDataPath) && Directory.Exists(Settings.GameDataPath);

		if (GameDirectoryFound)
		{
			DivinityApp.Log($"Loading base game mods from data folder...");
			await SetMainProgressTextAsync("Loading base game mods from data folder...");
			DivinityApp.Log($"GameDataPath is '{Settings.GameDataPath}'.");
			cancelTokenSource = GetCancellationToken(30000);
			baseMods = await RunTask(DivinityModDataLoader.LoadBuiltinModsAsync(Settings.GameDataPath, cancelTokenSource.Token), null);
			cancelTokenSource = GetCancellationToken(int.MaxValue);
			await IncreaseMainProgressValueAsync(taskStepAmount);

			string modsDirectory = Path.Combine(Settings.GameDataPath, "Mods");
			if (Directory.Exists(modsDirectory))
			{
				DivinityApp.Log($"Loading mod projects from '{modsDirectory}'.");
				await SetMainProgressTextAsync("Loading editor project mods...");
				cancelTokenSource = GetCancellationToken(30000);
				projects = await RunTask(DivinityModDataLoader.LoadEditorProjectsAsync(modsDirectory, cancelTokenSource.Token), null);
				cancelTokenSource = GetCancellationToken(int.MaxValue);
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}
		}

		baseMods ??= [];

		if (!GameDirectoryFound || baseMods.Count < DivinityApp.IgnoredMods.Count || !baseMods.Any(x => x.UUID == DivinityApp.MAIN_CAMPAIGN_UUID))
		{
			if (baseMods.Count == 0)
			{
				baseMods.AddRange(DivinityApp.IgnoredMods.Items);
			}
			else
			{
				foreach (var mod in DivinityApp.IgnoredMods.Items)
				{
					if (!baseMods.Any(x => x.UUID == mod.UUID)) baseMods.Add(mod);
				}
			}
		}

		if (Directory.Exists(PathwayData.AppDataModsPath))
		{
			DivinityApp.Log($"Loading mods from '{PathwayData.AppDataModsPath}'.");
			await SetMainProgressTextAsync("Loading mods from Local AppData folder...");
			cancelTokenSource.CancelAfter(TimeSpan.FromMinutes(10));
			modLoadingResults = await RunTask(DivinityModDataLoader.LoadModPackageDataAsync(PathwayData.AppDataModsPath, cancelTokenSource.Token), null);
			cancelTokenSource = GetCancellationToken(int.MaxValue);
			await IncreaseMainProgressValueAsync(taskStepAmount);
		}

		if (baseMods != null) MergeModLists(finalMods, baseMods);
		if (modLoadingResults != null)
		{
			//Add duplicate paks in the Data folder to the duplicates list
			var duplicateProjects = new List<string>();
			foreach (var group in baseMods.GroupBy(x => x.UUID))
			{
				if(group.Count() > 1)
				{
					var dupeMods = group.ToArray();
					duplicateProjects.Add(string.Join(Environment.NewLine, dupeMods.Select(x => $"{x.Name} [{x.UUID}]:\nData\\{x.FilePath}\n")));
				}
			}
			if(duplicateProjects.Count > 0)
			{
				var message = string.Join(Environment.NewLine, duplicateProjects);
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					var result = Xceed.Wpf.Toolkit.MessageBox.Show(Window,
					$"Duplicate toolkit projects were found in the Data folder:\n\n{message}",
					"Duplicate Toolkit Projects",
					MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, Window.MessageBoxStyle);
					ShowAlert($"Found duplicate toolkit mods in the Data folder", AlertType.Danger, 60);
				});
			}
			var baseModsDict = baseMods.DistinctBy(x => x.UUID).ToDictionary(x => x.UUID, x => x);
			foreach (var pakMod in modLoadingResults.Mods)
			{
				if (baseModsDict.TryGetValue(pakMod.UUID, out var baseMod) && !baseMod.IsEditorMod)
				{
					modLoadingResults.Duplicates.Add(baseMod);
				}
			}
			var dupeCount = modLoadingResults.Duplicates.Count;
			if (dupeCount > 0)
			{
				await Observable.Start(() =>
				{
					ShowAlert($"{dupeCount} duplicate mod(s) found", AlertType.Danger, 30);
					DeleteMods(modLoadingResults.Duplicates, true, modLoadingResults.Mods);
				}, RxApp.MainThreadScheduler);
			}
			MergeModLists(finalMods, modLoadingResults.Mods);
		}
		if (projects != null) MergeModLists(finalMods, projects);

		DivinityApp.Log($"Loaded '{finalMods.Count}' mods.");
		return [.. finalMods.OrderBy(m => m.Name)];
	}

	public bool ModIsAvailable(IDivinityModData divinityModData)
	{
		return mods.Items.Any(k => k.UUID == divinityModData.UUID)
			|| DivinityApp.IgnoredMods.Lookup(divinityModData.UUID).HasValue
			|| DivinityApp.IgnoredDependencyMods.Contains(divinityModData.UUID);
	}

	public async Task<List<DivinityProfileData>> LoadProfilesAsync()
	{
		if (Directory.Exists(PathwayData.AppDataProfilesPath))
		{
			DivinityApp.Log($"Loading profiles from '{PathwayData.AppDataProfilesPath}'.");

			var profiles = await DivinityModDataLoader.LoadProfileDataAsync(PathwayData.AppDataProfilesPath);
			DivinityApp.Log($"Loaded '{profiles.Count}' profiles.");
			if (profiles.Count > 0)
			{
				DivinityApp.Log(String.Join(Environment.NewLine, profiles.Select(x => $"{x.Name} | {x.UUID}")));
			}
			return profiles;
		}
		else
		{
			DivinityApp.Log($"Profile folder not found at '{PathwayData.AppDataProfilesPath}'.");
		}
		return null;
	}

	private static readonly DivinityLoadOrder _fallbackOrder = new DivinityLoadOrder()
	{
		Name = "Current",
		FilePath = "%LOCALAPPDATA%\\Larian Studios\\Baldur's Gate 3\\PlayerProfiles\\Public\\modsettings.lsx",
		IsModSettings = true
	};

	public void BuildModOrderList(int selectIndex = -1, string lastOrderName = "")
	{
		if (SelectedProfile == null)
		{
			//Fallback
			TryLoadFallbackOrder();
			return;
		}

		IsLoadingOrder = true;

		var currentOrder = new DivinityLoadOrder()
		{
			Name = "Current",
			FilePath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx"),
			IsModSettings = true
		};

		var orderNames = new List<string>();
		var nextOrders = new List<DivinityLoadOrder>();
		var profileMods = SelectedProfile.ActiveMods.ToList();
		var savedOrders = SavedModOrderList.ToList();

		foreach (var activeMod in profileMods)
		{
			orderNames.Add(activeMod.Name ?? activeMod.UUID);
			var mod = mods.Lookup(activeMod.UUID);
			if (mod.HasValue)
			{
				currentOrder.Add(mod.Value);
			}
			else
			{
				currentOrder.Add(activeMod, false, true);
			}
		}

		DivinityApp.Log($"Profile order: {string.Join(";", orderNames)}");

		nextOrders.Add(currentOrder);

		var lastExported = savedOrders.FirstOrDefault(x => x.Name == "LastExported");
		if(lastExported != null)
		{
			nextOrders.Add(lastExported);
			savedOrders.Remove(lastExported);
		}

		nextOrders.AddRange(savedOrders);

		if (!string.IsNullOrEmpty(lastOrderName))
		{
			int lastOrderIndex = nextOrders.IndexOf(nextOrders.FirstOrDefault(x => x.Name == lastOrderName));
			if (lastOrderIndex != -1) selectIndex = lastOrderIndex;
		}

		ModOrderList.Clear();
		ModOrderList.Add(nextOrders);

		if (selectIndex != -1)
		{
			if (selectIndex >= ModOrderList.Count) selectIndex = ModOrderList.Count - 1;
			DivinityApp.Log($"Setting next order index to [{selectIndex}/{ModOrderList.Count - 1}].");
			try
			{
				SelectedModOrderIndex = selectIndex;
				var nextOrder = ModOrderList.ElementAtOrDefault(selectIndex);

				LoadModOrder(nextOrder);

				Settings.LastOrder = nextOrder?.Name;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error setting next load order:\n{ex}");
			}
		}
		IsLoadingOrder = false;
	}

	private async Task<ImportOperationResults> AddModFromFile(Dictionary<string, DivinityModData> builtinMods, ImportOperationResults taskResult, string filePath, CancellationToken cts, bool? toActiveList = null)
	{
		var ext = Path.GetExtension(filePath).ToLower();
		if (ext.Equals(".pak", StringComparison.OrdinalIgnoreCase))
		{
			var outputFilePath = Path.Combine(PathwayData.AppDataModsPath, Path.GetFileName(filePath));
			try
			{
				taskResult.TotalPaks++;

				if (await DivinityFileUtils.CopyFileAsync(filePath, outputFilePath, cts))
				{
					var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, builtinMods, cts);
					if (mod != null)
					{
						taskResult.Mods.Add(mod);
						await Observable.Start(() =>
						{
							AddImportedMod(mod, toActiveList);
							return Unit.Default;
						}, RxApp.MainThreadScheduler);
					}
				}
			}
			catch (IOException ex)
			{
				DivinityApp.Log($"File may be in use by another process:\n{ex}");
				ShowAlert($"Failed to copy file '{Path.GetFileName(filePath)} - It may be locked by another process'", AlertType.Danger);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading file ({filePath}):\n{ex}");
			}
		}
		else if (_archiveFormats.Contains(ext, StringComparer.OrdinalIgnoreCase))
		{
			await ImportArchiveAsync(builtinMods, taskResult, filePath, true, cts, toActiveList);
		}
		else if (_compressedFormats.Contains(ext, StringComparer.OrdinalIgnoreCase))
		{
			await ImportCompressedFileAsync(builtinMods, taskResult, filePath, ext, true, cts, toActiveList);
		}
		return taskResult;
	}

	public void ImportMods(IEnumerable<string> files, bool? toActiveList = null)
	{
		if (!MainProgressIsActive)
		{
			MainProgressTitle = "Importing mods.";
			MainProgressWorkText = "";
			MainProgressValue = 0d;
			MainProgressIsActive = true;
			IsRefreshing = true;
			var result = new ImportOperationResults()
			{
				TotalFiles = files.Count()
			};

			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				var builtinMods = DivinityApp.IgnoredMods.Items.SafeToDictionary(x => x.Folder, x => x);
				MainProgressToken = new CancellationTokenSource();
				foreach (var f in files)
				{
					await AddModFromFile(builtinMods, result, f, MainProgressToken.Token, toActiveList);
				}

				if (UpdateHandler.Nexus.IsEnabled && result.Mods.Count > 0 && result.Mods.Any(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START))
				{
					await UpdateHandler.Nexus.Update(result.Mods, MainProgressToken.Token);
				}

				await ctrl.Yield();
				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					IsRefreshing = false;
					OnMainProgressComplete();

					if (result.Errors.Count > 0)
					{
						var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
						var errorOutputPath = DivinityApp.GetAppDirectory("_Logs", $"ImportMods_{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}_Errors.log");
						var logsDir = Path.GetDirectoryName(errorOutputPath);
						if (!Directory.Exists(logsDir))
						{
							Directory.CreateDirectory(logsDir);
						}
						File.WriteAllText(errorOutputPath, String.Join("\n", result.Errors.Select(x => $"File: {x.File}\nError:\n{x.Exception}")));
					}

					var total = result.Mods.Count;
					if (result.Success)
					{
						if (result.Mods.Count > 1)
						{
							ShowAlert($"Successfully imported {total} mods", AlertType.Success, 20);
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
						var selectNext = result.Mods.Select(x => x.UUID).ToHashSet();
						RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(20), () =>
						{
							var selectMods = mods.Items.Where(x => selectNext.Contains(x.UUID));
							DeselectAllMods();
							Layout.DeselectAll();
							Layout.SelectMods(selectMods);
						});
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
				});
				return Disposable.Empty;
			});
		}
	}

	private string GetInitialStartingDirectory(string prioritizePath = "")
	{
		var directory = prioritizePath;

		if (!String.IsNullOrEmpty(prioritizePath) && DivinityFileUtils.TryGetDirectoryOrParent(prioritizePath, out var actualDir))
		{
			directory = actualDir;
		}
		else
		{
			if (!String.IsNullOrEmpty(Settings.LastImportDirectoryPath))
			{
				directory = Settings.LastImportDirectoryPath;
			}

			if (!Directory.Exists(directory) && !String.IsNullOrEmpty(PathwayData.LastSaveFilePath) && DivinityFileUtils.TryGetDirectoryOrParent(PathwayData.LastSaveFilePath, out var lastDir))
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

	private static readonly List<string> _archiveFormats = new() { ".7z", ".7zip", ".gzip", ".rar", ".tar", ".tar.gz", ".zip" };
	private static readonly List<string> _compressedFormats = new() { ".bz2", ".xz", ".zst" };
	private static readonly string _archiveFormatsStr = String.Join(";", _archiveFormats.Select(x => "*" + x));
	private static readonly string _compressedFormatsStr = String.Join(";", _compressedFormats.Select(x => "*" + x));

	public static bool IsImportableFile(string ext)
	{
		return ext == ".pak" || _archiveFormats.Contains(ext) || _compressedFormats.Contains(ext);
	}

	private void OpenModImportDialog()
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".zip",
			Filter = $"All formats (*.pak;{_archiveFormatsStr};{_compressedFormatsStr})|*.pak;{_archiveFormatsStr};{_compressedFormatsStr}|Mod package (*.pak)|*.pak|Archive file ({_archiveFormatsStr})|{_archiveFormatsStr}|Compressed file ({_compressedFormatsStr})|{_compressedFormatsStr}|All files (*.*)|*.*",
			Title = "Import Mods from Archive...",
			ValidateNames = true,
			ReadOnlyChecked = true,
			Multiselect = true,
			InitialDirectory = GetInitialStartingDirectory(Settings.LastImportDirectoryPath)
		};

		if (dialog.ShowDialog(Window) == true)
		{
			var savedDirectory = Path.GetDirectoryName(dialog.FileName);
			if (Settings.LastImportDirectoryPath != savedDirectory)
			{
				Settings.LastImportDirectoryPath = savedDirectory;
				PathwayData.LastSaveFilePath = savedDirectory;
				SaveSettings();
			}

			ImportMods(dialog.FileNames);
		}
	}

	private void AddNewModOrder(DivinityLoadOrder newOrder = null)
	{
		var lastIndex = SelectedModOrderIndex;
		var lastOrders = ModOrderList.ToList();

		var nextOrders = new List<DivinityLoadOrder>();
		nextOrders.AddRange(SavedModOrderList);

		void undo()
		{
			SavedModOrderList.Clear();
			SavedModOrderList.AddRange(lastOrders);
			BuildModOrderList(lastIndex);
		};

		void redo()
		{
			if (newOrder == null)
			{
				newOrder = new DivinityLoadOrder()
				{
					Name = $"New{nextOrders.Count}",
					Order = ActiveMods.Select(m => m.ToOrderEntry()).ToList()
				};
				newOrder.FilePath = Path.Combine(GetOrdersDirectory(), DivinityModDataLoader.MakeSafeFilename(Path.Combine(newOrder.Name + ".json"), '_'));
			}
			SavedModOrderList.Add(newOrder);
			BuildModOrderList(ModOrderList.Count);
		};

		this.CreateSnapshot(undo, redo);

		redo();
	}

	public void DeselectAllMods()
	{
		foreach (var mod in mods.Items)
		{
			mod.IsSelected = false;
		}
	}

	private void TryLoadFallbackOrder()
	{
		try
		{
			LoadModOrder(_fallbackOrder);
			IsLoadingOrder = false;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading fallback order:\n{ex}");
		}
	}

	public bool LoadModOrder(DivinityLoadOrder order)
	{
		if (order == null) return false;

		IsLoadingOrder = true;

		var loadFrom = order.Order;

		foreach (var mod in ActiveMods)
		{
			mod.IsActive = false;
			mod.Index = -1;
		}

		DeselectAllMods();

		DivinityApp.Log($"Loading mod order '{order.Name}'.");

		var loadOrderIndex = 0;

		for (int i = 0; i < loadFrom.Count; i++)
		{
			var entry = loadFrom[i];
			if (!DivinityModDataLoader.IgnoreMod(entry.UUID))
			{
				var modResult = mods.Lookup(entry.UUID);
				if (modResult.HasValue)
				{
					var mod = modResult.Value;
					if (mod.ModType != "Adventure")
					{
						mod.IsActive = true;
						mod.Index = loadOrderIndex;
						if (mod.IsForceLoaded)
						{
							mod.ForceAllowInLoadOrder = true;
						}
						loadOrderIndex += 1;
					}
					else
					{
						var nextIndex = AdventureMods.IndexOf(mod);
						if (nextIndex != -1) SelectedAdventureModIndex = nextIndex;
					}
				}
			}
		}

		ActiveMods.Clear();
		ActiveMods.AddRange(addonMods.Where(x => x.CanAddToLoadOrder && x.IsActive).OrderBy(x => x.Index));
		InactiveMods.Clear();
		InactiveMods.AddRange(addonMods.Where(x => x.CanAddToLoadOrder && !x.IsActive));

		OnFilterTextChanged(ActiveModFilterText, ActiveMods);
		OnFilterTextChanged(InactiveModFilterText, InactiveMods);

		if (Settings?.DisableMissingModWarnings == false)
		{
			DisplayMissingMods(order);
		}

		OrderJustLoaded = true;

		IsLoadingOrder = false;
		return true;
	}

	public void MainWindowMessageBox_Closed_ResetColor(object sender, EventArgs e)
	{
		if (sender is Xceed.Wpf.Toolkit.MessageBox messageBox)
		{
			messageBox.WindowBackground = MainViewControl.MessageBoxDefaultBackgroundBrush;
			messageBox.Closed -= MainWindowMessageBox_Closed_ResetColor;
		}
	}

	private void UpdateModExtenderStatus(DivinityModData mod)
	{
		mod.CurrentExtenderVersion = Settings.ExtenderSettings.ExtenderMajorVersion;

		mod.ExtenderModStatus = DivinityExtenderModStatus.None;

		if (mod.ScriptExtenderData != null && mod.ScriptExtenderData.HasAnySettings)
		{
			if (mod.ScriptExtenderData.Lua)
			{
				if (!Settings.ExtenderSettings.EnableExtensions)
				{
					mod.ExtenderModStatus |= DivinityExtenderModStatus.DisabledFromConfig;
				}
				else
				{
					if (Settings.ExtenderSettings.ExtenderMajorVersion > -1)
					{
						if (mod.ScriptExtenderData.RequiredVersion > -1 && Settings.ExtenderSettings.ExtenderMajorVersion < mod.ScriptExtenderData.RequiredVersion)
						{
							mod.ExtenderModStatus |= DivinityExtenderModStatus.MissingRequiredVersion;
						}
						else
						{
							mod.ExtenderModStatus |= DivinityExtenderModStatus.Fulfilled;
						}
					}
					else
					{
						mod.ExtenderModStatus |= DivinityExtenderModStatus.MissingRequiredVersion;
					}
				}
			}
			else
			{
				mod.ExtenderModStatus |= DivinityExtenderModStatus.Supports;
			}
			if (!Settings.ExtenderUpdaterSettings.UpdaterIsAvailable)
			{
				mod.ExtenderModStatus |= DivinityExtenderModStatus.MissingUpdater;
			}
		}

		// Blinky animation on the tools/download buttons if the extender is required by mods and is missing
		if (mod.ExtenderModStatus.HasFlag(DivinityExtenderModStatus.MissingUpdater))
		{
			HighlightExtenderDownload = true;
		}
	}

	public void UpdateExtenderVersionForAllMods()
	{
		if (Mods.Count > 0)
		{
			HighlightExtenderDownload = false;

			foreach (var mod in Mods)
			{
				UpdateModExtenderStatus(mod);
			}
		}
	}

	private async Task<Unit> SetMainProgressTextAsync(string text)
	{
		return await Observable.Start(() =>
		{
			MainProgressWorkText = text;
			return Unit.Default;
		}, RxApp.MainThreadScheduler);
	}

	private CancellationToken workshopModLoadingCancelToken;

	private readonly List<string> ignoredModProjectNames = new() { "Test", "Debug" };
	private bool CanFetchWorkshopData(DivinityModData mod)
	{
		if (UpdateHandler.Workshop.CacheData.NonWorkshopMods.Contains(mod.UUID))
		{
			return false;
		}
		if (mod.IsEditorMod && (ignoredModProjectNames.Any(x => mod.Folder.IndexOf(x, StringComparison.OrdinalIgnoreCase) > -1) ||
			String.IsNullOrEmpty(mod.Author) || String.IsNullOrEmpty(mod.Description)))
		{
			return false;
		}
		else if (mod.IsLarianMod || String.IsNullOrEmpty(mod.DisplayName))
		{
			return false;
		}
		return String.IsNullOrEmpty(mod.WorkshopData.ID) || !UpdateHandler.Workshop.CacheData.Mods.ContainsKey(mod.UUID);
	}

	private void RefreshAllModUpdatesBackground()
	{
		IsRefreshingModUpdates = true;
		var disposable = RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, cts) =>
		{
			UpdateHandler.Workshop.SteamAppID = AppSettings.DefaultPathways.Steam.AppID;

			UpdateHandler.Nexus.APIKey = Settings.NexusModsAPIKey;
			UpdateHandler.Nexus.AppName = AppTitle;
			UpdateHandler.Nexus.AppVersion = Version.ToString();

			UpdateHandler.Nexus.IsEnabled = DivinityApp.NexusModsEnabled;

			await UpdateHandler.LoadAsync(UserMods, Version.ToString(), cts);
			await UpdateHandler.UpdateAsync(UserMods, cts);
			await UpdateHandler.SaveAsync(UserMods, Version.ToString(), cts);

			IsRefreshingModUpdates = false;
		});
	}

	private async Task CheckForEmptyOrderAsync(IScheduler sch, CancellationToken token)
	{
		if (SelectedProfile == null) return;
		var modSettingsPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
		var modSettingsData = await DivinityModDataLoader.LoadModSettingsFileAsync(modSettingsPath);

		if (modSettingsData.CountActive() <= 0)
		{
			var modSettingsOrder = SavedModOrderList.FirstOrDefault();
			var lastExported = SavedModOrderList.FirstOrDefault(x => x.Name == DivinityApp.PATH_LAST_EXPORTED_NAME);
			if (modSettingsOrder != null && lastExported != null && lastExported.Order.Count > 0)
			{
				var doReset = await Observable.Start(() =>
				{
					MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(Window,
					"It looks like the load order was reset externally. Use the last exported mod order?",
					"Restore Load Order",
					MessageBoxButton.YesNo,
					MessageBoxImage.Warning,
					MessageBoxResult.Yes,
					Window.MessageBoxStyle);
					if (result == MessageBoxResult.Yes)
					{
						return true;
					}
					return false;
				}, RxApp.MainThreadScheduler);

				if (doReset)
				{
					modSettingsOrder.SetOrder(lastExported);
					SelectedModOrder.SetOrder(lastExported);

					List<string> orderList = [];
					if (SelectedAdventureMod != null) orderList.Add(SelectedAdventureMod.UUID);
					orderList.AddRange(SelectedModOrder.Order.Select(x => x.UUID));

					await Observable.Start(() =>
					{
						SelectedProfile.ActiveMods.AddRange(orderList.Select(ProfileActiveModDataFromUUID));
					}, RxApp.MainThreadScheduler);

					var outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
					var finalOrder = DivinityModDataLoader.BuildOutputList(lastExported.Order, mods.Items, Settings.AutoAddDependenciesWhenExporting, SelectedAdventureMod);
					await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, finalOrder);

					await Observable.Start(() =>
					{
						BuildModOrderList(SelectedModOrderIndex);
					}, RxApp.MainThreadScheduler);
				}
			}
		}
	}

	private bool _firstRun = true;

	private async Task<Unit> RefreshAsync(IScheduler ctrl, CancellationToken t)
	{
		DivinityApp.Log($"Refreshing data asynchronously...");

		double taskStepAmount = 1.0 / 10;

		List<DivinityLoadOrderEntry> lastActiveOrder = null;
		string lastOrderName = "";
		if (SelectedModOrder != null)
		{
			lastActiveOrder = SelectedModOrder.Order.ToList();
			lastOrderName = SelectedModOrder.Name;
		}

		string lastAdventureMod = null;
		if (SelectedAdventureMod != null) lastAdventureMod = SelectedAdventureMod.UUID;

		string selectedProfileUUID = "";
		if (SelectedProfile != null)
		{
			selectedProfileUUID = SelectedProfile.UUID;
		}

		if(IsInitialized)
		{
			await Observable.Start(() =>
			{
				LoadSettings();
			}, RxApp.MainThreadScheduler);
		}

		if (Directory.Exists(PathwayData.AppDataGameFolder))
		{
			DivinityApp.Log("Loading mods...");
			await SetMainProgressTextAsync("Loading mods...");
			var loadedMods = await RunTaskStep(LoadModsAsync, taskStepAmount, []);
			await IncreaseMainProgressValueAsync(taskStepAmount);

			DivinityApp.Log("Loading profiles...");
			await SetMainProgressTextAsync("Loading profiles...");
			var loadedProfiles = await RunTask(LoadProfilesAsync(), []);
			await IncreaseMainProgressValueAsync(taskStepAmount);

			if (string.IsNullOrEmpty(selectedProfileUUID) && (loadedProfiles != null && loadedProfiles.Count > 0))
			{
				DivinityApp.Log("Loading current profile...");
				await SetMainProgressTextAsync("Loading current profile...");
				selectedProfileUUID = await RunTask(DivinityModDataLoader.GetSelectedProfileUUIDAsync(PathwayData.AppDataProfilesPath), string.Empty);
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}
			else
			{
				if ((loadedProfiles == null || loadedProfiles.Count == 0))
				{
					DivinityApp.Log("No profiles found?");
				}
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}

			DivinityApp.Log("Loading external load orders...");
			await SetMainProgressTextAsync("Loading external load orders...");
			var savedModOrderList = await RunTask(LoadExternalLoadOrdersAsync(), []);
			await IncreaseMainProgressValueAsync(taskStepAmount);

			if (savedModOrderList.Count > 0)
			{
				DivinityApp.Log($"{savedModOrderList.Count} saved load orders found.");
			}
			else
			{
				DivinityApp.Log("No saved orders found.");
			}

			DivinityApp.Log("Setting up mod lists...");
			await SetMainProgressTextAsync("Setting up mod lists...");

			await Observable.Start(() =>
			{
				LoadAppConfig();
				SetLoadedMods(loadedMods);

				Profiles.AddRange(loadedProfiles);

				SavedModOrderList = savedModOrderList;

				var index = Profiles.IndexOf(Profiles.FirstOrDefault(p => p.ProfileName == "Public"));
				if (index > -1)
				{
					SelectedProfileIndex = index;
				}
				else
				{
					if (!String.IsNullOrWhiteSpace(selectedProfileUUID))
					{

						index = Profiles.IndexOf(Profiles.FirstOrDefault(p => p.UUID == selectedProfileUUID));
						if (index > -1)
						{
							SelectedProfileIndex = index;
						}
						else
						{
							SelectedProfileIndex = 0;
							DivinityApp.Log($"Profile '{selectedProfileUUID}' not found {Profiles.Count}/{loadedProfiles.Count}.");
						}
					}
					else
					{
						SelectedProfileIndex = 0;
					}
				}

				DivinityApp.Log($"Set profile to ({SelectedProfile?.Name})[{SelectedProfileIndex}]");

				MainProgressWorkText = "Building mod order list...";

				try
				{
					if (SelectedModOrder != null && lastActiveOrder != null && lastActiveOrder.Count > 0)
					{
						SelectedModOrder.SetOrder(lastActiveOrder);
					}

					BuildModOrderList(0, lastOrderName);
				}
				catch(Exception ex)
				{
					DivinityApp.Log($"Error building mod order:\n{ex}");
					TryLoadFallbackOrder();
				}
				MainProgressValue += taskStepAmount;

				if (!GameDirectoryFound)
				{
					ShowAlert("Game Data folder is not valid. Please set it in the preferences window and refresh", AlertType.Danger);
					Window.OpenPreferences(false, true);
				}
			}, RxApp.MainThreadScheduler);

			await IncreaseMainProgressValueAsync(taskStepAmount);
			await SetMainProgressTextAsync("Finishing up...");
		}
		else
		{
			DivinityApp.Log($"[*ERROR*] Larian local AppData folder not found!");
		}

		await Observable.Start(() =>
		{
			try
			{
				if (String.IsNullOrEmpty(lastAdventureMod))
				{
					var activeAdventureMod = SelectedModOrder?.Order.FirstOrDefault(x => GetModType(x.UUID) == "Adventure");
					if (activeAdventureMod != null)
					{
						lastAdventureMod = activeAdventureMod.UUID;
					}
				}

				int defaultAdventureIndex = AdventureMods.IndexOf(AdventureMods.FirstOrDefault(x => x.UUID == DivinityApp.MAIN_CAMPAIGN_UUID));
				if (defaultAdventureIndex == -1) defaultAdventureIndex = 0;
				if (lastAdventureMod != null && AdventureMods != null && AdventureMods.Count > 0)
				{
					DivinityApp.Log($"Setting selected adventure mod.");
					var nextAdventureMod = AdventureMods.FirstOrDefault(x => x.UUID == lastAdventureMod);
					if (nextAdventureMod != null)
					{
						SelectedAdventureModIndex = AdventureMods.IndexOf(nextAdventureMod);
					}
					else
					{

						SelectedAdventureModIndex = defaultAdventureIndex;
					}
				}
				else
				{
					SelectedAdventureModIndex = defaultAdventureIndex;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error setting active adventure mod:\n{ex}");
			}

			DivinityApp.Log($"Finalizing refresh operation.");

			OnMainProgressComplete();
			OnRefreshed?.Invoke(this, new EventArgs());

			IsRefreshing = false;
			IsLoadingOrder = false;
			IsInitialized = true;

			if (AppSettings.FeatureEnabled("ScriptExtender"))
			{
				LoadExtenderSettingsBackground();
			}

			//Always check for updates on the first run
			if (Settings.CheckForUpdates && _firstRun)
			{
				_firstRun = false;
				CheckForUpdates(false, true);
			}

			//RefreshAllModUpdatesBackground();

			return Unit.Default;
		}, RxApp.MainThreadScheduler);
		if (ActiveMods.Count == 0)
		{
			RxApp.TaskpoolScheduler.ScheduleAsync(CheckForEmptyOrderAsync);
		}
		return Unit.Default;
	}

	private string GetOrdersDirectory()
	{
		string loadOrderDirectory = Settings.LoadOrderPath;
		if (String.IsNullOrWhiteSpace(loadOrderDirectory))
		{
			loadOrderDirectory = DivinityApp.GetAppDirectory("Orders");
		}
		else if (!Path.IsPathRooted(loadOrderDirectory))
		{
			loadOrderDirectory = DivinityApp.GetAppDirectory(loadOrderDirectory);
		}
		else if (Uri.IsWellFormedUriString(loadOrderDirectory, UriKind.Relative))
		{
			loadOrderDirectory = Path.GetFullPath(loadOrderDirectory);
		}
		return loadOrderDirectory;
	}

	private async Task<List<DivinityLoadOrder>> LoadExternalLoadOrdersAsync()
	{
		try
		{
			var loadOrderDirectory = GetOrdersDirectory();

			DivinityApp.Log($"Attempting to load saved load orders from '{loadOrderDirectory}'.");
			return await DivinityModDataLoader.FindLoadOrderFilesInDirectoryAsync(loadOrderDirectory);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error loading external load orders: {ex}.");
			return new List<DivinityLoadOrder>();
		}
	}

	private void SaveLoadOrder(bool skipSaveConfirmation = false)
	{
		RxApp.MainThreadScheduler.ScheduleAsync(async (sch, cts) => await SaveLoadOrderAsync(skipSaveConfirmation));
	}

	private async Task<bool> SaveLoadOrderAsync(bool skipSaveConfirmation = false)
	{
		bool result = false;
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			UpdateOrderFromActiveMods();

			string outputDirectory = Settings.LoadOrderPath.ToRealPath();

			if (String.IsNullOrWhiteSpace(outputDirectory))
			{
				outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
			}

			if (!Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);

			string outputPath = SelectedModOrder.FilePath;
			string outputName = DivinityModDataLoader.MakeSafeFilename(Path.Join(SelectedModOrder.Name + ".json"), '_');

			if (String.IsNullOrWhiteSpace(SelectedModOrder.FilePath))
			{
				SelectedModOrder.FilePath = Path.Join(outputDirectory, outputName);
				outputPath = SelectedModOrder.FilePath;
			}

			try
			{
				if (SelectedModOrder.IsModSettings)
				{
					//When saving the "Current" order, write this to modsettings.lsx instead of a json file.
					result = await ExportLoadOrderAsync();
					outputPath = Path.Join(SelectedProfile.Folder, "modsettings.lsx");
					_modSettingsWatcher.PauseWatcher(true, 1000);
				}
				else
				{
					result = await DivinityModDataLoader.ExportLoadOrderToFileAsync(outputPath, SelectedModOrder);
				}
			}
			catch (Exception ex)
			{
				ShowAlert($"Failed to save mod load order to '{outputPath}': {ex.Message}", AlertType.Danger);
				result = false;
			}

			if (result && !skipSaveConfirmation)
			{
				ShowAlert($"Saved mod load order to '{outputPath}'", AlertType.Success, 10);
			}
		}

		return result;
	}

	private void SaveLoadOrderAs()
	{
		UpdateOrderFromActiveMods();
		var ordersDir = GetOrdersDirectory();
		if (!Directory.Exists(ordersDir)) Directory.CreateDirectory(ordersDir);

		var startDirectory = GetInitialStartingDirectory(ordersDir);

		var dialog = new SaveFileDialog
		{
			AddExtension = true,
			DefaultExt = ".json",
			Filter = "JSON file (*.json)|*.json",
			InitialDirectory = startDirectory
		};

		string outputPath = Path.Combine(SelectedModOrder.Name + ".json");
		if (SelectedModOrder.IsModSettings)
		{
			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-") + "_HH-mm-ss";
			outputPath = $"Current_{DateTime.Now.ToString(sysFormat)}.json";
		}

		outputPath = DivinityModDataLoader.MakeSafeFilename(outputPath, '_');
		var modOrderName = Path.GetFileNameWithoutExtension(outputPath);

		//dialog.RestoreDirectory = true;
		dialog.FileName = outputPath;
		dialog.CheckFileExists = false;
		dialog.CheckPathExists = false;
		dialog.OverwritePrompt = true;
		dialog.Title = "Save Load Order As...";

		if (dialog.ShowDialog(Window) == true)
		{
			outputPath = dialog.FileName;
			modOrderName = Path.GetFileNameWithoutExtension(outputPath);
			// Save mods that aren't missing
			var tempOrder = new DivinityLoadOrder { Name = modOrderName };
			tempOrder.Order.AddRange(SelectedModOrder.Order.Where(x => Mods.Any(y => y.UUID == x.UUID)));
			if (DivinityModDataLoader.ExportLoadOrderToFile(outputPath, tempOrder))
			{
				ShowAlert($"Saved mod load order to '{outputPath}'", AlertType.Success, 10);
				var updatedOrder = false;
				foreach (var order in ModOrderList)
				{
					if (order.FilePath == outputPath)
					{
						order.SetOrder(tempOrder);
						updatedOrder = true;
						DivinityApp.Log($"Updated saved order '{order.Name}' from '{modOrderName}'");
					}
				}
				if (!updatedOrder) AddNewModOrder(tempOrder);
				LoadModOrder(tempOrder);
			}
			else
			{
				ShowAlert($"Failed to save mod load order to '{outputPath}'", AlertType.Danger);
			}
		}
	}

	private void DisplayMissingMods(DivinityLoadOrder order = null)
	{
		var displayExtenderModWarning = false;
		var checkMissingMods = !Settings.DisableMissingModWarnings;

		order ??= SelectedModOrder;
		if (order != null && checkMissingMods)
		{
			var missingResults = new MissingModsResults();

			for (var i = 0; i < order.Order.Count; i++)
			{
				var entry = order.Order[i];
				if (TryGetMod(entry.UUID, out var mod) && mod.IsActive)
				{
					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (dependency == null) continue;

							if (!DivinityModDataLoader.IgnoreModDependency(dependency.UUID) && !TryGetMod(dependency.UUID, out _))
							{
								missingResults.AddDependency(dependency, [mod.Name]);
							}
						}
					}
				}
				else if (!DivinityModDataLoader.IgnoreMod(entry.UUID))
				{
					missingResults.AddMissing(entry, i);
				}
			}

			if (missingResults.TotalMissing > 0)
			{
				List<string> messages = [];
				
				var missingMessage = missingResults.GetMissingMessage();
				var missingDependencies = missingResults.GetDependenciesMessage();

				if (!String.IsNullOrWhiteSpace(missingMessage))
				{
					messages.Add(missingMessage);
				}

				if (!String.IsNullOrWhiteSpace(missingDependencies))
				{
					messages.Add($"Missing Dependencies:\n{missingDependencies}");
				}

				var finalMessage = string.Join(Environment.NewLine, messages);
				View.MainWindowMessageBox_OK.WindowBackground = MainViewControl.MessageBoxErrorBackgroundBrush;
				View.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
				View.MainWindowMessageBox_OK.ShowMessageBox(finalMessage,
					"Missing Mods in Load Order", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
			}
			else
			{
				displayExtenderModWarning = true;
			}
		}
		else
		{
			displayExtenderModWarning = true;
		}

		if (order != null && checkMissingMods && displayExtenderModWarning && AppSettings.FeatureEnabled("ScriptExtender"))
		{
			var missingResults = new MissingModsResults();

			//DivinityApp.LogMessage($"Mod Order: {String.Join("\n", order.Order.Select(x => x.Name))}");
			DivinityApp.Log("Checking mods for extender requirements.");
			for (int i = 0; i < order.Order.Count; i++)
			{
				var entry = order.Order[i];
				if (TryGetMod(entry.UUID, out var mod))
				{
					if (mod.ExtenderIcon == ScriptExtenderIconType.Missing)
					{
						DivinityApp.Log($"{mod.Name} | ExtenderModStatus: {mod.ExtenderModStatus}");
						missingResults.AddExtenderRequirement(mod);

						if (mod.Dependencies.Count > 0)
						{
							foreach (var dependency in mod.Dependencies.Items)
							{
								if (TryGetMod(dependency.UUID, out var dependencyMod))
								{
									// Dependencies not in the order that require the extender
									if (mod.ExtenderIcon == ScriptExtenderIconType.Missing)
									{
										DivinityApp.Log($"{mod.Name} | ExtenderModStatus: {mod.ExtenderModStatus}");
										missingResults.AddExtenderRequirement(dependencyMod, [mod.Name]);
									}
								}
							}
						}
					}
				}
			}

			if (missingResults.ExtenderRequired.Count > 0)
			{
				var finalMessage = "The following mods require the Script Extender. Functionality may be limited without it.\n";
				finalMessage += missingResults.GetExtenderRequiredMessage();

				View.MainWindowMessageBox_OK.WindowBackground = MainViewControl.MessageBoxErrorBackgroundBrush;
				View.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
				View.MainWindowMessageBox_OK.ShowMessageBox(finalMessage,
					"Mods Require the Script Extender - Install it with the Tools menu!", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
			}
		}
	}

	private DivinityProfileActiveModData ProfileActiveModDataFromUUID(string uuid)
	{
		if (TryGetMod(uuid, out var mod))
		{
			return mod.ToProfileModData();
		}
		return new DivinityProfileActiveModData()
		{
			UUID = uuid
		};
	}

	private void ExportLoadOrder()
	{
		RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
		{
			await ExportLoadOrderAsync();
			return Disposable.Empty;
		});
	}

	private async Task BackupCurrentLoadOrderAsync()
	{
		var backupOrderPath = Path.Combine(GetOrdersDirectory(), DivinityApp.PATH_LAST_EXPORTED_NAME + ".json");
		try
		{
			var backupOrder = new DivinityLoadOrder { Name = DivinityApp.PATH_LAST_EXPORTED_NAME, FilePath = backupOrderPath };
			backupOrder.SetOrder(SelectedModOrder);

			await DivinityModDataLoader.ExportLoadOrderToFileAsync(backupOrderPath, backupOrder);
			var updatedOrder = false;
			foreach (var order in ModOrderList)
			{
				if (order.FilePath == backupOrderPath)
				{
					order.SetOrder(backupOrder);
					updatedOrder = true;
				}
			}
			if (!updatedOrder) AddNewModOrder(backupOrder);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error saving backup load order '{backupOrderPath}':\n{ex}");
		}
	}

	private void DeleteModCrashSanityCheck()
	{
		if (Settings.DeleteModCrashSanityCheck && !string.IsNullOrWhiteSpace(PathwayData.AppDataGameFolder))
		{
			var modCrashSanityCheck = Path.Join(PathwayData.AppDataGameFolder, "ModCrashSanityCheck");
			try
			{
				if (Directory.Exists(modCrashSanityCheck))
				{
					Directory.Delete(modCrashSanityCheck);

					DivinityApp.Log($"Deleted '{modCrashSanityCheck.ReplaceSpecialPaths()}'");
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error deleting '{modCrashSanityCheck.ReplaceSpecialPaths()}':\n{ex}");
			}
		}
	}

	private async Task<bool> ExportLoadOrderAsync()
	{
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			UpdateOrderFromActiveMods();
			DeleteModCrashSanityCheck();

			var outputAdventureMod = SelectedAdventureMod;
			if (outputAdventureMod == null)
			{
				var gustavX = mods.Lookup(DivinityApp.GUSTAVX_UUID);
				if (gustavX.HasValue)
				{
					outputAdventureMod = gustavX.Value;
				}
				else
				{
					//Try and fallback to GustavDev
					var gustavDev = mods.Lookup(DivinityApp.GUSTAVDEV_UUID);
					if (gustavDev.HasValue)
					{
						outputAdventureMod = gustavDev.Value;
					}
					else
					{
						var gustavDevInherent = DivinityApp.IgnoredMods.Lookup(DivinityApp.GUSTAVDEV_UUID);
						if (gustavDevInherent.HasValue)
						{
							outputAdventureMod = gustavDevInherent.Value;
						}
					}
				}
			}
			string outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
			var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, mods.Items, Settings.AutoAddDependenciesWhenExporting, outputAdventureMod);
			var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, finalOrder);

			await BackupCurrentLoadOrderAsync();

			var dir = GetLarianStudiosAppDataFolder();
			if (SelectedModOrder.Order.Count > 0)
			{
				await DivinityModDataLoader.UpdateLauncherPreferencesAsync(dir, false, false, true);
			}
			else
			{
				if (Settings.DisableLauncherTelemetry || Settings.DisableLauncherModWarnings)
				{
					await DivinityModDataLoader.UpdateLauncherPreferencesAsync(dir, !Settings.DisableLauncherTelemetry, !Settings.DisableLauncherModWarnings);
				}
			}

			if (result)
			{
				await Observable.Start(() =>
				{
					ShowAlert($"Exported load order to '{outputPath}'", AlertType.Success, 15);

					if (DivinityModDataLoader.ExportedSelectedProfile(PathwayData.AppDataProfilesPath, SelectedProfile.UUID))
					{
						DivinityApp.Log($"Set active profile to '{SelectedProfile.Name}'");
					}
					else
					{
						DivinityApp.Log($"Could not set active profile to '{SelectedProfile.Name}'");
					}

					//Update "Current" order
					if (!SelectedModOrder.IsModSettings)
					{
						this.ModOrderList.First(x => x.IsModSettings)?.SetOrder(SelectedModOrder.Order);
					}

					List<string> orderList = [];
					if (SelectedAdventureMod != null) orderList.Add(SelectedAdventureMod.UUID);
					orderList.AddRange(SelectedModOrder.Order.Select(x => x.UUID));

					SelectedProfile.ActiveMods.Clear();
					SelectedProfile.ActiveMods.AddRange(orderList.Select(x => ProfileActiveModDataFromUUID(x)));
					DisplayMissingMods(SelectedModOrder);

					HasExported = true;

					return Unit.Default;
				}, RxApp.MainThreadScheduler);
				return true;
			}
			else
			{
				await Observable.Start(() =>
				{
					string msg = $"Problem exporting load order to '{outputPath}'. Is the file locked?";
					ShowAlert(msg, AlertType.Danger);
					View.MainWindowMessageBox_OK.WindowBackground = MainViewControl.MessageBoxErrorBackgroundBrush;
					View.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
					View.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
					return Unit.Default;
				}, RxApp.MainThreadScheduler);
			}
		}
		else
		{
			await Observable.Start(() =>
			{
				ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
				return Unit.Default;
			}, RxApp.MainThreadScheduler);
		}

		return false;
	}

	private void OnMainProgressComplete(double delay = 0)
	{
		DivinityApp.Log($"Main progress is complete.");

		MainProgressValue = 1d;
		MainProgressWorkText = "Finished.";

		if (MainProgressToken != null)
		{
			MainProgressToken.Dispose();
			MainProgressToken = null;
		}

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

	private static readonly ArchiveEncoding _archiveEncoding = new(Encoding.UTF8, Encoding.UTF8);
	private static readonly ReaderOptions _importReaderOptions = new() { ArchiveEncoding = _archiveEncoding };
	private static readonly WriterOptions _exportWriterOptions = new(CompressionType.Deflate) { ArchiveEncoding = _archiveEncoding };

	private void ImportOrderFromArchive()
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".zip",
			Filter = $"Archive file (*.7z,*.rar;*.zip)|{_archiveFormatsStr}|All files (*.*)|*.*",
			Title = "Import Order & Mods from Archive...",
			ValidateNames = true,
			ReadOnlyChecked = true,
			Multiselect = false,
			InitialDirectory = GetInitialStartingDirectory(Settings.LastImportDirectoryPath)
		};

		if (dialog.ShowDialog(Window) == true)
		{
			var savedDirectory = Path.GetDirectoryName(dialog.FileName);
			if (Settings.LastImportDirectoryPath != savedDirectory)
			{
				Settings.LastImportDirectoryPath = savedDirectory;
				PathwayData.LastSaveFilePath = savedDirectory;
				SaveSettings();
			}
			//if(!Path.GetExtension(dialog.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
			//{
			//	view.AlertBar.SetDangerAlert($"Currently only .zip format archives are supported.", -1);
			//	return;
			//}
			MainProgressTitle = $"Importing mods from '{dialog.FileName}'.";
			MainProgressWorkText = "";
			MainProgressValue = 0d;
			MainProgressIsActive = true;
			var result = new ImportOperationResults()
			{
				TotalFiles = 1
			};
			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				var builtinMods = DivinityApp.IgnoredMods.Items.SafeToDictionary(x => x.Folder, x => x);
				MainProgressToken = new CancellationTokenSource();
				await ImportArchiveAsync(builtinMods, result, dialog.FileName, false, MainProgressToken.Token);
				if (result.Mods.Count > 0 && result.Mods.Any(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START))
				{
					await UpdateHandler.Nexus.Update(result.Mods, MainProgressToken.Token);
				}
				await ctrl.Yield(t);
				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					OnMainProgressComplete();

					if (result.Errors.Count > 0)
					{
						var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
						var errorOutputPath = DivinityApp.GetAppDirectory("_Logs", $"ImportOrderFromArchive_{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}_Errors.log");
						var logsDir = Path.GetDirectoryName(errorOutputPath);
						if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);
						File.WriteAllText(errorOutputPath, String.Join("\n", result.Errors.Select(x => $"File: {x.File}\nError:\n{x.Exception}")));
					}

					var messages = new List<string>();
					var total = result.Orders.Count + result.Mods.Count;

					if (total > 0)
					{
						if (result.Orders.Count > 0)
						{
							messages.Add($"{result.Orders.Count} order(s)");

							foreach (var order in result.Orders)
							{
								if (order.Name == "Current")
								{
									if (SelectedModOrder?.IsModSettings == true)
									{
										SelectedModOrder.SetFrom(order);
										LoadModOrder(SelectedModOrder);
									}
									else
									{
										var currentOrder = ModOrderList.FirstOrDefault(x => x.IsModSettings);
										if (currentOrder != null)
										{
											SelectedModOrder.SetFrom(currentOrder);
										}
									}
								}
								else
								{
									AddNewModOrder(order);
								}
							}
						}
						if (result.Mods.Count > 0)
						{
							messages.Add($"{result.Mods.Count} mod(s)");

							var selectNext = result.Mods.Select(x => x.UUID).ToHashSet();
							RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(20), () =>
							{
								var selectMods = mods.Items.Where(x => selectNext.Contains(x.UUID));
								DeselectAllMods();
								Layout.DeselectAll();
								Layout.SelectMods(selectMods);
							});
						}
						var msg = String.Join(", ", messages);
						ShowAlert($"Imported {msg}", AlertType.Success, 20);
					}
					else
					{
						ShowAlert($"Successfully extracted archive, but no mods or load orders were found", AlertType.Warning, 20);
					}
				});
				return Disposable.Empty;
			});
		}
	}

	private void AddImportedMod(DivinityModData mod, bool? toActiveList = null)
	{
		mod.NexusModsEnabled = DivinityApp.NexusModsEnabled;

		if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
		{
			mods.Remove(mod.UUID);
			mods.AddOrUpdate(mod);
			mod.IsSelected = true;
			DivinityApp.Log($"Imported Override Mod: {mod}");
			return;
		}
		var existingMod = mods.Items.FirstOrDefault(x => x.UUID == mod.UUID);
		if (existingMod != null)
		{
			if (toActiveList == null) toActiveList = existingMod.IsActive;

			if (existingMod.IsActive == toActiveList)
			{
				mod.Index = existingMod.Index;
				mod.IsActive = existingMod.IsActive;
				if (existingMod.IsActive)
				{
					ActiveMods.Replace(existingMod, mod);
				}
				else
				{
					InactiveMods.Replace(existingMod, mod);
				}
				foreach (var order in ModOrderList)
				{
					order.Update(mod);
				}
			}
			else
			{
				if (existingMod.IsActive)
				{
					ActiveMods.Remove(existingMod);
				}
				else
				{
					InactiveMods.Remove(existingMod);
				}
				if (toActiveList == true)
				{
					AddActiveMod(mod);
				}
				else
				{
					RemoveActiveMod(mod);
				}
			}
		}
		else
		{
			if (toActiveList == true)
			{
				AddActiveMod(mod);
			}
			else
			{
				RemoveActiveMod(mod);
			}
		}
		mods.AddOrUpdate(mod);
		UpdateModExtenderStatus(mod);
		DivinityApp.Log($"Imported Mod: {mod}");
	}

	private async Task<bool> ImportCompressedFileAsync(Dictionary<string, DivinityModData> builtinMods, ImportOperationResults taskResult, string filePath, string extension, bool onlyMods, CancellationToken cts, bool? toActiveList = null)
	{
		FileStream fileStream = null;
		string outputDirectory = PathwayData.AppDataModsPath;
		double taskStepAmount = 1.0 / 4;
		bool success = false;
		var jsonFiles = new Dictionary<string, string>();
		try
		{
			fileStream = File.Open(filePath, new FileStreamOptions
			{
				Options = FileOptions.Asynchronous,
				Mode = FileMode.Open,
				Access = FileAccess.Read,
				Share = FileShare.Read,
				BufferSize = 4096
			});

			if (fileStream != null)
			{
				var info = NexusModFileVersionData.FromFilePath(filePath);

				await fileStream.ReadAsync(new byte[fileStream.Length], 0, (int)fileStream.Length);
				fileStream.Position = 0;
				IncreaseMainProgressValue(taskStepAmount);
				System.IO.Stream decompressionStream = null;
				TempFile tempFile = null;

				try
				{
					switch (extension)
					{
						case ".bz2":
							decompressionStream = new BZip2Stream(fileStream, SharpCompress.Compressors.CompressionMode.Decompress, true);
							break;
						case ".xz":
							decompressionStream = new XZStream(fileStream);
							break;
						case ".zst":
							decompressionStream = new DecompressionStream(fileStream);
							break;
					}
					if (decompressionStream != null)
					{
						DivinityApp.Log($"Checking if compressed file ({filePath} => {extension}) is a pak.");
						var outputName = Path.GetFileNameWithoutExtension(filePath);
						if (!outputName.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) outputName += ".pak";
						var outputFilePath = Path.Combine(outputDirectory, outputName);

						tempFile = await TempFile.CreateAsync(filePath, decompressionStream, cts);

						try
						{
							var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, builtinMods, cts, tempFile.Stream);
							if (mod != null)
							{
								try
								{
									mod.LastModified = File.GetLastWriteTime(filePath);
									mod.LastUpdated = mod.LastModified;
								}
								catch (Exception ex)
								{
									DivinityApp.Log($"Error getting pak last modified date for '{ex}': {ex}");
								}

								if (!outputName.Contains(mod.Name))
								{
									var nameFromMeta = $"{mod.Folder}.pak";
									outputFilePath = Path.Combine(outputDirectory, nameFromMeta);
									mod.FilePath = outputFilePath;
								}
								using (var fs = File.Create(outputFilePath, 4096, System.IO.FileOptions.Asynchronous))
								{
									try
									{
										await decompressionStream.CopyToAsync(fs, 4096, cts);
										success = true;
									}
									catch (Exception ex)
									{
										taskResult.AddError(outputFilePath, ex);
										DivinityApp.Log($"Error copying file '{outputName}' from archive to '{outputFilePath}':\n{ex}");
									}
								}

								if (success)
								{
									taskResult.TotalPaks++;
									taskResult.Mods.Add(mod);
									mod.NexusModsData.SetModVersion(info);
									await Observable.Start(() =>
									{
										AddImportedMod(mod, toActiveList);
										return Unit.Default;
									}, RxApp.MainThreadScheduler);
								}
							}
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error reading decompressed file '{filePath}' as pak:\n{ex}");
						}
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error reading file '{filePath}':\n{ex}");
				}
				finally
				{
					decompressionStream?.Dispose();
					tempFile?.Dispose();
				}

				if (info.Success && success)
				{
					//Still save cache from imported zips, even if we aren't updating
					await UpdateHandler.Nexus.SaveCacheAsync(false, Version.ToString(), MainProgressToken.Token);
				}

				IncreaseMainProgressValue(taskStepAmount);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error extracting package: {ex}");
			RxApp.MainThreadScheduler.Schedule(_ =>
			{
				taskResult.AddError(filePath, ex);
				ShowAlert($"Error extracting archive (check the log): {ex.Message}", AlertType.Danger, 0);
			});
		}
		finally
		{
			RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Cleaning up...");
			fileStream?.Close();
			IncreaseMainProgressValue(taskStepAmount);

			if (!onlyMods && jsonFiles.Count > 0)
			{
				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					foreach (var kvp in jsonFiles)
					{
						DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(kvp.Value);
						if (order != null)
						{
							taskResult.Orders.Add(order);
							order.Name = kvp.Key;
							DivinityApp.Log($"Imported mod order from archive: {String.Join(@"\n\t", order.Order.Select(x => x.Name))}");
							AddNewModOrder(order);
						}
					}
				});
			}
			IncreaseMainProgressValue(taskStepAmount);
		}
		return success;
	}

	private async Task<bool> ImportArchiveAsync(Dictionary<string, DivinityModData> builtinMods, ImportOperationResults taskResult, string archivePath, bool onlyMods, CancellationToken cts, bool? toActiveList = null)
	{
		FileStream fileStream = null;
		string outputDirectory = PathwayData.AppDataModsPath;
		double taskStepAmount = 1.0 / 4;
		bool success = false;
		var jsonFiles = new Dictionary<string, string>();
		try
		{
			fileStream = File.Open(archivePath, new FileStreamOptions
			{
				Options = FileOptions.Asynchronous,
				Mode = FileMode.Open,
				Access = FileAccess.Read,
				Share = FileShare.Read,
				BufferSize = 4096
			});
			if (fileStream != null)
			{
				var info = NexusModFileVersionData.FromFilePath(archivePath);

				fileStream.Position = 0;
				IncreaseMainProgressValue(taskStepAmount);
				using (var archive = ArchiveFactory.Open(fileStream, _importReaderOptions))
				{
					foreach (var file in archive.Entries)
					{
						if (cts.IsCancellationRequested) return false;
						if (!file.IsDirectory)
						{
							if (file.Key.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
							{
								var outputName = Path.GetFileName(file.Key);
								var outputFilePath = Path.Combine(outputDirectory, outputName);
								taskResult.TotalPaks++;
								using (var entryStream = file.OpenEntryStream())
								{
									using var fs = File.Create(outputFilePath, 4096, System.IO.FileOptions.Asynchronous);
									try
									{
										await entryStream.CopyToAsync(fs, 4096, cts);
										success = true;
									}
									catch (Exception ex)
									{
										taskResult.AddError(outputFilePath, ex);
										DivinityApp.Log($"Error copying file '{file.Key}' from archive to '{outputFilePath}':\n{ex}");
									}
								}

								if (success)
								{
									var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, builtinMods, cts);
									if (mod != null)
									{
										taskResult.Mods.Add(mod);
										mod.NexusModsData.SetModVersion(info);
										await Observable.Start(() =>
										{
											AddImportedMod(mod, toActiveList);
											return Unit.Default;
										}, RxApp.MainThreadScheduler);
									}
								}
							}
							else if (file.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
							{
								using var entryStream = file.OpenEntryStream();
								try
								{
									int length = (int)file.Size;
									var result = new byte[length];
									await entryStream.ReadAsync(result, 0, length);
									string text = Encoding.UTF8.GetString(result);
									if (!String.IsNullOrWhiteSpace(text))
									{
										jsonFiles.Add(Path.GetFileNameWithoutExtension(file.Key), text);
									}
								}
								catch (Exception ex)
								{
									taskResult.AddError(file.Key, ex);
									DivinityApp.Log($"Error reading json file '{file.Key}' from archive:\n{ex}");
								}
							}
						}
					}
				}

				if (info.Success && success)
				{
					//Still save cache from imported zips, even if we aren't updating
					await UpdateHandler.Nexus.SaveCacheAsync(false, Version.ToString(), MainProgressToken.Token);
				}

				IncreaseMainProgressValue(taskStepAmount);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error extracting package: {ex}");
			RxApp.MainThreadScheduler.Schedule(_ =>
			{
				taskResult.AddError(archivePath, ex);
				ShowAlert($"Error extracting archive (check the log): {ex.Message}", AlertType.Danger, 0);
			});
		}
		finally
		{
			RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Cleaning up...");
			fileStream?.Close();
			IncreaseMainProgressValue(taskStepAmount);

			if (!onlyMods && jsonFiles.Count > 0)
			{
				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					foreach (var kvp in jsonFiles)
					{
						DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(kvp.Value);
						if (order != null)
						{
							taskResult.Orders.Add(order);
							order.Name = kvp.Key;
							DivinityApp.Log($"Imported mod order from archive: {String.Join(@"\n\t", order.Order.Select(x => x.Name))}");
						}
					}
				});
			}
			IncreaseMainProgressValue(taskStepAmount);
		}
		return success;
	}

	private void ExportLoadOrderToArchive_Start()
	{
		//view.MainWindowMessageBox.Text = "Add active mods to a zip file?";
		//view.MainWindowMessageBox.Caption = "Depending on the number of mods, this may take some time.";
		MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, $"Save active mods to a zip file?{Environment.NewLine}Depending on the number of mods, this may take some time.", "Confirm Archive Creation",
			MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel, Window.MessageBoxStyle);
		if (result == MessageBoxResult.OK)
		{
			MainProgressTitle = "Adding active mods to zip...";
			MainProgressWorkText = "";
			MainProgressValue = 0d;
			MainProgressIsActive = true;
			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				MainProgressToken = new CancellationTokenSource();
				await ExportLoadOrderToArchiveAsync("", MainProgressToken.Token);
				await ctrl.Yield();
				RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());
				return Disposable.Empty;
			});
		}
	}

	private async Task<bool> ExportLoadOrderToArchiveAsync(string outputPath, CancellationToken t)
	{
		var success = false;
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
			var gameDataFolder = Path.GetFullPath(Settings.GameDataPath);
			var appDir = DivinityApp.GetAppDirectory();
			var tempDir = Path.Combine(appDir, "_Temp_" + DateTime.Now.ToString(sysFormat + "_HH-mm-ss"));
			Directory.CreateDirectory(tempDir);

			if (String.IsNullOrEmpty(outputPath))
			{
				var baseOrderName = SelectedModOrder.Name;
				if (SelectedModOrder.IsModSettings)
				{
					baseOrderName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}";
				}
				var outputDir = Path.Combine(appDir, "Export");
				outputPath = Path.Combine(outputDir, $"{baseOrderName}-{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.zip");
				if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
			}

			var modPaks = new List<DivinityModData>(Mods.Where(x => SelectedModOrder.Order.Any(o => o.UUID == x.UUID)));
			modPaks.AddRange(ForceLoadedMods.Where(x => !x.IsForceLoadedMergedMod));

			var incrementProgress = 1d / modPaks.Count;

			try
			{
				using (var stream = File.OpenWrite(outputPath))
				using (var zipWriter = WriterFactory.Open(stream, ArchiveType.Zip, _exportWriterOptions))
				{
					var orderFileName = DivinityModDataLoader.MakeSafeFilename(Path.Combine(SelectedModOrder.Name + ".json"), '_');
					var contents = JsonConvert.SerializeObject(SelectedModOrder, Newtonsoft.Json.Formatting.Indented);
					using (var ms = new System.IO.MemoryStream())
					{
						using var swriter = new System.IO.StreamWriter(ms);
						await swriter.WriteAsync(contents);
						swriter.Flush();
						ms.Position = 0;
						zipWriter.Write(orderFileName, ms);
					}

					foreach (var mod in modPaks)
					{
						if (t.IsCancellationRequested) return false;
						if (!mod.IsEditorMod)
						{
							var fileName = Path.GetFileName(mod.FilePath);
							await WriteZipAsync(zipWriter, fileName, mod.FilePath, t);
						}
						else
						{
							var outputPackage = Path.ChangeExtension(Path.Combine(tempDir, mod.Folder), "pak");
							//Imported Classic Projects
							if (!mod.Folder.Contains(mod.UUID))
							{
								outputPackage = Path.ChangeExtension(Path.Combine(tempDir, mod.Folder + "_" + mod.UUID), "pak");
							}

							var sourceFolders = new List<string>();

							var modsFolder = Path.Combine(gameDataFolder, $"Mods/{mod.Folder}");
							var publicFolder = Path.Combine(gameDataFolder, $"Public/{mod.Folder}");

							if (Directory.Exists(modsFolder)) sourceFolders.Add(modsFolder);
							if (Directory.Exists(publicFolder)) sourceFolders.Add(publicFolder);

							DivinityApp.Log($"Creating package for editor mod '{mod.Name}' - '{outputPackage}'.");

							if (await DivinityFileUtils.CreatePackageAsync(gameDataFolder, sourceFolders, outputPackage, t, DivinityFileUtils.IgnoredPackageFiles))
							{
								var fileName = Path.GetFileName(outputPackage);
								await WriteZipAsync(zipWriter, fileName, outputPackage, t);
								File.Delete(outputPackage);
							}
						}

						RxApp.MainThreadScheduler.Schedule(_ => MainProgressValue += incrementProgress);
					}
				}

				RxApp.MainThreadScheduler.Schedule(() =>
				{
					ShowAlert($"Exported load order to '{outputPath}'", AlertType.Success, 15);
					ProcessHelper.TryOpenPath(Path.GetDirectoryName(outputPath));
				});

				success = true;
			}
			catch (Exception ex)
			{
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					string msg = $"Error writing load order archive '{outputPath}': {ex}";
					DivinityApp.Log(msg);
					ShowAlert(msg, AlertType.Danger);
				});
			}

			Directory.Delete(tempDir);
		}
		else
		{
			RxApp.MainThreadScheduler.Schedule(() =>
			{
				ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
			});
		}

		return success;
	}

	private static Task WriteZipAsync(IWriter writer, string entryName, string source, CancellationToken token)
	{
		if (token.IsCancellationRequested)
		{
			return Task.FromCanceled(token);
		}

		var task = Task.Run(async () =>
		{
			// execute actual operation in child task
			var childTask = Task.Factory.StartNew(() =>
			{
				try
				{
					writer.Write(entryName, source);
				}
				catch (Exception)
				{
					// ignored because an exception on a cancellation request 
					// cannot be avoided if the stream gets disposed afterwards 
				}
			}, TaskCreationOptions.AttachedToParent);

			var awaiter = childTask.GetAwaiter();
			while (!awaiter.IsCompleted)
			{
				await Task.Delay(0, token);
			}
		}, token);

		return task;
	}

	private void ExportLoadOrderToArchiveAs()
	{
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			UpdateOrderFromActiveMods();

			var dialog = new SaveFileDialog
			{
				AddExtension = true,
				DefaultExt = ".zip",
				Filter = "Archive file (*.zip)|*.zip",
				InitialDirectory = GetInitialStartingDirectory()
			};

			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
			var baseOrderName = SelectedModOrder.Name;
			if (SelectedModOrder.IsModSettings)
			{
				baseOrderName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}";
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
				MainProgressTitle = "Adding active mods to zip...";
				MainProgressWorkText = "";
				MainProgressValue = 0d;
				MainProgressIsActive = true;

				RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
				{
					MainProgressToken = new CancellationTokenSource();
					await ExportLoadOrderToArchiveAsync(dialog.FileName, MainProgressToken.Token);
					await ctrl.Yield();
					RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());
					return Disposable.Empty;
				});
			}
		}
		else
		{
			ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
		}

	}

	private string ModToTSVLine(DivinityModData mod)
	{
		var index = mod.Index.ToString();
		if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
		{
			index = "Override";
		}
		var urls = String.Join(";", mod.GetAllURLs());
		return $"{index}\t{mod.Name}\t{mod.Author}\t{mod.OutputPakName}\t{String.Join(", ", mod.Tags)}\t{String.Join(", ", mod.Dependencies.Items.Select(y => y.Name))}\t{urls}";
	}

	private string ModToTextLine(DivinityModData mod)
	{
		var index = mod.Index.ToString() + ".";
		if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
		{
			index = "Override";
		}
		var urls = String.Join(";", mod.GetAllURLs());
		return $"{index} {mod.Name} ({mod.OutputPakName}) {urls}";
	}

	private void ExportLoadOrderToTextFileAs()
	{
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			UpdateOrderFromActiveMods();

			var dialog = new SaveFileDialog
			{
				AddExtension = true,
				DefaultExt = ".tsv",
				Filter = "Spreadsheet file (*.tsv)|*.tsv|Plain text file (*.txt)|*.txt|JSON file (*.json)|*.json",
				InitialDirectory = GetInitialStartingDirectory()
			};

			string sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
			string baseOrderName = SelectedModOrder.Name;
			if (SelectedModOrder.IsModSettings)
			{
				baseOrderName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}";
			}
			string outputName = $"{baseOrderName}-{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.tsv";

			//dialog.RestoreDirectory = true;
			dialog.FileName = DivinityModDataLoader.MakeSafeFilename(outputName, '_');
			dialog.CheckFileExists = false;
			dialog.CheckPathExists = false;
			dialog.OverwritePrompt = true;
			dialog.Title = "Export Load Order As Text File...";

			if (dialog.ShowDialog(Window) == true)
			{
				var exportMods = new List<DivinityModData>(ActiveMods);
				exportMods.AddRange(ForceLoadedMods.ToList().OrderBy(x => x.Name));

				var fileType = Path.GetExtension(dialog.FileName);
				string outputText = "";
				if (fileType.Equals(".json", StringComparison.OrdinalIgnoreCase))
				{
					outputText = JsonConvert.SerializeObject(exportMods.Select(x => DivinitySerializedModData.FromMod(x)).ToList(), Formatting.Indented, new JsonSerializerSettings
					{
						NullValueHandling = NullValueHandling.Ignore
					});
				}
				else if (fileType.Equals(".tsv", StringComparison.OrdinalIgnoreCase))
				{
					outputText = "Index\tName\tAuthor\tFileName\tTags\tDependencies\tURL\n";
					outputText += String.Join("\n", exportMods.Select(ModToTSVLine));
				}
				else
				{
					//Text file format
					outputText = String.Join("\n", exportMods.Select(ModToTextLine));
				}
				try
				{
					File.WriteAllText(dialog.FileName, outputText);
					ShowAlert($"Exported order to '{dialog.FileName}'", AlertType.Success, 20);
				}
				catch (Exception ex)
				{
					ShowAlert($"Error exporting mod order to '{dialog.FileName}':\n{ex}", AlertType.Danger);
				}
			}
		}
		else
		{
			DivinityApp.Log($"SelectedProfile({SelectedProfile}) SelectedModOrder({SelectedModOrder})");
			ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
		}
	}

	private DivinityLoadOrder ImportOrderFromSave()
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".lsv",
			Filter = "Larian Save file (*.lsv)|*.lsv",
			Title = "Load Mod Order From Save..."
		};

		var startPath = "";
		if (SelectedProfile != null)
		{
			string profilePath = Path.GetFullPath(Path.Combine(SelectedProfile.Folder, "Savegames"));
			string storyPath = Path.Combine(profilePath, "Story");
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
			PathwayData.LastSaveFilePath = Path.GetDirectoryName(dialog.FileName);
			DivinityApp.Log($"Loading order from '{dialog.FileName}'.");
			var newOrder = DivinityModDataLoader.GetLoadOrderFromSave(dialog.FileName, GetOrdersDirectory());
			if (newOrder != null)
			{
				DivinityApp.Log($"Imported mod order: {String.Join(Environment.NewLine + "\t", newOrder.Order.Select(x => x.Name))}");
				return newOrder;
			}
			else
			{
				DivinityApp.Log($"Failed to load order from '{dialog.FileName}'.");
				ShowAlert($"No mod order found in save \"{Path.GetFileNameWithoutExtension(dialog.FileName)}\"", AlertType.Danger, 30);
			}
		}
		return null;
	}

	private void ImportOrderFromSaveAsNew()
	{
		var order = ImportOrderFromSave();
		if (order != null)
		{
			AddNewModOrder(order);
		}
	}

	private void ImportOrderFromSaveToCurrent()
	{
		var order = ImportOrderFromSave();
		if (order != null)
		{
			if (SelectedModOrder != null)
			{
				SelectedModOrder.SetOrder(order);
				if (LoadModOrder(SelectedModOrder))
				{
					DivinityApp.Log($"Successfully re-loaded order {SelectedModOrder.Name} with save order.");
				}
				else
				{
					DivinityApp.Log($"Failed to load order {SelectedModOrder.Name}.");
				}
			}
			else
			{
				AddNewModOrder(order);
				LoadModOrder(order);
			}
		}
	}

	private void ImportOrderFromFile()
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".json",
			Filter = "All formats (*.json;*.txt;*.tsv)|*.json;*.txt;*.tsv|JSON file (*.json)|*.json|Text file (*.txt)|*.txt|TSV file (*.tsv)|*.tsv",
			Title = "Load Mod Order From File...",
			InitialDirectory = GetInitialStartingDirectory(Settings.LastLoadedOrderFilePath)
		};

		if (dialog.ShowDialog(Window) == true)
		{
			Settings.LastLoadedOrderFilePath = Path.GetDirectoryName(dialog.FileName);
			SaveSettings();
			DivinityApp.Log($"Loading order from '{dialog.FileName}'.");
			var newOrder = DivinityModDataLoader.LoadOrderFromFile(dialog.FileName, mods.Items);
			if (newOrder != null)
			{
				DivinityApp.Log($"Imported mod order:\n{String.Join(Environment.NewLine + "\t", newOrder.Order.Select(x => x.Name))}");
				if (newOrder.IsDecipheredOrder)
				{
					if (SelectedModOrder != null)
					{
						SelectedModOrder.SetOrder(newOrder);
						if (LoadModOrder(SelectedModOrder))
						{
							ShowAlert($"Successfully overwrote order '{SelectedModOrder.Name}' with with imported order", AlertType.Success, 20);
						}
						else
						{
							ShowAlert($"Failed to reset order to '{dialog.FileName}'", AlertType.Danger, 60);
						}
					}
					else
					{
						AddNewModOrder(newOrder);
						LoadModOrder(newOrder);
						ShowAlert($"Successfully imported order '{newOrder.Name}'", AlertType.Success, 20);
					}
				}
				else
				{
					AddNewModOrder(newOrder);
					LoadModOrder(newOrder);
					ShowAlert($"Successfully imported order '{newOrder.Name}'", AlertType.Success, 20);
				}
			}
			else
			{
				ShowAlert($"Failed to import order from '{dialog.FileName}'", AlertType.Danger, 60);
			}
		}
	}

	private void RenameSave_Start()
	{
		string profileSavesDirectory = "";
		if (SelectedProfile != null)
		{
			profileSavesDirectory = Path.GetFullPath(Path.Combine(SelectedProfile.Folder, "Savegames"));
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
		if (SelectedProfile != null)
		{
			string profilePath = Path.GetFullPath(Path.Combine(SelectedProfile.Folder, "Savegames"));
			string storyPath = Path.Combine(profilePath, "Story");
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
						string previewImage = Path.Combine(rootFolder, rootFileName + ".WebP");
						string renamedImage = Path.Combine(rootFolder, Path.GetFileNameWithoutExtension(renameDialog.FileName) + ".WebP");
						if (File.Exists(previewImage))
						{
							File.Move(previewImage, renamedImage);
							DivinityApp.Log($"Renamed save screenshot '{previewImage}' to '{renamedImage}'.");
						}

						string originalDirectory = Path.GetDirectoryName(dialog.FileName);
						string desiredDirectory = Path.GetDirectoryName(renameDialog.FileName);

						if (!String.IsNullOrEmpty(profileSavesDirectory) && DivinityFileUtils.IsSubdirectoryOf(profileSavesDirectory, desiredDirectory))
						{
							if (originalDirectory == desiredDirectory)
							{
								var dirInfo = new DirectoryInfo(originalDirectory);
								if (dirInfo.Name.Equals(Path.GetFileNameWithoutExtension(dialog.FileName)))
								{
									desiredDirectory = Path.Combine(dirInfo.Parent.FullName, Path.GetFileNameWithoutExtension(renameDialog.FileName));
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

	public void CheckForUpdates(bool force = false, bool skipTimeCheck = false)
	{
		var updateVM = Services.Get<AppUpdateWindowViewModel>();
		if (updateVM != null)
		{
			if (!force)
			{
				if (skipTimeCheck || Settings.LastUpdateCheck == -1 || (DateTimeOffset.Now.ToUnixTimeSeconds() - Settings.LastUpdateCheck >= 43200))
				{
					updateVM.ScheduleUpdateCheck();
				}
			}
			else
			{
				updateVM.ScheduleUpdateCheck(true);
			}
			Settings.LastUpdateCheck = DateTimeOffset.Now.ToUnixTimeSeconds();
		}
	}

	public void OnViewActivated(MainWindow window, MainViewControl parentView)
	{
		Window = window;
		View = parentView;
		DivinityApp.Commands.SetViewModel(this);

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

		var loaded = LoadSettings();
		Keys.LoadKeybindings(this);
		SaveSettings();

		if (loaded && Settings.SaveWindowLocation)
		{
			Window.ApplyWindowPosition(Settings.Window);
		}

		Settings.Loaded = loaded;

		ModUpdatesViewVisible = ModUpdatesAvailable = false;
		MainProgressTitle = "Loading...";
		MainProgressValue = 0d;
		CanCancelProgress = false;
		MainProgressIsActive = true;
		Window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
		Window.TaskbarItemInfo.ProgressValue = 0;
		IsRefreshing = true;
		RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, token) =>
		{
			await RefreshAsync(sch, token);
			if(ProcessHelper.IsCurrentProcessAdmin())
			{
				if(!Settings.Confirmations.DisableAdminModeWarning)
				{
					RxApp.MainThreadScheduler.Schedule(() =>
					{
						var result = Xceed.Wpf.Toolkit.MessageBox.Show(Window,
						"BG3MM is currently running as an administrator, which can lead to issues.\nPlease restart BG3MM in non-admin mode.\nClick Cancel to disable this warning in the future.",
						"Process Elevation Warning",
						MessageBoxButton.OKCancel, MessageBoxImage.Warning, MessageBoxResult.OK, Window.MessageBoxStyle);
						if(result == MessageBoxResult.Cancel)
						{
							Settings.Confirmations.DisableAdminModeWarning = true;
							SaveSettings();
						}
					});
				}
			}
		});
	}

	public bool AutoChangedOrder { get; set; }
	public ViewModelActivator Activator { get; }

	private readonly Regex filterPropertyPattern = new("@([^\\s]+?)([\\s]+)([^@\\s]*)");
	private readonly Regex filterPropertyPatternWithQuotes = new("@([^\\s]+?)([\\s\"]+)([^@\"]*)");

	[Reactive] public int TotalActiveModsHidden { get; set; }
	[Reactive] public int TotalInactiveModsHidden { get; set; }

	private string HiddenToLabel(int totalHidden, int totalCount)
	{
		if (totalHidden > 0)
		{
			return $"{totalCount - totalHidden} Matched, {totalHidden} Hidden";
		}
		else
		{
			return $"0 Matched";
		}
	}

	private string SelectedToLabel(int total, int totalHidden)
	{
		if (totalHidden > 0)
		{
			return $", {total} Selected";
		}
		return $"{total} Selected";
	}

	public void OnFilterTextChanged(string searchText, IEnumerable<DivinityModData> modDataList)
	{
		int totalHidden = 0;
		//DivinityApp.LogMessage("Filtering mod list with search term " + searchText);
		if (String.IsNullOrWhiteSpace(searchText))
		{
			foreach (var m in modDataList)
			{
				m.Visibility = Visibility.Visible;
			}
		}
		else
		{
			if (searchText.IndexOf("@") > -1)
			{
				string remainingSearch = searchText;
				List<DivinityModFilterData> searchProps = new List<DivinityModFilterData>();

				MatchCollection matches;

				if (searchText.IndexOf("\"") > -1)
				{
					matches = filterPropertyPatternWithQuotes.Matches(searchText);
				}
				else
				{
					matches = filterPropertyPattern.Matches(searchText);
				}

				if (matches.Count > 0)
				{
					foreach (Match match in matches)
					{
						if (match.Success)
						{
							var prop = match.Groups[1]?.Value;
							var value = match.Groups[3]?.Value;
							if (String.IsNullOrEmpty(value)) value = "";
							if (!String.IsNullOrWhiteSpace(prop))
							{
								searchProps.Add(new DivinityModFilterData()
								{
									FilterProperty = prop,
									FilterValue = value
								});

								remainingSearch = remainingSearch.Replace(match.Value, "");
							}
						}
					}
				}

				remainingSearch = remainingSearch.Replace("\"", "");

				//If no Name property is specified, use the remaining unmatched text for that
				if (!String.IsNullOrWhiteSpace(remainingSearch) && !searchProps.Any(f => f.PropertyContains("Name")))
				{
					remainingSearch = remainingSearch.Trim();
					searchProps.Add(new DivinityModFilterData()
					{
						FilterProperty = "Name",
						FilterValue = remainingSearch
					});
				}

				foreach (var mod in modDataList)
				{
					//@Mode GM @Author Leader
					int totalMatches = 0;
					foreach (var f in searchProps)
					{
						if (f.Match(mod))
						{
							totalMatches += 1;
						}
					}
					if (totalMatches >= searchProps.Count)
					{
						mod.Visibility = Visibility.Visible;
					}
					else
					{
						mod.Visibility = Visibility.Collapsed;
						mod.IsSelected = false;
						totalHidden += 1;
					}
				}
			}
			else
			{
				foreach (var m in modDataList)
				{
					if (CultureInfo.CurrentCulture.CompareInfo.IndexOf(m.Name, searchText, CompareOptions.IgnoreCase) >= 0)
					{
						m.Visibility = Visibility.Visible;
					}
					else
					{
						m.Visibility = Visibility.Collapsed;
						m.IsSelected = false;
						totalHidden += 1;
					}
				}
			}
		}

		if (modDataList == ActiveMods)
		{
			TotalActiveModsHidden = totalHidden;
		}
		else if (modDataList == InactiveMods)
		{
			TotalInactiveModsHidden = totalHidden;
		}
	}

	private readonly MainWindowExceptionHandler exceptionHandler;

	public void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0)
	{
		message = message.ReplaceSpecialPaths();
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

	private void DeleteOrder(DivinityLoadOrder order)
	{
		MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, $"Delete load order '{order.Name}'? This cannot be undone.", "Confirm Order Deletion",
			MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, Window.MessageBoxStyle);
		if (result == MessageBoxResult.Yes)
		{
			SelectedModOrderIndex = 0;
			ModOrderList.Remove(order);
			if (!String.IsNullOrEmpty(order.FilePath) && File.Exists(order.FilePath))
			{
				RecycleBinHelper.DeleteFile(order.FilePath, false, false);
				ShowAlert($"Sent load order '{order.FilePath}' to the recycle bin", AlertType.Warning, 25);
			}
		}
	}

	private void DeleteMods(List<DivinityModData> targetMods, bool isDeletingDuplicates = false, List<DivinityModData> loadedMods = null)
	{
		if (!IsDeletingFiles)
		{
			var targetUUIDs = targetMods.Select(x => x.UUID).ToHashSet();

			var deleteFilesData = targetMods.Select(x => ModFileDeletionData.FromMod(x, isDeletingDuplicates, loadedMods));
			this.View.DeleteFilesView.ViewModel.IsDeletingDuplicates = isDeletingDuplicates;
			this.View.DeleteFilesView.ViewModel.Files.AddRange(deleteFilesData);

			this.View.DeleteFilesView.ViewModel.IsVisible = true;
		}
	}

	public void DeleteMod(DivinityModData mod)
	{
		DeleteMods([mod]);
	}

	public void RemoveDeletedMods(HashSet<string> deletedMods, bool removeFromLoadOrder = true)
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			mods.RemoveKeys(deletedMods);

			if (removeFromLoadOrder)
			{
				SelectedModOrder.Order.RemoveAll(x => deletedMods.Contains(x.UUID));
				SelectedProfile.ActiveMods.RemoveAll(x => deletedMods.Contains(x.UUID));
				//SaveLoadOrder(true);
			}

			InactiveMods.RemoveMany(InactiveMods.Where(x => deletedMods.Contains(x.UUID)));
			ActiveMods.RemoveMany(ActiveMods.Where(x => deletedMods.Contains(x.UUID)));
			ForceLoadedMods.RemoveMany(ForceLoadedMods.Where(x => deletedMods.Contains(x.UUID)));
		});
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

			int totalWork = SelectedPakMods.Count;
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
				foreach (var path in SelectedPakMods.Select(x => x.FilePath))
				{
					if (MainProgressToken.IsCancellationRequested) break;
					try
					{
						//Put each pak into its own folder
						string pakName = Path.GetFileNameWithoutExtension(path);
						RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Extracting {pakName}...");
						string destination = Path.Combine(outputDirectory, pakName);

						//In case the foldername == the pak name and we're only extracting one pak
						if (totalWork == 1 && Path.GetDirectoryName(outputDirectory).Equals(pakName))
						{
							destination = outputDirectory;
						}
						var success = await DivinityFileUtils.ExtractPackageAsync(path, destination, MainProgressToken.Token);
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
						ProcessHelper.TryOpenPath(openOutputPath);
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

		if (SelectedPakMods.Count == 1)
		{
			ExtractSelectedMods_ChooseFolder();
		}
		else
		{
			MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, $"Extract the following mods?\n'{String.Join("\n", SelectedPakMods.Select(x => $"{x.DisplayName}"))}", "Extract Mods?",
			MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, Window.MessageBoxStyle);
			if (result == MessageBoxResult.Yes)
			{
				ExtractSelectedMods_ChooseFolder();
			}
		}
	}

	private void ExtractSelectedAdventure()
	{
		if (SelectedAdventureMod == null || SelectedAdventureMod.IsEditorMod || SelectedAdventureMod.IsLarianMod || !File.Exists(SelectedAdventureMod.FilePath))
		{
			var displayName = SelectedAdventureMod != null ? SelectedAdventureMod.DisplayName : "";
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

			MainProgressTitle = $"Extracting {SelectedAdventureMod.DisplayName}...";
			MainProgressValue = 0d;
			MainProgressToken = new CancellationTokenSource();
			CanCancelProgress = true;
			MainProgressIsActive = true;

			var openOutputPath = dialog.SelectedPath;

			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				if (MainProgressToken.IsCancellationRequested) return Disposable.Empty;
				var path = SelectedAdventureMod.FilePath;
				var success = false;
				try
				{
					string pakName = Path.GetFileNameWithoutExtension(path);
					RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Extracting {pakName}...");
					string destination = Path.Combine(outputDirectory, pakName);
					if (Path.GetDirectoryName(outputDirectory).Equals(pakName))
					{
						destination = outputDirectory;
					}
					openOutputPath = destination;
					success = await DivinityFileUtils.ExtractPackageAsync(path, destination, MainProgressToken.Token);
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
						ProcessHelper.TryOpenPath(openOutputPath);
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

	private int SortModOrder(DivinityLoadOrderEntry a, DivinityLoadOrderEntry b)
	{
		if (a != null && b != null)
		{
			var moda = mods.Items.FirstOrDefault(x => x.UUID == a.UUID);
			var modb = mods.Items.FirstOrDefault(x => x.UUID == b.UUID);
			if (moda != null && modb != null)
			{
				return moda.Index.CompareTo(modb.Index);
			}
			else if (moda != null)
			{
				return 1;
			}
			else if (modb != null)
			{
				return -1;
			}
		}
		else if (a != null)
		{
			return 1;
		}
		else if (b != null)
		{
			return -1;
		}
		return 0;
	}

	private string LastRenamingOrderName { get; set; } = "";

	public void StopRenaming(bool cancel = false)
	{
		if (IsRenamingOrder)
		{
			if (!cancel)
			{
				LastRenamingOrderName = "";
			}
			else if (!String.IsNullOrEmpty(LastRenamingOrderName))
			{
				SelectedModOrder.Name = LastRenamingOrderName;
				LastRenamingOrderName = "";
			}
			IsRenamingOrder = false;
		}
	}

	private async Task<Unit> ToggleRenamingLoadOrder(object control)
	{
		IsRenamingOrder = !IsRenamingOrder;

		if (IsRenamingOrder)
		{
			LastRenamingOrderName = SelectedModOrder.Name;
		}

		await Task.Delay(50);
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			if (control is ComboBox comboBox)
			{
				var tb = comboBox.FindVisualChildren<TextBox>().FirstOrDefault();
				if (tb != null)
				{
					tb.Focus();
					if (IsRenamingOrder)
					{
						tb.SelectAll();
					}
					else
					{
						tb.Select(0, 0);
					}
				}
			}
			else if (control is TextBox tb)
			{
				if (IsRenamingOrder)
				{
					tb.SelectAll();

				}
				else
				{
					tb.Select(0, 0);
				}
			}
		});
		return Unit.Default;
	}

	public void ClearMissingMods()
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			var totalRemoved = 0;
			if (SelectedModOrder != null)
			{
				totalRemoved = SelectedModOrder.Order.RemoveAll(x => !ModExists(x.UUID));
				SelectedProfile.ActiveMods.Clear();
				SelectedProfile.ActiveMods.AddRange(SelectedModOrder.Order.Select(x => ProfileActiveModDataFromUUID(x.UUID)));
			}

			if (totalRemoved > 0)
			{
				ShowAlert($"Removed {totalRemoved} missing mods from the current order. Save to confirm", AlertType.Warning);
			}
		});
	}

	private void LoadAppConfig()
	{
		AppSettingsLoaded = false;

		var resourcesFolder = DivinityApp.GetAppDirectory(DivinityApp.PATH_RESOURCES);
		var appFeaturesPath = Path.Combine(resourcesFolder, DivinityApp.PATH_APP_FEATURES);
		var defaultPathwaysPath = Path.Combine(resourcesFolder, DivinityApp.PATH_DEFAULT_PATHWAYS);
		var ignoredModsPath = Path.Combine(resourcesFolder, DivinityApp.PATH_IGNORED_MODS);

		DivinityApp.Log($"Loading resources from '{resourcesFolder}'");

		if (File.Exists(appFeaturesPath))
		{
			var appFeaturesDict = DivinityJsonUtils.SafeDeserializeFromPath<Dictionary<string, bool>>(appFeaturesPath);
			if (appFeaturesDict != null)
			{
				foreach (var kvp in appFeaturesDict)
				{
					try
					{
						if (!String.IsNullOrEmpty(kvp.Key))
						{
							AppSettings.Features[kvp.Key.ToLower()] = kvp.Value;
						}
					}
					catch (Exception ex)
					{
						DivinityApp.Log("Error setting feature key:");
						DivinityApp.Log(ex.ToString());
					}
				}
			}
		}

		if (File.Exists(defaultPathwaysPath))
		{
			AppSettings.DefaultPathways = DivinityJsonUtils.SafeDeserializeFromPath<DefaultPathwayData>(defaultPathwaysPath);
		}

		if (File.Exists(ignoredModsPath))
		{
			ignoredModsData = DivinityJsonUtils.SafeDeserializeFromPath<IgnoredModsData>(ignoredModsPath);
			if (ignoredModsData != null)
			{
				if (ignoredModsData.IgnoreBuiltinPath != null)
				{
					foreach (var path in ignoredModsData.IgnoreBuiltinPath)
					{
						if (!String.IsNullOrEmpty(path))
						{
							DivinityModDataLoader.IgnoreBuiltinPath.Add(path.Replace(Path.PathSeparator, '/'));
						}
					}
				}

				foreach (var dict in ignoredModsData.Mods)
				{
					var mod = new DivinityModData(true);
					if (dict.TryGetValue("UUID", out var uuid))
					{
						mod.UUID = (string)uuid;

						if (dict.TryGetValue("Name", out var name))
						{
							mod.Name = (string)name;
						}
						if (dict.TryGetValue("Description", out var desc))
						{
							mod.Description = (string)desc;
						}
						if (dict.TryGetValue("Folder", out var folder))
						{
							mod.Folder = (string)folder;
						}
						if (dict.TryGetValue("Type", out var modType))
						{
							mod.ModType = (string)modType;
						}
						if (dict.TryGetValue("Author", out var author))
						{
							mod.Author = (string)author;
						}
						if (dict.TryGetValue("Version", out var vObj))
						{
							ulong version;
							if (vObj is string vStr)
							{
								version = ulong.Parse(vStr);
							}
							else
							{
								version = Convert.ToUInt64(vObj);
							}
							mod.Version = new DivinityModVersion2(version);
						}
						if (dict.TryGetValue("Tags", out var tags))
						{
							if (tags is string tagsText && !String.IsNullOrWhiteSpace(tagsText))
							{
								mod.AddTags(tagsText.Split(';'));
							}
						}
						var existingIgnoredMod = DivinityApp.IgnoredMods.Lookup(mod.UUID);
						bool added = false;

						if (!existingIgnoredMod.HasValue)
						{
							DivinityApp.IgnoredMods.AddOrUpdate(mod);
							added = true;
						}
						else if (existingIgnoredMod.Value.Version < mod.Version)
						{
							DivinityApp.IgnoredMods.AddOrUpdate(mod);
							added = true;
						}

						if (added) DivinityApp.Log($"Ignored mod added: Name({mod.Name}) UUID({mod.UUID})");
					}
				}

				foreach (var uuid in ignoredModsData.IgnoreDependencies)
				{
					DivinityApp.IgnoredDependencyMods.Add(uuid);
				}

				//DivinityApp.LogMessage("Ignored mods:\n" + String.Join("\n", DivinityApp.IgnoredMods.Select(x => x.Name)));
			}
		}

		AppSettingsLoaded = true;
	}
	public void OnKeyDown(Key key)
	{
		switch (key)
		{
			case Key.Up:
			case Key.Right:
			case Key.Down:
			case Key.Left:
				DivinityApp.IsKeyboardNavigating = true;
				break;
		}
	}

	public void OnKeyUp(Key key)
	{
		if (key == Keys.Confirm.Key)
		{
			CanMoveSelectedMods = true;
		}
	}

	public void AddActiveMod(DivinityModData mod)
	{
		if (!ActiveMods.Any(x => x.UUID == mod.UUID))
		{
			ActiveMods.Add(mod);
			mod.IsActive = true;
			mod.Index = ActiveMods.Count - 1;
			SelectedModOrder.Add(mod);
		}
		InactiveMods.Remove(mod);
	}

	public void RemoveActiveMod(DivinityModData mod)
	{
		SelectedModOrder.Remove(mod);
		ActiveMods.Remove(mod);
		mod.IsActive = false;
		if (mod.IsForceLoadedMergedMod || !mod.IsForceLoaded)
		{
			if (!InactiveMods.Any(x => x.UUID == mod.UUID))
			{
				InactiveMods.Add(mod);
			}
		}
		else
		{
			mod.Index = -1;
			//Safeguard
			InactiveMods.Remove(mod);
		}
	}

	private void OnNexusModsRateLimitsUpdated(NexusModsRateLimitsUpdatedEventArgs e)
	{
		StatusBarRightText = $"NexusMods Limits [Hourly ({e.Limits.HourlyRemaining}/{e.Limits.HourlyLimit}) Daily ({e.Limits.DailyRemaining}/{e.Limits.DailyLimit})]";
	}

	IDisposable _updateOrderTask = null;

	public void UpdateOrderFromActiveMods()
	{
		_updateOrderTask?.Dispose();

		if (SelectedModOrder != null)
		{
			SelectedModOrder.Order.Clear();
			SelectedModOrder.AddRange(ActiveMods, true);
		}
	}

	public MainWindowViewModel() : base()
	{
		Services.RegisterSingleton<IModRegistryService>(new ModRegistryService(mods));

		_settings.InitSubscriptions();

		MainProgressValue = 0d;
		MainProgressIsActive = true;
		StatusBarBusyIndicatorVisibility = Visibility.Collapsed;
		_updateHandler = new ModUpdateHandler();

		exceptionHandler = new MainWindowExceptionHandler(this);
		RxApp.DefaultExceptionHandler = exceptionHandler;

		this.ModUpdatesViewData = new ModUpdatesViewData(this);

		var assembly = Assembly.GetExecutingAssembly();
		var productName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false)).Product;
		AppTitle = productName;
		Version = assembly.GetName().Version;
		Title = $"{productName} {Version}";
		AutoUpdater.InstalledVersion = Version;
		AutoUpdater.AppTitle = Title;
		DivinityApp.Log($"{Title} initializing...");

		this.DropHandler = new ModListDropHandler(this);
		this.DragHandler = new ModListDragHandler(this);

		Activator = new ViewModelActivator();

		this.WhenActivated((CompositeDisposable disposables) =>
		{
			if (!disposables.Contains(this.Disposables)) disposables.Add(this.Disposables);
		});

		UpdateNexusModsLimitsCommand = ReactiveCommand.Create<NexusModsRateLimitsUpdatedEventArgs>(OnNexusModsRateLimitsUpdated, outputScheduler: RxApp.MainThreadScheduler);

		NexusModsDataLoader.RateLimitsUpdated += (sender, e) =>
		{
			UpdateNexusModsLimitsCommand.Execute(e);
		};

		_isLocked = this.WhenAnyValue(x => x.IsDragging, x => x.IsRefreshing, x => x.IsLoadingOrder, (b1, b2, b3) => b1 || b2 || b3).ToProperty(this, nameof(IsLocked));

		_allowDrop = this.WhenAnyValue(x => x.IsLoadingOrder, x => x.IsRefreshing, x => x.IsInitialized, (b1, b2, b3) => !b1 && !b2 && b3)
			.ToProperty(this, nameof(AllowDrop), initialValue: true);

		var whenRefreshing = this.WhenAnyValue(x => x.UpdateHandler.IsRefreshing);
		_updatingBusyIndicatorVisibility = whenRefreshing.Select(PropertyConverters.BoolToVisibility)
			.ToProperty(this, nameof(UpdatingBusyIndicatorVisibility), Visibility.Visible, true, RxApp.MainThreadScheduler);

		_updateCountVisibility = whenRefreshing.Select(b => PropertyConverters.BoolToVisibility(!b))
			.ToProperty(this, nameof(UpdateCountVisibility), Visibility.Visible, true, RxApp.MainThreadScheduler);

		_updatesViewVisibility = this.WhenAnyValue(x => x.ModUpdatesViewVisible)
			.Select(PropertyConverters.BoolToVisibility)
			.ToProperty(this, nameof(UpdatesViewVisibility), Visibility.Collapsed, true, RxApp.MainThreadScheduler);

		_developerModeVisibility = this.WhenAnyValue(x => x.Settings.DebugModeEnabled, x => x.Settings.ExtenderSettings.DeveloperMode)
		.Select(x => PropertyConverters.BoolToVisibility(x.Item1 || x.Item2))
		.ToProperty(this, nameof(DeveloperModeVisibility), Visibility.Collapsed, true, RxApp.MainThreadScheduler);

		bool anyBoolTuple(ValueTuple<bool, bool, bool, bool, bool> b) => b.Item1 || b.Item2 || b.Item3 || b.Item4 || b.Item5;
		_logFolderShortcutButtonVisibility = this.WhenAnyValue(
			x => x.Settings.ExtenderSettings.LogCompile,
			x => x.Settings.ExtenderSettings.LogRuntime,
			x => x.Settings.ExtenderSettings.EnableLogging,
			x => x.Settings.ExtenderSettings.DeveloperMode,
			x => x.Settings.DebugModeEnabled)
		.Select(x => PropertyConverters.BoolToVisibility(anyBoolTuple(x)))
		.ToProperty(this, nameof(LogFolderShortcutButtonVisibility), true, RxApp.MainThreadScheduler);

		_keys = new AppKeys(this);

		#region Keys Setup
		Keys.SaveDefaultKeybindings();

		var canExecuteSaveCommand = this.WhenAnyValue(x => x.CanSaveOrder, (canSave) => canSave == true);
		Keys.Save.AddAction(() => SaveLoadOrder(), canExecuteSaveCommand);

		var canExecuteSaveAsCommand = this.WhenAnyValue(x => x.CanSaveOrder, x => x.MainProgressIsActive, (canSave, p) => canSave && !p);
		Keys.SaveAs.AddAction(SaveLoadOrderAs, canExecuteSaveAsCommand);
		Keys.ImportMod.AddAction(OpenModImportDialog);
		Keys.NewOrder.AddAction(() => AddNewModOrder());

		var canRefreshObservable = this.WhenAnyValue(x => x.IsRefreshing, b => !b).StartWith(true);
		RefreshCommand = ReactiveCommand.Create(() =>
		{
			ModUpdatesViewData?.Clear();
			ModUpdatesViewVisible = ModUpdatesAvailable = false;
			MainProgressTitle = !IsInitialized ? "Loading..." : "Refreshing...";
			MainProgressValue = 0d;
			CanCancelProgress = false;
			MainProgressIsActive = true;
			mods.Clear();
			Profiles.Clear();
			Window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
			Window.TaskbarItemInfo.ProgressValue = 0;
			IsRefreshing = true;
			RxApp.TaskpoolScheduler.ScheduleAsync(RefreshAsync);
		}, canRefreshObservable, RxApp.MainThreadScheduler);

		Keys.Refresh.AddAction(() => RefreshCommand.Execute(Unit.Default).Subscribe(), canRefreshObservable);

		var canRefreshModUpdates = this.WhenAnyValue(x => x.IsRefreshing, x => x.IsRefreshingModUpdates, x => x.AppSettingsLoaded, (b1, b2, b3) => !b1 && !b2 && b3).StartWith(false);

		RefreshModUpdatesCommand = ReactiveCommand.Create(() =>
		{
			ModUpdatesViewData?.Clear();
			ModUpdatesViewVisible = ModUpdatesAvailable = false;
			RefreshAllModUpdatesBackground();
		}, canRefreshModUpdates, RxApp.MainThreadScheduler);

		Keys.RefreshModUpdates.AddAction(() => RefreshModUpdatesCommand.Execute(Unit.Default).Subscribe(), canRefreshModUpdates);

		IObservable<bool> canStartExport = this.WhenAny(x => x.MainProgressToken, (t) => t != null).StartWith(false);
		Keys.ExportOrderToZip.AddAction(ExportLoadOrderToArchive_Start, canStartExport);
		Keys.ExportOrderToArchiveAs.AddAction(ExportLoadOrderToArchiveAs, canStartExport);

		var anyActiveObservable = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);
		Keys.ExportOrderToList.AddAction(ExportLoadOrderToTextFileAs, anyActiveObservable);

		var canOpenDialogWindow = this.WhenAnyValue(x => x.MainProgressIsActive).Select(x => !x);
		Keys.ImportOrderFromSave.AddAction(ImportOrderFromSaveToCurrent, canOpenDialogWindow);
		Keys.ImportOrderFromSaveAsNew.AddAction(ImportOrderFromSaveAsNew, canOpenDialogWindow);
		Keys.ImportOrderFromFile.AddAction(ImportOrderFromFile, canOpenDialogWindow);
		Keys.ImportOrderFromZipFile.AddAction(ImportOrderFromArchive, canOpenDialogWindow);

		Keys.OpenDonationLink.AddAction(() =>
		{
			ProcessHelper.TryOpenUrl(DivinityApp.URL_DONATION);
		});

		Keys.OpenRepositoryPage.AddAction(() =>
		{
			ProcessHelper.TryOpenUrl(DivinityApp.URL_REPO);
		});

		Keys.ToggleViewTheme.AddAction(() =>
		{
			Settings.DarkThemeEnabled = !Settings.DarkThemeEnabled;
		});

		Keys.ToggleFileNameDisplay.AddAction(() =>
		{
			Settings.DisplayFileNames = !Settings.DisplayFileNames;

			foreach (var m in Mods)
			{
				m.DisplayFileForName = Settings.DisplayFileNames;
			}
		});

		Keys.DeleteSelectedMods.AddAction(() =>
		{
			IEnumerable<DivinityModData> targetList = null;
			if (DivinityApp.IsKeyboardNavigating)
			{
				var modLayout = View.ModLayout;
				if (modLayout != null)
				{
					if (modLayout.ActiveModsListView.IsKeyboardFocusWithin)
					{
						targetList = ActiveMods;
					}
					else if (modLayout.ForceLoadedModsListView.IsKeyboardFocusWithin)
					{
						targetList = ForceLoadedMods;
					}
					else
					{
						targetList = InactiveMods;
					}
				}
			}
			else
			{
				targetList = mods.Items;
			}

			if (targetList != null)
			{
				var selectedMods = targetList.Where(x => x.IsSelected);
				var selectedEligableMods = selectedMods.Where(x => x.CanDelete).ToList();

				if (selectedEligableMods.Count > 0)
				{
					DeleteMods(selectedEligableMods);
				}
				else
				{
					this.View.DeleteFilesView.ViewModel.Close();
				}
				if (selectedMods.Any(x => x.IsEditorMod))
				{
					ShowAlert("Editor mods cannot be deleted with the Mod Manager", AlertType.Warning, 60);
				}
			}
			else
			{
				DivinityApp.Log("No target list to delete mods from.");
			}
		});

		#endregion

		var canToggleUpdatesView = this.WhenAnyValue(x => x.ModUpdatesViewVisible, x => x.ModUpdatesAvailable, (isVisible, hasUpdates) => isVisible || hasUpdates);
		void toggleUpdatesView()
		{
			ModUpdatesViewVisible = !ModUpdatesViewVisible;
		};
		Keys.ToggleUpdatesView.AddAction(toggleUpdatesView, canToggleUpdatesView);
		ToggleUpdatesViewCommand = ReactiveCommand.Create(toggleUpdatesView, canToggleUpdatesView);

		IObservable<bool> canCancelProgress = this.WhenAnyValue(x => x.CanCancelProgress).StartWith(true);
		CancelMainProgressCommand = ReactiveCommand.Create(() =>
		{
			if (MainProgressToken != null && MainProgressToken.Token.CanBeCanceled)
			{
				MainProgressToken.Token.Register(() => { MainProgressIsActive = false; });
				MainProgressToken.Cancel();
			}
		}, canCancelProgress);


		CopyPathToClipboardCommand = ReactiveCommand.Create((string path) =>
		{
			if (!String.IsNullOrWhiteSpace(path))
			{
				Clipboard.SetText(path);
				ShowAlert($"Copied '{path}' to clipboard", 0, 10);
			}
			else
			{
				ShowAlert($"Path '{path}' not found", AlertType.Danger, 30);
			}
		});

		RenameSaveCommand = ReactiveCommand.Create(RenameSave_Start, canOpenDialogWindow);

		CopyOrderToClipboardCommand = ReactiveCommand.Create(() =>
		{
			try
			{
				if (ActiveMods.Count > 0)
				{
					string text = "";
					for (int i = 0; i < ActiveMods.Count; i++)
					{
						var mod = ActiveMods[i];
						text += $"{mod.Index}. {mod.DisplayName}";
						if (i < ActiveMods.Count - 1) text += Environment.NewLine;
					}
					Clipboard.SetText(text);
					ShowAlert("Copied mod order to clipboard", AlertType.Info, 10);
				}
				else
				{
					ShowAlert("Current order is empty", AlertType.Warning, 10);
				}
			}
			catch (Exception ex)
			{
				ShowAlert($"Error copying order to clipboard: {ex}", AlertType.Danger, 15);
			}
		});

		var profileChanged = this.WhenAnyValue(x => x.SelectedProfileIndex, x => x.Profiles.Count).Select(x => Profiles.ElementAtOrDefault(x.Item1));
		_selectedProfile = profileChanged.ToProperty(this, nameof(SelectedProfile)).DisposeWith(Disposables);
		var hasNonNullProfile = this.WhenAnyValue(x => x.SelectedProfile).Select(x => x != null);
		_hasProfile = hasNonNullProfile.ToProperty(this, nameof(HasProfile)).DisposeWith(Disposables);

		Keys.ExportOrderToGame.AddAction(ExportLoadOrder, hasNonNullProfile);

		profileChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe((profile) =>
		{
			if (profile != null && profile.ActiveMods != null && profile.ActiveMods.Count > 0)
			{
				var adventureModData = AdventureMods.FirstOrDefault(x => profile.ActiveMods.Any(y => y.UUID == x.UUID));
				//Migrate old profiles from Gustav to GustavDev
				if (adventureModData != null && (adventureModData.UUID == DivinityApp.GUSTAV_UUID || adventureModData.UUID == DivinityApp.GUSTAVDEV_UUID))
				{
					var main = mods.Lookup(DivinityApp.MAIN_CAMPAIGN_UUID);
					if (main.HasValue)
					{
						adventureModData = mods.Lookup(DivinityApp.MAIN_CAMPAIGN_UUID).Value;
					}
				}
				if (adventureModData != null)
				{
					var nextAdventure = AdventureMods.IndexOf(adventureModData);
					DivinityApp.Log($"Found adventure mod in profile: {adventureModData.Name} | {nextAdventure}");
					if (nextAdventure > -1)
					{
						SelectedAdventureModIndex = nextAdventure;
					}
				}
			}
		});

		_selectedModOrder = this.WhenAnyValue(x => x.SelectedModOrderIndex, x => x.ModOrderList.Count).
			Select(x => ModOrderList.ElementAtOrDefault(x.Item1)).ToProperty(this, nameof(SelectedModOrder));
		_selectedModOrderName = this.WhenAnyValue(x => x.SelectedModOrder).WhereNotNull().Select(x => x.Name).ToProperty(this, nameof(SelectedModOrderName), true, RxApp.MainThreadScheduler);
		_isBaseLoadOrder = this.WhenAnyValue(x => x.SelectedModOrder).Select(x => x != null && x.IsModSettings).ToProperty(this, nameof(IsBaseLoadOrder), true, RxApp.MainThreadScheduler);

		//Throttle in case the index changes quickly in a short timespan
		this.WhenAnyValue(vm => vm.SelectedModOrderIndex).ObserveOn(RxApp.MainThreadScheduler).Subscribe((_) =>
		{
			if (!this.IsRefreshing && SelectedModOrderIndex > -1)
			{
				if (SelectedModOrder != null && !IsLoadingOrder)
				{
					if (!SelectedModOrder.OrderEquals(ActiveMods.Select(x => x.UUID)))
					{
						if (LoadModOrder(SelectedModOrder))
						{
							DivinityApp.Log($"Successfully loaded order {SelectedModOrder.Name}.");
						}
						else
						{
							DivinityApp.Log($"Failed to load order {SelectedModOrder.Name}.");
						}
					}
					else
					{
						DivinityApp.Log($"Order changed to {SelectedModOrder.Name}. Skipping list loading since the orders match.");
					}
				}
			}
		});

		this.WhenAnyValue(vm => vm.SelectedProfileIndex, (index) => index > -1 && index < Profiles.Count)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe((b) =>
		{
			if (!IsRefreshing && b)
			{
				if (SelectedModOrder != null)
				{
					BuildModOrderList(SelectedModOrderIndex);
				}
				else
				{
					BuildModOrderList(0);
				}
			}
		});

		var modsConnection = mods.Connect();
		modsConnection.Publish();

		modsConnection.Filter(x => x.IsUserMod).Bind(out _userMods).Subscribe();
		modsConnection.AutoRefresh(x => x.CanAddToLoadOrder).Filter(x => x.CanAddToLoadOrder).Bind(out addonMods).Subscribe();
		modsConnection.AutoRefresh(x => x.ForceAllowInLoadOrder)
			.Filter(x => x.IsForceLoaded && !x.IsForceLoadedMergedMod && !x.ForceAllowInLoadOrder)
			.ObserveOn(RxApp.MainThreadScheduler).Bind(out _forceLoadedMods).Subscribe();

		//Throttle filters so they only happen when typing stops for 500ms

		this.WhenAnyValue(x => x.ActiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
			Subscribe((s) => { OnFilterTextChanged(s, ActiveMods); });

		this.WhenAnyValue(x => x.InactiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
			Subscribe((s) => { OnFilterTextChanged(s, InactiveMods); });

		ActiveMods.WhenAnyPropertyChanged(nameof(DivinityModData.Index)).Throttle(TimeSpan.FromMilliseconds(25)).Subscribe(_ =>
		{
			SelectedModOrder?.Sort(SortModOrder);
		});

		var selectedModsConnection = modsConnection.AutoRefresh(x => x.IsSelected, TimeSpan.FromMilliseconds(25)).AutoRefresh(x => x.IsActive, TimeSpan.FromMilliseconds(25)).Filter(x => x.IsSelected);

		_activeSelected = selectedModsConnection.Filter(x => x.IsActive || !x.CanAddToLoadOrder).Count().ToProperty(this, nameof(ActiveSelected), true, RxApp.MainThreadScheduler);
		_inactiveSelected = selectedModsConnection.Filter(x => !x.IsActive && x.CanAddToLoadOrder).Count().ToProperty(this, nameof(InactiveSelected), true, RxApp.MainThreadScheduler);

		_activeSelectedText = this.WhenAnyValue(x => x.ActiveSelected, x => x.TotalActiveModsHidden).Select(x => SelectedToLabel(x.Item1, x.Item2)).ToProperty(this, nameof(ActiveSelectedText), true, RxApp.MainThreadScheduler);
		_inactiveSelectedText = this.WhenAnyValue(x => x.InactiveSelected, x => x.TotalInactiveModsHidden).Select(x => SelectedToLabel(x.Item1, x.Item2)).ToProperty(this, nameof(InactiveSelectedText), true, RxApp.MainThreadScheduler);

		_activeModsFilterResultText = this.WhenAnyValue(x => x.TotalActiveModsHidden).Select(x => HiddenToLabel(x, ActiveMods.Count)).ToProperty(this, nameof(ActiveModsFilterResultText), true, RxApp.MainThreadScheduler);

		_inactiveModsFilterResultText = this.WhenAnyValue(x => x.TotalInactiveModsHidden).Select(x => HiddenToLabel(x, InactiveMods.Count)).ToProperty(this, nameof(InactiveModsFilterResultText), true, RxApp.MainThreadScheduler);

		DivinityApp.Events.OrderNameChanged += OnOrderNameChanged;

		modsConnection.Filter(x => x.ModType == "Adventure" && !x.IsHidden).Bind(out adventureMods).DisposeMany().Subscribe();
		_selectedAdventureMod = this.WhenAnyValue(x => x.SelectedAdventureModIndex, x => x.AdventureMods.Count, (index, count) => index >= 0 && count > 0 && index < count).
			Where(b => b == true).Select(x => AdventureMods[SelectedAdventureModIndex]).
			ToProperty(this, x => x.SelectedAdventureMod).DisposeWith(this.Disposables);

		var adventureModCanOpenObservable = this.WhenAnyValue(x => x.SelectedAdventureMod, (mod) => mod != null && !mod.IsLarianMod);
		adventureModCanOpenObservable.Subscribe();

		this.WhenAnyValue(x => x.SelectedAdventureModIndex)
			.Throttle(TimeSpan.FromMilliseconds(50))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(i =>
		{
			if (AdventureMods != null && SelectedAdventureMod != null && SelectedProfile != null && SelectedProfile.ActiveMods != null)
			{
				if (!SelectedProfile.ActiveMods.Any(m => m.UUID == SelectedAdventureMod.UUID))
				{
					SelectedProfile.ActiveMods.RemoveAll(r => AdventureMods.Any(y => y.UUID == r.UUID));
					SelectedProfile.ActiveMods.Insert(0, SelectedAdventureMod.ToProfileModData());
				}
			}
		});

		OpenAdventureModInFileExplorerCommand = ReactiveCommand.Create<string>((path) =>
		{
			DivinityApp.Commands.OpenInFileExplorer(path);
		}, adventureModCanOpenObservable);

		CopyAdventureModPathToClipboardCommand = ReactiveCommand.Create<string>((path) =>
		{
			if (!String.IsNullOrWhiteSpace(path))
			{
				Clipboard.SetText(path);
				ShowAlert($"Copied '{path}' to clipboard", 0, 10);
			}
			else
			{
				ShowAlert($"Path '{path}' not found", AlertType.Danger, 30);
			}
		}, adventureModCanOpenObservable);

		var canCheckForUpdates = this.WhenAnyValue(x => x.MainProgressIsActive, b => b == false);
		void checkForUpdatesAction()
		{
			ShowAlert("Checking for updates...", AlertType.Info, 30);
			CheckForUpdates(true);
			SaveSettings();
		}
		CheckForAppUpdatesCommand = ReactiveCommand.Create(checkForUpdatesAction, canCheckForUpdates);
		Keys.CheckForUpdates.AddAction(checkForUpdatesAction, canCheckForUpdates);

		var canRenameOrder = this.WhenAnyValue(x => x.SelectedModOrderIndex, (i) => i > 0);
		ToggleOrderRenamingCommand = ReactiveCommand.CreateFromTask<object, Unit>(ToggleRenamingLoadOrder, canRenameOrder, RxApp.MainThreadScheduler);

		var canDeleteOrder = this.WhenAnyValue(x => x.MainProgressIsActive, x => x.SelectedModOrderIndex).Select(x => !x.Item1 && x.Item2 > 0);
		DeleteOrderCommand = ReactiveCommand.Create<DivinityLoadOrder>(DeleteOrder, canDeleteOrder, RxApp.MainThreadScheduler);

		modsConnection.AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected && !x.IsEditorMod && File.Exists(x.FilePath)).Bind(out selectedPakMods).Subscribe();

		var anyPakModSelectedObservable = this.WhenAnyValue(x => x.SelectedPakMods.Count, (count) => count > 0);
		Keys.ExtractSelectedMods.AddAction(ExtractSelectedMods_Start, anyPakModSelectedObservable);

		var canExtractAdventure = this.WhenAnyValue(x => x.SelectedAdventureMod, m => m != null && !m.IsEditorMod && !m.IsLarianMod);
		Keys.ExtractSelectedAdventure.AddAction(ExtractSelectedAdventure, canExtractAdventure);

		this.WhenAnyValue(x => x.ModUpdatesViewData.NewAvailable,
			x => x.ModUpdatesViewData.UpdatesAvailable, (b1, b2) => b1 || b2).BindTo(this, x => x.ModUpdatesAvailable);

		ModUpdatesViewData.CloseView = new Action<bool>((bool refresh) =>
		{
			ModUpdatesViewData.Clear();
			if (refresh) RefreshCommand.Execute(Unit.Default).Subscribe();
			ModUpdatesViewVisible = false;
			Window.Activate();
		});

		Keys.SpeakActiveModOrder.AddAction(() =>
		{
			if (ActiveMods.Count > 0)
			{
				var text = string.Join(", ", ActiveMods.Select(x => x.DisplayName));
				Services.ScreenReader.Speak($"{ActiveMods.Count} mods in the active order, including:\n{text}", true);
			}
			else
			{
				Services.ScreenReader.Speak($"Zero mods are active.", true);
			}
		});

		Keys.StopSpeaking.AddAction(() =>
		{
			Services.ScreenReader.Silence();
		});

		SaveSettingsSilentlyCommand = ReactiveCommand.Create(SaveSettings);

		_isDeletingFiles = this.WhenAnyValue(x => x.View.DeleteFilesView.ViewModel.IsVisible).ToProperty(this, nameof(IsDeletingFiles), true, RxApp.MainThreadScheduler);

		_hideModList = this.WhenAnyValue(x => x.MainProgressIsActive, x => x.IsDeletingFiles, (a, b) => a || b)
			.ToProperty(this, nameof(HideModList), true, false, RxApp.MainThreadScheduler);

		var forceLoadedModsConnection = this.ForceLoadedMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);
		_hasForceLoadedMods = forceLoadedModsConnection.Count().StartWith(0).Select(x => x > 0).ToProperty(this, nameof(HasForceLoadedMods), false, true, RxApp.MainThreadScheduler);

		DivinityInteractions.ConfirmModDeletion.RegisterHandler(async interaction =>
		{
			var sentenceStart = interaction.Input.PermanentlyDelete ? "Permanently delete" : "Delete";
			var msg = $"{sentenceStart} {interaction.Input.Total} mod file(s)?";

			var confirmed = await Observable.Start((Func<bool>)(() =>
			{
				MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(Window, msg, "Confirm Mod Deletion",
				MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, Window.MessageBoxStyle);
				if (result == MessageBoxResult.Yes)
				{
					return true;
				}
				return false;
			}), RxApp.MainThreadScheduler);
			interaction.SetOutput(confirmed);
		});

		CanSaveOrder = true;
		LayoutMode = 0;

		ActiveMods.CollectionChanged += (o, e) =>
		{
			if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset)
			{
				HasExported = false;
			}
			_updateOrderTask?.Dispose();
			_updateOrderTask = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), UpdateOrderFromActiveMods);
		};

		var fwService = Services.Get<IFileWatcherService>();
		_modSettingsWatcher = fwService.WatchDirectory("", "*modsettings.lsx");
		//modSettingsWatcher.PauseWatcher(true);
		this.WhenAnyValue(x => x.SelectedProfile).WhereNotNull().Select(x => x.Folder).Subscribe(path =>
		{
			_modSettingsWatcher.SetDirectory(path);
		});

		IDisposable checkModSettingsTask = null;

		_modSettingsWatcher.FileChanged.Subscribe(e =>
		{
			if (SelectedModOrder != null && HasExported)
			{
				//var exeName = !Settings.LaunchDX11 ? "bg3" : "bg3_dx11";
				//var isGameRunning = Process.GetProcessesByName(exeName).Length > 0;
				checkModSettingsTask?.Dispose();
				checkModSettingsTask = RxApp.TaskpoolScheduler.ScheduleAsync(TimeSpan.FromSeconds(2), async (sch, cts) =>
				{
					var activeCount = ActiveMods.Count;
					var modSettingsData = await DivinityModDataLoader.LoadModSettingsFileAsync(e.FullPath);
					if (activeCount > 0 && modSettingsData.CountActive() <= 0)
					{
						ShowAlert("The active load order (modsettings.lsx) has been reset externally", AlertType.Danger);
						RxApp.MainThreadScheduler.Schedule(() =>
						{
							//Window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
							Window.FlashTaskbar();
							var result = Xceed.Wpf.Toolkit.MessageBox.Show(Window,
							"The active load order (modsettings.lsx) has been reset externally, which has deactivated your mods.\nOne or more mods may be invalid in your current load order.",
							"Mod Order Reset",
							MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, Window.MessageBoxStyle);
						});
					}
					HasExported = false;
				});
			}
		});
	}
}