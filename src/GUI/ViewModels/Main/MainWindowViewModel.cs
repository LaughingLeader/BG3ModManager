using AutoUpdaterDotNET;

using DivinityModManager.Extensions;
using DivinityModManager.Models;
using DivinityModManager.Models.App;
using DivinityModManager.Models.Mod;
using DivinityModManager.Models.NexusMods;
using DivinityModManager.Models.Settings;
using DivinityModManager.Models.Updates;
using DivinityModManager.ModUpdater.Cache;
using DivinityModManager.Util;
using DivinityModManager.Views.Main;
using DivinityModManager.Windows;
using DivinityModManager.ViewModels.Main;

using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Reactive.Bindings.Extensions;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers;
using SharpCompress.Writers;

using Splat;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ZstdSharp;

namespace DivinityModManager.ViewModels;

public class MainWindowViewModel : BaseHistoryViewModel, IDivinityAppViewModel, IScreen
{
	private const int ARCHIVE_BUFFER = 128000;

    public RoutingState Router { get; }
	public ViewManager Views { get; }

    [Reactive] public MainWindow Window { get; private set; }
	[Reactive] public DivinityModManager.Views.MainViewControl View { get; private set; }
	public DownloadActivityBarViewModel DownloadBar { get; private set; }

	public DivinityModManager.Views.IModViewLayout Layout { get; set; }

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

	private readonly IModUpdaterService _updater;
	private readonly ISettingsService _settings;

	[Reactive] public string Title { get; set; }
	[Reactive] public string Version { get; set; }

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

	[ObservableAsProperty] public DivinityModData SelectedAdventureMod { get; }
	[ObservableAsProperty] public Visibility AdventureModBoxVisibility { get; }

	protected ReadOnlyObservableCollection<DivinityModData> selectedPakMods;
	public ReadOnlyObservableCollection<DivinityModData> SelectedPakMods => selectedPakMods;

	protected readonly SourceCache<DivinityModData, string> workshopMods = new(mod => mod.UUID);

	protected ReadOnlyObservableCollection<DivinityModData> workshopModsCollection;
	public ReadOnlyObservableCollection<DivinityModData> WorkshopMods => workshopModsCollection;

	public DivinityPathwayData PathwayData { get; private set; } = new DivinityPathwayData();

	public ModUpdatesViewData ModUpdatesViewData { get; private set; }

	public AppSettings AppSettings { get; private set; }
	public ModManagerSettings Settings { get; private set; }
	public UserModConfig UserModConfig { get; private set; }

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
	[ObservableAsProperty] public int ActiveSelected { get; }
	[ObservableAsProperty] public int InactiveSelected { get; }
	[ObservableAsProperty] public int OverrideModsSelected { get; }
	[ObservableAsProperty] public string ActiveSelectedText { get; }
	[ObservableAsProperty] public string InactiveSelectedText { get; }
	[ObservableAsProperty] public string OverrideModsSelectedText { get; }
	[ObservableAsProperty] public string ActiveModsFilterResultText { get; }
	[ObservableAsProperty] public string InactiveModsFilterResultText { get; }
	[ObservableAsProperty] public string OverrideModsFilterResultText { get; }

	[Reactive] public string ActiveModFilterText { get; set; }
	[Reactive] public string InactiveModFilterText { get; set; }
	[Reactive] public string OverrideModsFilterText { get; set; }

	[Reactive] public int SelectedProfileIndex { get; set; }
	[Reactive] public DivinityProfileData SelectedProfile { get; private set; }
	[ObservableAsProperty] public bool HasProfile { get; }

	public ObservableCollectionExtended<DivinityLoadOrder> ModOrderList { get; set; } = new ObservableCollectionExtended<DivinityLoadOrder>();

	[Reactive] public int SelectedModOrderIndex { get; set; }

	[Reactive] public DivinityLoadOrder SelectedModOrder { get; private set; }
	[ObservableAsProperty] public string SelectedModOrderName { get; }
	[ObservableAsProperty] public bool IsBaseLoadOrder { get; }

	public List<DivinityLoadOrder> SavedModOrderList { get; set; } = new List<DivinityLoadOrder>();

	[Reactive] public bool AppSettingsLoaded { get; set; }
	[Reactive] public bool CanMoveSelectedMods { get; set; }
	[Reactive] public bool CanSaveOrder { get; set; }
	[Reactive] public bool GameIsRunning { get; private set; }
	[Reactive] public bool CanForceLaunchGame { get; set; }
	[Reactive] public bool IsDragging { get; set; }
	[Reactive] public bool IsLoadingOrder { get; set; }
	[Reactive] public bool IsRefreshing { get; private set; }
	[Reactive] public bool IsRefreshingModUpdates { get; private set; }
	[Reactive] public bool IsRenamingOrder { get; set; }
	[Reactive] public bool OrderJustLoaded { get; set; }
	[Reactive] public int LayoutMode { get; set; }

	[Reactive] public string StatusText { get; set; }
	[Reactive] public string StatusBarRightText { get; set; }

	[Reactive] public bool ModUpdatesAvailable { get; set; }
	[Reactive] public bool ModUpdatesViewVisible { get; set; }
	[Reactive] public bool HighlightExtenderDownload { get; set; }
	[Reactive] public bool GameDirectoryFound { get; set; }

	/// <summary>Used to locked certain functionality when data is loading or the user is dragging an item.</summary>
	[ObservableAsProperty] public bool IsLocked { get; }
	[ObservableAsProperty] public bool AllowDrop { get; }
	[ObservableAsProperty] public bool CanLaunchGame { get; }
	[ObservableAsProperty] public bool HideModList { get; }
	[ObservableAsProperty] public bool HasForceLoadedMods { get; }
	[ObservableAsProperty] public bool IsDeletingFiles { get; }

	[ObservableAsProperty] public string OpenGameButtonToolTip { get; }

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
	[Reactive] public Visibility StatusBarBusyIndicatorVisibility { get; set; }
	[ObservableAsProperty] public bool GitHubModSupportEnabled { get; }
	[ObservableAsProperty] public bool NexusModsSupportEnabled { get; }
	[ObservableAsProperty] public bool SteamWorkshopSupportEnabled { get; }
	[ObservableAsProperty] public string NexusModsLimitsText { get; }
	[ObservableAsProperty] public BitmapImage NexusModsProfileBitmapImage { get; }
	[ObservableAsProperty] public Visibility NexusModsProfileAvatarVisibility { get; }
	[ObservableAsProperty] public Visibility UpdatingBusyIndicatorVisibility { get; }
	[ObservableAsProperty] public Visibility UpdateCountVisibility { get; }
	[ObservableAsProperty] public Visibility UpdatesViewVisibility { get; }
	[ObservableAsProperty] public Visibility DeveloperModeVisibility { get; }
	[ObservableAsProperty] public Visibility LogFolderShortcutButtonVisibility { get; }

	public ICommand ToggleUpdatesViewCommand { get; private set; }
	public ICommand CheckForAppUpdatesCommand { get; set; }
	public ReactiveCommand<UpdateInfoEventArgs, Unit> OnAppUpdateCheckedCommand { get; set; }
	public ICommand CancelMainProgressCommand { get; set; }
	public ICommand RenameSaveCommand { get; private set; }
	public ICommand CopyOrderToClipboardCommand { get; private set; }
	public ICommand ExportOrderAsListCommand { get; private set; }
	public ICommand ConfirmCommand { get; set; }
	public ICommand FocusFilterCommand { get; set; }
	public ICommand SaveSettingsSilentlyCommand { get; private set; }
	public ReactiveCommand<DivinityLoadOrder, Unit> DeleteOrderCommand { get; private set; }
	public ReactiveCommand<object, Unit> ToggleOrderRenamingCommand { get; set; }
	public ReactiveCommand<Unit, Unit> RefreshCommand { get; private set; }
	public ReactiveCommand<Unit, Unit> RefreshModUpdatesCommand { get; private set; }
	public ICommand CheckForGitHubModUpdatesCommand { get; private set; }
	public ICommand CheckForNexusModsUpdatesCommand { get; private set; }
	public ICommand CheckForSteamWorkshopUpdatesCommand { get; private set; }
	public ICommand FetchNexusModsInfoFromFilesCommand { get; private set; }
	public EventHandler OnRefreshed { get; set; }

	#region DungeonMaster Support

	//TODO - Waiting for DM mode to be released
	[ObservableAsProperty] public Visibility GameMasterModeVisibility { get; }

	protected SourceList<DivinityGameMasterCampaign> gameMasterCampaigns = new();

	private readonly ReadOnlyObservableCollection<DivinityGameMasterCampaign> gameMasterCampaignsData;
	public ReadOnlyObservableCollection<DivinityGameMasterCampaign> GameMasterCampaigns => gameMasterCampaignsData;

	private int selectedGameMasterCampaignIndex = 0;

	public int SelectedGameMasterCampaignIndex
	{
		get => selectedGameMasterCampaignIndex;
		set
		{
			this.RaiseAndSetIfChanged(ref selectedGameMasterCampaignIndex, value);
			this.RaisePropertyChanged("SelectedGameMasterCampaign");
		}
	}
	public bool UserChangedSelectedGMCampaign { get; set; }

	[ObservableAsProperty] public DivinityGameMasterCampaign SelectedGameMasterCampaign { get; }
	public ICommand OpenGameMasterCampaignInFileExplorerCommand { get; private set; }
	public ICommand CopyGameMasterCampaignPathToClipboardCommand { get; private set; }

	private readonly AppServices.IFileWatcherWrapper _modSettingsWatcher;

	private void SetLoadedGMCampaigns(IEnumerable<DivinityGameMasterCampaign> data)
	{
		string lastSelectedCampaignUUID = "";
		if (UserChangedSelectedGMCampaign && SelectedGameMasterCampaign != null)
		{
			lastSelectedCampaignUUID = SelectedGameMasterCampaign.UUID;
		}

		gameMasterCampaigns.Clear();
		if (data != null)
		{
			gameMasterCampaigns.AddRange(data);
		}

		DivinityGameMasterCampaign nextSelected = null;

		if (String.IsNullOrEmpty(lastSelectedCampaignUUID) || !IsInitialized)
		{
			nextSelected = gameMasterCampaigns.Items.OrderByDescending(x => x.LastModified ?? DateTimeOffset.MinValue).FirstOrDefault();

		}
		else
		{
			nextSelected = gameMasterCampaigns.Items.FirstOrDefault(x => x.UUID == lastSelectedCampaignUUID);
		}

		if (nextSelected != null)
		{
			SelectedGameMasterCampaignIndex = gameMasterCampaigns.Items.IndexOf(nextSelected);
		}
		else
		{
			SelectedGameMasterCampaignIndex = 0;
		}
	}

	public bool LoadGameMasterCampaignModOrder(DivinityGameMasterCampaign campaign)
	{
		if (campaign.Dependencies == null) return false;

		var currentOrder = ModOrderList.First();
		currentOrder.Order.Clear();

		List<DivinityMissingModData> missingMods = new();
		if (campaign.Dependencies.Count > 0)
		{
			int index = 0;
			foreach (var entry in campaign.Dependencies.Items)
			{
				if (TryGetMod(entry.UUID, out var mod))
				{
					mod.IsActive = true;
					currentOrder.Add(mod);
					index++;
					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (!DivinityModDataLoader.IgnoreMod(dependency.UUID) && !mods.Items.Any(x => x.UUID == dependency.UUID) &&
								!missingMods.Any(x => x.UUID == dependency.UUID))
							{
								missingMods.Add(new DivinityMissingModData
								{
									Index = -1,
									Name = dependency.Name,
									UUID = dependency.UUID,
									Dependency = true
								});
							}
						}
					}
				}
				else if (!DivinityModDataLoader.IgnoreMod(entry.UUID) && !missingMods.Any(x => x.UUID == entry.UUID))
				{
					missingMods.Add(new DivinityMissingModData
					{
						Index = index,
						Name = entry.Name,
						UUID = entry.UUID
					});
				}
			}
		}

		DivinityApp.Log($"Updated 'Current' with dependencies from GM campaign {campaign.Name}.");

		if (SelectedModOrderIndex == 0)
		{
			DivinityApp.Log($"Loading mod order for GM campaign {campaign.Name}.");
			LoadModOrder(currentOrder, missingMods);
		}

		return true;
	}

	#endregion

	public bool DebugMode { get; set; }

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
			//UpdateExtenderVersionForAllMods();

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

	private bool FilterDependencies(DivinityModDependencyData x, bool devMode)
	{
		if (!devMode)
		{
			return !DivinityModDataLoader.IgnoreModDependency(x.UUID);
		}
		return true;
	}

	private Func<DivinityModDependencyData, bool> MakeDependencyFilter(bool b)
	{
		return (x) => FilterDependencies(x, b);
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
				await DivinityModDataLoader.UpdateLauncherPreferencesAsync(GetLarianStudiosAppDataFolder(), !Settings.DisableLauncherTelemetry, !Settings.DisableLauncherModWarnings);
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
			var isModded = ActiveMods.Count > 0 ? 1 : 0;
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
	private string GetLaunchGameTooltip(ValueTuple<string, bool, bool, bool> x)
	{
		var exePath = x.Item1;
		var limitToSingle = x.Item2;
		var isRunning = x.Item3;
		var canForce = x.Item4;
		if (exePath.IsExistingFile())
		{
			if (isRunning && limitToSingle)
			{
				if (canForce) return "Force Launch the Game";
				return "Launch Game [Locked]\nThe game is already running - Opening the game again will create debug profiles, which may be unintended\nHold Shift to bypass this restriction";
			}
		}
		else
		{
			return $"Launch Game [Not Found]\nThe exe path '{exePath}' does not exist\nConfigure the 'Game Executable Path' in the Preferences window";
		}
		return "Launch Game";
	}

	private void InitSettingsBindings()
	{
		DivinityApp.DependencyFilter = Settings.WhenAnyValue(x => x.DebugModeEnabled).Select(MakeDependencyFilter);

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
		whenGameExeProperties.Select(GetLaunchGameTooltip).ToUIProperty(this, x => x.OpenGameButtonToolTip, "Launch Game");

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

		Settings.WhenAnyValue(x => x.DarkThemeEnabled).Skip(1).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
		{
			View.UpdateColorTheme(b);
			SaveSettings();
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
				SetGamePathways(Settings.GameDataPath, x);
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

	private void ApplyUserModConfig()
	{
		foreach (var mod in mods.Items)
		{
			var config = _settings.ModConfig.Mods.Lookup(mod.UUID);
			if (config.HasValue)
			{
				mod.ApplyModConfig(config.Value);
			}
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

		Settings.DefaultExtenderLogDirectory = Path.Join(GetLarianStudiosAppDataFolder(), "Baldur's Gate 3", "Extender Logs");

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

		_updater.GitHub.IsEnabled = githubSupportEnabled;
		_updater.NexusMods.IsEnabled = nexusModsSupportEnabled;
		_updater.SteamWorkshop.IsEnabled = workshopSupportEnabled;

		if (Settings.LogEnabled)
		{
			Window.ToggleLogging(true);
		}

		SetGamePathways(Settings.GameDataPath, Settings.DocumentsFolderPathOverride);

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

	public void CheckForWorkshopModUpdates(CancellationToken token)
	{
		ModUpdatesViewData.Clear();

		int count = 0;
		foreach (var workshopMod in WorkshopMods)
		{
			if (token.IsCancellationRequested)
			{
				break;
			}
			if (TryGetMod(workshopMod.UUID, out var pakMod))
			{
				pakMod.WorkshopData.ModId = workshopMod.WorkshopData.ModId;
				if (!pakMod.IsEditorMod)
				{
					if (!File.Exists(pakMod.FilePath) || workshopMod.Version > pakMod.Version || workshopMod.IsNewerThan(pakMod))
					{
						if (workshopMod.Version.VersionInt > pakMod.Version.VersionInt)
						{
							DivinityApp.Log($"Update available for ({pakMod.FileName}): Workshop({workshopMod.Version.VersionInt}|{pakMod.Version.Version})({workshopMod.Version.Version}) > Local({pakMod.Version.VersionInt}|{pakMod.Version.Version})");
						}
						else
						{
							DivinityApp.Log($"Update available for ({pakMod.FileName}): Workshop({workshopMod.LastModified}) > Local({pakMod.LastModified})");
						}

						ModUpdatesViewData.Add(new DivinityModUpdateData()
						{
							Mod = pakMod,
							DownloadData = new ModDownloadData()
							{
								DownloadPath = workshopMod.FilePath,
								DownloadPathType = ModDownloadPathType.FILE,
								DownloadSourceType = ModSourceType.STEAM,
								Version = workshopMod.Version.Version,
								Date = workshopMod.LastModified
							}
						});
						count++;
					}
				}
				else
				{
					DivinityApp.Log($"[***WARNING***] An editor mod has a local workshop pak! ({pakMod.Name}):");
					DivinityApp.Log($"--- Editor Version({pakMod.Version.Version}) | Workshop Version({workshopMod.Version.Version})");
				}
			}
			else
			{
				ModUpdatesViewData.Add(new DivinityModUpdateData()
				{
					Mod = workshopMod,
					DownloadData = new ModDownloadData()
					{
						DownloadPath = workshopMod.FilePath,
						DownloadPathType = ModDownloadPathType.FILE,
						DownloadSourceType = ModSourceType.STEAM,
						Version = workshopMod.Version.Version,
						Date = workshopMod.LastModified
					},
					//IsNewMod = true,
				});
				count++;
			}
		}
		if (count > 0)
		{
			ModUpdatesViewData.SelectAll(true);
			DivinityApp.Log($"'{count}' mod updates pending.");
		}
		IsRefreshingModUpdates = false;
	}

	private string GetLarianStudiosAppDataFolder()
	{
		if (Directory.Exists(PathwayData.AppDataGameFolder))
		{
			var parentDir = Directory.GetParent(PathwayData.AppDataGameFolder);
			if (parentDir != null)
			{
				return parentDir.FullName;
			}
		}
		string appDataFolder;
		if (!String.IsNullOrEmpty(Settings.DocumentsFolderPathOverride))
		{
			appDataFolder = Settings.DocumentsFolderPathOverride;
		}
		else
		{
			appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);
			if (String.IsNullOrEmpty(appDataFolder) || !Directory.Exists(appDataFolder))
			{
				var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
				if (Directory.Exists(userFolder))
				{
					appDataFolder = Path.Join(userFolder, "AppData", "Local", "Larian Studios");
				}
			}
			else
			{
				appDataFolder = Path.Join(appDataFolder, "Larian Studios");
			}
		}
		return appDataFolder;
	}

	private void SetGamePathways(string currentGameDataPath, string gameDataFolderOverride = "")
	{
		try
		{
			string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify);

			if (String.IsNullOrWhiteSpace(AppSettings.DefaultPathways.DocumentsGameFolder))
			{
				AppSettings.DefaultPathways.DocumentsGameFolder = "Larian Studios\\Baldur's Gate 3";
			}

			string gameDataFolder = Path.Join(localAppDataFolder, AppSettings.DefaultPathways.DocumentsGameFolder);

			if (!String.IsNullOrEmpty(gameDataFolderOverride) && Directory.Exists(gameDataFolderOverride))
			{
				gameDataFolder = gameDataFolderOverride;
				var parentDir = Directory.GetParent(gameDataFolder);
				if (parentDir != null)
				{
					localAppDataFolder = parentDir.FullName;
				}
			}
			else if (!Directory.Exists(gameDataFolder))
			{
				var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
				if (Directory.Exists(userFolder))
				{
					localAppDataFolder = Path.Join(userFolder, "AppData", "Local");
					gameDataFolder = Path.Join(localAppDataFolder, AppSettings.DefaultPathways.DocumentsGameFolder);
				}
			}

			string modPakFolder = Path.Join(gameDataFolder, "Mods");
			string gmCampaignsFolder = Path.Join(gameDataFolder, "GMCampaigns");
			string profileFolder = Path.Join(gameDataFolder, "PlayerProfiles");

			PathwayData.AppDataGameFolder = gameDataFolder;
			PathwayData.AppDataModsPath = modPakFolder;
			PathwayData.AppDataCampaignsPath = gmCampaignsFolder;
			PathwayData.AppDataProfilesPath = profileFolder;

			if (Directory.Exists(localAppDataFolder))
			{
				Directory.CreateDirectory(gameDataFolder);
				DivinityApp.Log($"Larian documents folder set to '{gameDataFolder}'.");

				if (!Directory.Exists(modPakFolder))
				{
					DivinityApp.Log($"No mods folder found at '{modPakFolder}'. Creating folder.");
					Directory.CreateDirectory(modPakFolder);
				}

				if (!Directory.Exists(gmCampaignsFolder))
				{
					DivinityApp.Log($"No GM campaigns folder found at '{gmCampaignsFolder}'. Creating folder.");
					Directory.CreateDirectory(gmCampaignsFolder);
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

			if (String.IsNullOrWhiteSpace(currentGameDataPath) || !Directory.Exists(currentGameDataPath))
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
							exePath = Path.Join(installPath, AppSettings.DefaultPathways.Steam.ExePath);
						}
						else
						{
							exePath = Path.Join(installPath, AppSettings.DefaultPathways.GOG.ExePath);
						}
						if (File.Exists(exePath))
						{
							Settings.GameExecutablePath = exePath.Replace("\\", "/");
							DivinityApp.Log($"Exe path set to '{exePath}'.");
						}
					}

					string gameDataPath = Path.Join(installPath, AppSettings.DefaultPathways.GameDataFolder).Replace("\\", "/");
					if (Directory.Exists(gameDataPath))
					{
						DivinityApp.Log($"Set game data path to '{gameDataPath}'.");
						Settings.GameDataPath = gameDataPath;
					}
					else
					{
						DivinityApp.Log($"Failed to find game data path at '{gameDataPath}'.");
					}
				}
			}
			else
			{
				string installPath = Path.GetFullPath(Path.Join(Settings.GameDataPath, @"..\..\"));
				PathwayData.InstallPath = installPath;
				if (!File.Exists(Settings.GameExecutablePath))
				{
					string exePath = "";
					if (!DivinityRegistryHelper.IsGOG)
					{
						exePath = Path.Join(installPath, AppSettings.DefaultPathways.Steam.ExePath);
					}
					else
					{
						exePath = Path.Join(installPath, AppSettings.DefaultPathways.GOG.ExePath);
					}
					if (File.Exists(exePath))
					{
						Settings.GameExecutablePath = exePath.Replace("\\", "/");
						DivinityApp.Log($"Exe path set to '{exePath}'.");
					}
				}
			}


			if (!Directory.Exists(Settings.GameDataPath) || !File.Exists(Settings.GameExecutablePath))
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
					var dataDirectory = Path.Join(dialog.SelectedPath, AppSettings.DefaultPathways.GameDataFolder);
					var exePath = Path.Join(dialog.SelectedPath, AppSettings.DefaultPathways.Steam.ExePath);
					if (!File.Exists(exePath))
					{
						exePath = Path.Join(dialog.SelectedPath, AppSettings.DefaultPathways.GOG.ExePath);
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

			if (AppSettings.Features.ScriptExtender && IsInitialized && !IsRefreshing)
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
		mods.Clear();
		foreach (var mod in loadedMods)
		{
			mod.SteamWorkshopEnabled = SteamWorkshopSupportEnabled;
			mod.NexusModsEnabled = NexusModsSupportEnabled;

			if (mod.IsLarianMod)
			{
				var existingIgnoredMod = DivinityApp.IgnoredMods.FirstOrDefault(x => x.UUID == mod.UUID);
				if (existingIgnoredMod != null && existingIgnoredMod != mod)
				{
					DivinityApp.IgnoredMods.Remove(existingIgnoredMod);
				}
				DivinityApp.IgnoredMods.Add(mod);
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
		}
	}

	private void MergeModLists(ref List<DivinityModData> finalMods, List<DivinityModData> newMods, bool preferNew = false)
	{
		foreach (var mod in newMods)
		{
			var existing = finalMods.FirstOrDefault(x => x.UUID == mod.UUID);
			if (existing != null)
			{
				if (preferNew || existing.Version.VersionInt < mod.Version.VersionInt)
				{
					finalMods.Replace(existing, mod);
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
		CancellationTokenSource token = new();
		if (last != null && last.IsCancellationRequested)
		{
			last.Dispose();
		}
		token.CancelAfter(delay);
		return token;
	}

	private async Task<TResult> RunTask<TResult>(Task<TResult> task, TResult defaultValue)
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

	public async Task<List<DivinityModData>> LoadModsAsync(double taskStepAmount = 0.1d)
	{
		List<DivinityModData> finalMods = new();
		ModLoadingResults modLoadingResults = null;
		//List<DivinityModData> projects = null;
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

			// No longer necessary since LSLib's VFS changes will pick up loose mods via LoadBuiltinModsAsync
			/*string modsDirectory = Path.Join(Settings.GameDataPath, "Mods");
			if (Directory.Exists(modsDirectory))
			{
				DivinityApp.Log($"Loading mod projects from '{modsDirectory}'.");
				await SetMainProgressTextAsync("Loading editor project mods...");
				cancelTokenSource = GetCancellationToken(30000);
				projects = await RunTask(DivinityModDataLoader.LoadEditorProjectsAsync(modsDirectory, cancelTokenSource.Token), null);
				cancelTokenSource = GetCancellationToken(int.MaxValue);
				await IncreaseMainProgressValueAsync(taskStepAmount);
			}*/
		}

		baseMods ??= new List<DivinityModData>();

		if (!GameDirectoryFound || baseMods.Count < DivinityApp.IgnoredMods.Count)
		{
			if (baseMods.Count == 0)
			{
				baseMods.AddRange(DivinityApp.IgnoredMods);
			}
			else
			{
				foreach (var mod in DivinityApp.IgnoredMods)
				{
					if (!baseMods.Any(x => x.UUID == mod.UUID)) baseMods.Add(mod);
				}
			}
		}

		if (Directory.Exists(PathwayData.AppDataModsPath))
		{
			DivinityApp.Log($"Loading mods from '{PathwayData.AppDataModsPath}'.");
			await SetMainProgressTextAsync("Loading mods from documents folder...");
			cancelTokenSource.CancelAfter(TimeSpan.FromMinutes(10));
			modLoadingResults = await RunTask(DivinityModDataLoader.LoadModPackageDataAsync(PathwayData.AppDataModsPath, cancelTokenSource.Token), null);
			cancelTokenSource = GetCancellationToken(int.MaxValue);
			await IncreaseMainProgressValueAsync(taskStepAmount);
		}

		if (baseMods != null) MergeModLists(ref finalMods, baseMods);
		if (modLoadingResults != null)
		{
			MergeModLists(ref finalMods, modLoadingResults.Mods);
			var dupeCount = modLoadingResults.Duplicates.Count;
			if (dupeCount > 0)
			{
				await Observable.Start(() =>
				{
					ShowAlert($"{dupeCount} duplicate mod(s) found", AlertType.Danger, 30);
					DeleteMods(modLoadingResults.Duplicates, true, modLoadingResults.Mods);
				}, RxApp.MainThreadScheduler);
			}
		}
		//if (projects != null) MergeModLists(ref finalMods, projects, true);

		finalMods = finalMods.OrderBy(m => m.Name).ToList();
		DivinityApp.Log($"Loaded '{finalMods.Count}' mods.");
		return finalMods;
	}

	public async Task<List<DivinityGameMasterCampaign>> LoadGameMasterCampaignsAsync(double taskStepAmount = 0.1d)
	{
		List<DivinityGameMasterCampaign> data = null;

		var cancelTokenSource = GetCancellationToken(int.MaxValue);

		if (!String.IsNullOrWhiteSpace(PathwayData.AppDataCampaignsPath) && Directory.Exists(PathwayData.AppDataCampaignsPath))
		{
			DivinityApp.Log($"Loading gamemaster campaigns from '{PathwayData.AppDataCampaignsPath}'.");
			await SetMainProgressTextAsync("Loading GM Campaigns from documents folder...");
			cancelTokenSource.CancelAfter(60000);
			data = DivinityModDataLoader.LoadGameMasterData(PathwayData.AppDataCampaignsPath, cancelTokenSource.Token);
			cancelTokenSource = GetCancellationToken(int.MaxValue);
			await IncreaseMainProgressValueAsync(taskStepAmount);
		}

		if (data != null)
		{
			data = data.OrderBy(m => m.Name).ToList();
			DivinityApp.Log($"Loaded '{data.Count}' GM campaigns.");
		}

		return data;
	}

	public bool ModIsAvailable(IDivinityModData divinityModData)
	{
		return mods.Items.Any(k => k.UUID == divinityModData.UUID)
			|| DivinityApp.IgnoredMods.Any(im => im.UUID == divinityModData.UUID)
			|| DivinityApp.IgnoredDependencyMods.Any(d => d.UUID == divinityModData.UUID);
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

	public void BuildModOrderList(int selectIndex = -1, string lastOrderName = "")
	{
		if (SelectedProfile != null)
		{
			IsLoadingOrder = true;

			List<DivinityMissingModData> missingMods = new();

			DivinityLoadOrder currentOrder = new() { Name = "Current", FilePath = Path.Join(SelectedProfile.FilePath, "modsettings.lsx"), IsModSettings = true };

			if (SelectedModOrder != null && SelectedModOrder.IsModSettings)
			{
				currentOrder.SetOrder(SelectedModOrder);
			}
			else
			{
				foreach (var uuid in SelectedProfile.ModOrder)
				{
					var activeModData = SelectedProfile.ActiveMods.FirstOrDefault(y => y.UUID == uuid);
					if (activeModData != null)
					{
						var mod = mods.Items.FirstOrDefault(m => m.UUID.Equals(uuid, StringComparison.OrdinalIgnoreCase));
						if (mod != null)
						{
							currentOrder.Add(mod);
						}
						else
						{
							var x = new DivinityMissingModData
							{
								Index = SelectedProfile.ModOrder.IndexOf(uuid),
								Name = activeModData.Name,
								UUID = activeModData.UUID
							};
							missingMods.Add(x);
						}
					}
					else
					{
						DivinityApp.Log($"UUID {uuid} is missing from the profile's active mod list.");
					}
				}
			}

			ModOrderList.Clear();
			ModOrderList.Add(currentOrder);
			if (SelectedProfile.SavedLoadOrder != null && !SelectedProfile.SavedLoadOrder.IsModSettings)
			{
				ModOrderList.Add(SelectedProfile.SavedLoadOrder);
			}
			else
			{
				SelectedProfile.SavedLoadOrder = currentOrder;
			}

			DivinityApp.Log($"Profile order: {String.Join(";", SelectedProfile.SavedLoadOrder.Order.Select(x => x.Name))}");

			ModOrderList.AddRange(SavedModOrderList);

			if (!String.IsNullOrEmpty(lastOrderName))
			{
				int lastOrderIndex = ModOrderList.IndexOf(ModOrderList.FirstOrDefault(x => x.Name == lastOrderName));
				if (lastOrderIndex != -1) selectIndex = lastOrderIndex;
			}

			RxApp.MainThreadScheduler.Schedule(_ =>
			{
				if (selectIndex != -1)
				{
					if (selectIndex >= ModOrderList.Count) selectIndex = ModOrderList.Count - 1;
					DivinityApp.Log($"Setting next order index to [{selectIndex}/{ModOrderList.Count - 1}].");
					try
					{
						SelectedModOrderIndex = selectIndex;
						var nextOrder = ModOrderList.ElementAtOrDefault(selectIndex);

						LoadModOrder(nextOrder, missingMods);

						/*if (nextOrder.IsModSettings && Settings.GameMasterModeEnabled && SelectedGameMasterCampaign != null)
						{
							LoadGameMasterCampaignModOrder(SelectedGameMasterCampaign);
						}
						*/

						Settings.LastOrder = nextOrder?.Name;
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error setting next load order:\n{ex}");
					}
				}
				IsLoadingOrder = false;
			});
		}
	}

	private async Task<ModuleInfo> TryGetMetaFromZipAsync(string filePath, CancellationToken token)
	{
		TempFile tempFile = null;
		try
		{
			using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
			await fileStream.ReadAsync(new byte[fileStream.Length], 0, (int)fileStream.Length);
			fileStream.Position = 0;

			using var archive = ArchiveFactory.Open(fileStream, _importReaderOptions);
			foreach (var file in archive.Entries)
			{
				if (token.IsCancellationRequested) return null;
				if (!file.IsDirectory)
				{
					if (file.Key.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
					{
						using var entryStream = file.OpenEntryStream();
						tempFile = await TempFile.CreateAsync(String.Join("\\", filePath, file.Key), entryStream, token);
						var meta = DivinityModDataLoader.TryGetMetaFromPakFileStream(tempFile.Stream, filePath, token);
						if (meta == null)
						{
							var pakName = Path.GetFileNameWithoutExtension(file.Key);
							var overrideMod = mods.Lookup(pakName);
							if (overrideMod.HasValue)
							{
								return new ModuleInfo
								{
									UUID = pakName,
								};
							}
						}
						else
						{
							return meta;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error reading zip:\n{ex}");
		}
		finally
		{
			tempFile?.Dispose();
		}

		return null;
	}

	private async Task<ModuleInfo> TryGetMetaFromCompressedFileAsync(string filePath, string extension, CancellationToken token)
	{
		ModuleInfo result = null;
		using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
		{
			await fileStream.ReadAsync(new byte[fileStream.Length], 0, (int)fileStream.Length);
			fileStream.Position = 0;

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
					tempFile = await TempFile.CreateAsync(filePath, decompressionStream, token);
					result = DivinityModDataLoader.TryGetMetaFromPakFileStream(tempFile.Stream, filePath, token);
					if (result == null)
					{
						var pakName = Path.GetFileNameWithoutExtension(filePath);
						var overrideMod = mods.Lookup(pakName);
						if (overrideMod.HasValue)
						{
							result = new ModuleInfo
							{
								UUID = pakName,
							};
						}
					}
				}
			}
			finally
			{
				decompressionStream?.Dispose();
				tempFile?.Dispose();
			}
		}
		return result;
	}

	private async Task<bool> FetchNexusModsIdFromFilesAsync(List<string> files, ImportOperationResults results, CancellationToken token)
	{
		foreach (var filePath in files)
		{
			try
			{
				if (token.IsCancellationRequested) break;

				var ext = Path.GetExtension(filePath).ToLower();

				var isArchive = _archiveFormats.Contains(ext, StringComparer.OrdinalIgnoreCase);
				var isCompressedFile = !isArchive && _compressedFormats.Contains(ext, StringComparer.OrdinalIgnoreCase);

				if (isArchive || isCompressedFile)
				{
					var info = NexusModFileVersionData.FromFilePath(filePath);
					if (info.Success)
					{
						ModuleInfo meta = null;
						if (isArchive)
						{
							meta = await TryGetMetaFromZipAsync(filePath, token);
						}
						else if (isCompressedFile)
						{
							meta = await TryGetMetaFromCompressedFileAsync(filePath, ext, token);
						}

						if (meta != null)
						{
							var mod = mods.Lookup(meta.UUID);
							if (mod.HasValue)
							{
								mod.Value.NexusModsData.SetModVersion(info);
								results.Mods.Add(mod.Value);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error fetching info:\n{ex}");
				results.AddError(filePath, ex);
			}
		}

		if (results.Success)
		{
			DivinityApp.Log($"Updated NexusMods mod ids for ({results.Mods.Count}) mod(s).");
			await _updater.NexusMods.Update(results.Mods, token);
			await _updater.NexusMods.SaveCacheAsync(false, Version, token);
		}
		return results.Success;
	}

	private void OpenModIdsImportDialog()
	{
		//Filter = $"All formats (*.pak;{_archiveFormatsStr};{_compressedFormatsStr})|*.pak;{_archiveFormatsStr};{_compressedFormatsStr}|Mod package (*.pak)|*.pak|Archive file ({_archiveFormatsStr})|{_archiveFormatsStr}|Compressed file ({_compressedFormatsStr})|{_compressedFormatsStr}|All files (*.*)|*.*",
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".zip",
			Filter = $"All formats ({_archiveFormatsStr};{_compressedFormatsStr})|{_archiveFormatsStr};{_compressedFormatsStr}|Archive file ({_archiveFormatsStr})|{_archiveFormatsStr}|Compressed file ({_compressedFormatsStr})|{_compressedFormatsStr}|All files (*.*)|*.*",
			Title = "Import NexusMods ModId(s) from Archive(s)...",
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

			var files = dialog.FileNames.ToList();

			if (!MainProgressIsActive)
			{
				MainProgressTitle = "Parsing files for NexusMods ModIds...";
				MainProgressWorkText = "";
				MainProgressValue = 0d;
				MainProgressIsActive = true;
				IsRefreshing = true;
				var result = new ImportOperationResults()
				{
					TotalFiles = files.Count()
				};

				RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, t) =>
				{
					MainProgressToken = new CancellationTokenSource();

					await FetchNexusModsIdFromFilesAsync(files, result, MainProgressToken.Token);

					RxApp.MainThreadScheduler.Schedule(_ =>
					{
						IsRefreshing = false;
						OnMainProgressComplete();

						if (result.Errors.Count > 0)
						{
							var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
							var errorOutputPath = DivinityApp.GetAppDirectory("_Logs", $"ImportNexusModsModIds_{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}_Errors.log");
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
								ShowAlert($"Successfully imported NexusMods ids for {total} mods", AlertType.Success, 20);
							}
							else if (total == 1)
							{
								ShowAlert($"Successfully imported the NexusMods id for '{result.Mods.First().Name}'", AlertType.Success, 20);
							}
							else
							{
								ShowAlert("No NexusMods ids found", AlertType.Success, 20);
							}
						}
						else
						{
							if (total == 0)
							{
								ShowAlert("No NexusMods ids found. Does the .zip name contain an id, with a .pak file inside?", AlertType.Warning, 60);
							}
							else if (result.Errors.Count > 0)
							{
								ShowAlert($"Encountered some errors fetching ids - Check the log", AlertType.Danger, 60);
							}
						}
					});
					return Disposable.Empty;
				});
			}
		}
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

	private async Task<ImportOperationResults> AddModFromFile(Dictionary<string, DivinityModData> builtinMods, ImportOperationResults taskResult, string filePath, CancellationToken token, bool toActiveList = false)
	{
		var ext = Path.GetExtension(filePath).ToLower();
		if (ext.Equals(".pak", StringComparison.OrdinalIgnoreCase))
		{
			var outputFilePath = Path.Join(PathwayData.AppDataModsPath, Path.GetFileName(filePath));
			try
			{
				taskResult.TotalPaks++;

				await FileUtils.CopyFileAsync(filePath, outputFilePath, token);

				if (File.Exists(outputFilePath))
				{
					var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, builtinMods, token);
					if (mod != null)
					{
						taskResult.Mods.Add(mod);
						await Observable.Start(() =>
						{
							AddImportedMod(mod, toActiveList);
						}, RxApp.MainThreadScheduler);
					}
				}
			}
			catch (System.IO.IOException ex)
			{
				DivinityApp.Log($"File may be in use by another process:\n{ex}");
				ShowAlert($"Failed to copy file '{Path.GetFileName(filePath)} - It may be locked by another process'", AlertType.Danger);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading file ({filePath}):\n{ex}");
			}
		}
		else
		{
			var importOptions = new ImportParameters(filePath, PathwayData.AppDataModsPath, token, taskResult)
			{
				BuiltinMods = builtinMods,
				OnlyMods = true,
				Extension = ext,
				ReportProgress = amount => IncreaseMainProgressValue(amount),
				ShowAlert = (msg, t, timeout) => RxApp.MainThreadScheduler.Schedule(() => ShowAlert(msg, t, timeout))
			};

			if (_archiveFormats.Contains(ext, StringComparer.OrdinalIgnoreCase))
			{
				await ImportUtils.ImportArchiveAsync(importOptions);
			}
			else if (_compressedFormats.Contains(ext, StringComparer.OrdinalIgnoreCase))
			{
				await ImportUtils.ImportCompressedFileAsync(importOptions);
			}

			if (importOptions.Result.Mods.Count > 0)
			{
				await Observable.Start(() =>
				{
					foreach (var mod in importOptions.Result.Mods)
					{
						AddImportedMod(mod, toActiveList);
					}
				}, RxApp.MainThreadScheduler);
			}
		}

		return taskResult;
	}

	public void ImportMods(IEnumerable<string> files, bool toActiveList = false)
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
				var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
				MainProgressToken = new CancellationTokenSource();
				foreach (var f in files)
				{
					await AddModFromFile(builtinMods, result, f, MainProgressToken.Token, toActiveList);
				}

				if (_updater.NexusMods.IsEnabled && result.Mods.Count > 0 && result.Mods.Any(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START))
				{
					await _updater.NexusMods.Update(result.Mods, MainProgressToken.Token);
					await _updater.NexusMods.SaveCacheAsync(false, Version, MainProgressToken.Token);
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

	private static readonly List<string> _archiveFormats = new() { ".7z", ".7zip", ".gzip", ".rar", ".tar", ".tar.gz", ".zip" };
	private static readonly List<string> _compressedFormats = new() { ".bz2", ".xz", ".zst" };
	private static readonly string _archiveFormatsStr = String.Join(";", _archiveFormats.Select(x => "*" + x));
	private static readonly string _compressedFormatsStr = String.Join(";", _compressedFormats.Select(x => "*" + x));

	public static bool IsImportableFile(string ext)
	{
		return ext == ".pak" || _archiveFormats.Contains(ext) || _compressedFormats.Contains(ext);
	}

	private void AddNewModOrder(DivinityLoadOrder newOrder = null)
	{
		var lastIndex = SelectedModOrderIndex;
		var lastOrders = ModOrderList.ToList();

		var nextOrders = new List<DivinityLoadOrder>
		{
			SelectedProfile.SavedLoadOrder
		};
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
				newOrder.FilePath = Path.Join(Settings.LoadOrderPath, DivinityModDataLoader.MakeSafeFilename(Path.Join(newOrder.Name + ".json"), '_'));
			}
			SavedModOrderList.Add(newOrder);
			BuildModOrderList(SavedModOrderList.Count); // +1 due to Current being index 0
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

	public bool LoadModOrder(DivinityLoadOrder order, List<DivinityMissingModData> missingModsFromProfileOrder = null)
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
		Dictionary<string, DivinityMissingModData> missingMods = new();
		if (missingModsFromProfileOrder != null && missingModsFromProfileOrder.Count > 0)
		{
			missingModsFromProfileOrder.ForEach(x => missingMods[x.UUID] = x);
			DivinityApp.Log($"Missing mods (from profile): {String.Join(";", missingModsFromProfileOrder)}");
		}

		var loadOrderIndex = 0;

		for (int i = 0; i < loadFrom.Count; i++)
		{
			var entry = loadFrom[i];
			if (!DivinityModDataLoader.IgnoreMod(entry.UUID))
			{
				var modResult = mods.Lookup(entry.UUID);
				if (!modResult.HasValue)
				{
					missingMods[entry.UUID] = new DivinityMissingModData
					{
						Index = i,
						Name = entry.Name,
						UUID = entry.UUID
					};
					entry.Missing = true;
				}
				else
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

					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (!String.IsNullOrWhiteSpace(dependency.UUID) && !DivinityModDataLoader.IgnoreMod(dependency.UUID) && !ModExists(dependency.UUID))
							{
								missingMods[dependency.UUID] = new DivinityMissingModData
								{
									Index = -1,
									Name = dependency.Name,
									UUID = dependency.UUID,
									Dependency = true
								};
							}
						}
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
		OnFilterTextChanged(OverrideModsFilterText, ForceLoadedMods);

		if (missingMods.Count > 0)
		{
			var orderedMissingMods = missingMods.Values.OrderBy(x => x.Index).ToList();

			DivinityApp.Log($"Missing mods: {String.Join(";", orderedMissingMods)}");
			if (Settings?.DisableMissingModWarnings == true)
			{
				DivinityApp.Log("Skipping missing mod display.");
			}
			else
			{
				View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
				View.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
				View.MainWindowMessageBox_OK.ShowMessageBox(String.Join("\n", orderedMissingMods),
					"Missing Mods in Load Order", MessageBoxButton.OK);
			}
		}

		OrderJustLoaded = true;

		IsLoadingOrder = false;
		return true;
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
		if (mods.Count > 0)
		{
			foreach (var mod in mods.Items)
			{
				UpdateModExtenderStatus(mod);
			}
		}
	}

	public async Task<Unit> SetMainProgressTextAsync(string text)
	{
		return await Observable.Start(() =>
		{
			MainProgressWorkText = text;
			return Unit.Default;
		}, RxApp.MainThreadScheduler);
	}

	public async Task<Unit> StartMainProgressAsync(string title)
	{
		if (!MainProgressIsActive)
		{
			await Observable.Start(() =>
			{
				MainProgressTitle = title;
				MainProgressWorkText = "";
				MainProgressValue = 0d;
				MainProgressIsActive = true;
				IsRefreshing = true;
			}, RxApp.MainThreadScheduler);
		}
		return Unit.Default;
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
			return UserMods.Where(x => CanUpdateMod(x, now, minUpdateTime, settingsService)).ToList();
		}
		return UserMods;
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
					var mod = mods.Lookup(kvp.Key);
					if (mod.HasValue)
					{
						var updateData = new DivinityModUpdateData()
						{
							Mod = mod.Value,
							DownloadData = new ModDownloadData()
							{
								DownloadPath = kvp.Value.BrowserDownloadLink,
								DownloadPathType = ModDownloadPathType.URL,
								DownloadSourceType = ModSourceType.GITHUB,
								Version = kvp.Value.Version,
								Date = kvp.Value.Date
							},
						};
						ModUpdatesViewData.Add(updateData);
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
					ModUpdatesViewData.Add(updateData);
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
		var results = await _updater.GetSteamWorkshopUpdatesAsync(Settings, GetUpdateableMods(), Version, token);
		await sch.Yield(token);
		if (!token.IsCancellationRequested && results.Count > 0)
		{
			await Observable.Start(() =>
			{
				workshopMods.AddOrUpdate(results.Values);
				DivinityApp.Log($"Loaded '{workshopMods.Count}' workshop mods from '{Settings.WorkshopPath}'.");
				if (!token.IsCancellationRequested)
				{
					CheckForWorkshopModUpdates(token);
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
			await RefreshGitHubModsUpdatesBackgroundAsync(sch, token);
			await RefreshNexusModsUpdatesBackgroundAsync(sch, token);
			await RefreshSteamWorkshopUpdatesBackgroundAsync(sch, token);

			IsRefreshingModUpdates = false;
		});
	}

	private async Task<Unit> RefreshAsync(IScheduler ctrl, CancellationToken token)
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

		if (Directory.Exists(PathwayData.AppDataGameFolder))
		{
			DivinityApp.Log("Loading mods...");
			await SetMainProgressTextAsync("Loading mods...");
			var loadedMods = await LoadModsAsync(taskStepAmount);
			await IncreaseMainProgressValueAsync(taskStepAmount);

			DivinityApp.Log("Loading profiles...");
			await SetMainProgressTextAsync("Loading profiles...");
			var loadedProfiles = await LoadProfilesAsync();
			await IncreaseMainProgressValueAsync(taskStepAmount);

			if (String.IsNullOrEmpty(selectedProfileUUID) && (loadedProfiles != null && loadedProfiles.Count > 0))
			{
				DivinityApp.Log("Loading current profile...");
				await SetMainProgressTextAsync("Loading current profile...");
				selectedProfileUUID = await DivinityModDataLoader.GetSelectedProfileUUIDAsync(PathwayData.AppDataProfilesPath);
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

			//await SetMainProgressTextAsync("Loading GM Campaigns...");
			//var loadedGMCampaigns = await LoadGameMasterCampaignsAsync(taskStepAmount);
			//await IncreaseMainProgressValueAsync(taskStepAmount);

			DivinityApp.Log("Loading external load orders...");
			await SetMainProgressTextAsync("Loading external load orders...");
			var savedModOrderList = await RunTask(LoadExternalLoadOrdersAsync(), new List<DivinityLoadOrder>());
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
				//SetLoadedGMCampaigns(loadedGMCampaigns);

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

				if (lastActiveOrder != null && lastActiveOrder.Count > 0)
				{
					SelectedModOrder?.SetOrder(lastActiveOrder);
				}
				BuildModOrderList(0, lastOrderName);
				MainProgressValue += taskStepAmount;

				if (!GameDirectoryFound)
				{
					ShowAlert("Game Data folder is not valid. Please set it in the preferences window and refresh", AlertType.Danger);
					App.WM.Settings.Toggle(true);
				}
			}, RxApp.MainThreadScheduler);

			await IncreaseMainProgressValueAsync(taskStepAmount);
			await SetMainProgressTextAsync("Finishing up...");
		}
		else
		{
			DivinityApp.Log($"[*ERROR*] Larian documents folder not found!");
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
						if (nextAdventureMod.UUID == DivinityApp.GAMEMASTER_UUID)
						{
							Settings.GameMasterModeEnabled = true;
						}
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

			View.ModLayout.RestoreLayout();

			OnMainProgressComplete();
			OnRefreshed?.Invoke(this, new EventArgs());

			IsRefreshing = false;
			IsLoadingOrder = false;
			IsInitialized = true;

			ApplyUserModConfig();

			if (AppSettings.Features.ScriptExtender)
			{
				LoadExtenderSettingsBackground();
			}

			RefreshModUpdatesCommand.Execute().Subscribe();
		}, RxApp.MainThreadScheduler);
		return Unit.Default;
	}

	private async Task<List<DivinityLoadOrder>> LoadExternalLoadOrdersAsync()
	{
		try
		{
			string loadOrderDirectory = Settings.LoadOrderPath;
			if (String.IsNullOrWhiteSpace(loadOrderDirectory))
			{
				loadOrderDirectory = DivinityApp.GetAppDirectory("Orders");
			}
			else if (Uri.IsWellFormedUriString(loadOrderDirectory, UriKind.Relative))
			{
				loadOrderDirectory = Path.GetFullPath(loadOrderDirectory);
			}

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
			string outputDirectory = Settings.LoadOrderPath;

			if (String.IsNullOrWhiteSpace(outputDirectory))
			{
				outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
			}

			if (!Directory.Exists(outputDirectory))
			{
				Directory.CreateDirectory(outputDirectory);
			}

			string outputPath = SelectedModOrder.FilePath;
			string outputName = DivinityModDataLoader.MakeSafeFilename(Path.Join(SelectedModOrder.Name + ".json"), '_');

			if (String.IsNullOrWhiteSpace(SelectedModOrder.FilePath))
			{
				var ordersDir = Settings.LoadOrderPath;
				//Relative path
				if (Settings.LoadOrderPath.IndexOf(@":\") == -1)
				{
					ordersDir = DivinityApp.GetAppDirectory(Settings.LoadOrderPath);
					if (!Directory.Exists(ordersDir)) Directory.CreateDirectory(ordersDir);
				}
				SelectedModOrder.FilePath = Path.Join(ordersDir, outputName);
				outputPath = SelectedModOrder.FilePath;
			}

			try
			{
				if (SelectedModOrder.IsModSettings)
				{
					//When saving the "Current" order, write this to modsettings.lsx instead of a json file.
					result = await ExportLoadOrderAsync();
					outputPath = Path.Join(SelectedProfile.FilePath, "modsettings.lsx");
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
		var ordersDir = Settings.LoadOrderPath;
		//Relative path
		if (Settings.LoadOrderPath.IndexOf(@":\") == -1)
		{
			ordersDir = DivinityApp.GetAppDirectory(Settings.LoadOrderPath);
			if (!Directory.Exists(ordersDir)) Directory.CreateDirectory(ordersDir);
		}
		var startDirectory = GetInitialStartingDirectory(ordersDir);

		var dialog = new SaveFileDialog
		{
			AddExtension = true,
			DefaultExt = ".json",
			Filter = "JSON file (*.json)|*.json",
			InitialDirectory = startDirectory
		};

		string outputPath = Path.Join(SelectedModOrder.Name + ".json");
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
				int updatedOrderIndex = -1;
				for (var i = 0; i < ModOrderList.Count; i++)
				{
					var order = ModOrderList[i];
					if (order.FilePath == outputPath)
					{
						updatedOrderIndex = i;
						order.SetOrder(tempOrder);
						updatedOrder = true;
						DivinityApp.Log($"Updated saved order '{order.Name}' from '{modOrderName}'");
					}
				}
				if (!updatedOrder)
				{
					AddNewModOrder(tempOrder);
				}
				else
				{
					SelectedModOrderIndex = updatedOrderIndex;
					LoadModOrder(tempOrder);
				}
			}
			else
			{
				ShowAlert($"Failed to save mod load order to '{outputPath}'", AlertType.Danger);
			}
		}
	}

	private void DisplayMissingMods(DivinityLoadOrder order = null)
	{
		bool displayExtenderModWarning = false;

		order ??= SelectedModOrder;
		if (order != null && Settings?.DisableMissingModWarnings != true)
		{
			List<DivinityMissingModData> missingMods = new();

			for (int i = 0; i < order.Order.Count; i++)
			{
				var entry = order.Order[i];
				if (TryGetMod(entry.UUID, out var mod))
				{
					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (!DivinityModDataLoader.IgnoreMod(dependency.UUID) && !mods.Items.Any(x => x.UUID == dependency.UUID) &&
								!missingMods.Any(x => x.UUID == dependency.UUID))
							{
								var x = new DivinityMissingModData
								{
									Index = -1,
									Name = dependency.Name,
									UUID = dependency.UUID,
									Dependency = true
								};
								missingMods.Add(x);
							}
						}
					}
				}
				else if (!DivinityModDataLoader.IgnoreMod(entry.UUID))
				{
					var x = new DivinityMissingModData
					{
						Index = i,
						Name = entry.Name,
						UUID = entry.UUID
					};
					missingMods.Add(x);
					entry.Missing = true;
				}
			}

			if (missingMods.Count > 0)
			{
				View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
				View.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
				View.MainWindowMessageBox_OK.ShowMessageBox(String.Join("\n", missingMods.OrderBy(x => x.Index)),
					"Missing Mods in Load Order", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
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

		if (Settings?.DisableMissingModWarnings != true && displayExtenderModWarning && AppSettings.Features.ScriptExtender)
		{
			//DivinityApp.LogMessage($"Mod Order: {String.Join("\n", order.Order.Select(x => x.Name))}");
			DivinityApp.Log("Checking mods for extender requirements.");
			List<DivinityMissingModData> extenderRequiredMods = new();
			for (int i = 0; i < order.Order.Count; i++)
			{
				var entry = order.Order[i];
				var mod = ActiveMods.FirstOrDefault(m => m.UUID == entry.UUID);
				if (mod != null)
				{
					if (mod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
					{
						extenderRequiredMods.Add(new DivinityMissingModData
						{
							Index = mod.Index,
							Name = mod.DisplayName,
							UUID = mod.UUID,
							Dependency = false
						});

						if (mod.Dependencies.Count > 0)
						{
							foreach (var dependency in mod.Dependencies.Items)
							{
								if (TryGetMod(dependency.UUID, out var dependencyMod))
								{
									// Dependencies not in the order that require the extender
									if (dependencyMod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
									{
										extenderRequiredMods.Add(new DivinityMissingModData
										{
											Index = mod.Index - 1,
											Name = dependencyMod.DisplayName,
											UUID = dependencyMod.UUID,
											Dependency = true
										});
									}
								}
							}
						}
					}
				}
			}

			if (extenderRequiredMods.Count > 0)
			{
				DivinityApp.Log("Displaying mods that require the extender.");
				View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
				View.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
				View.MainWindowMessageBox_OK.ShowMessageBox("Functionality may be limited without the Script Extender.\n" + String.Join("\n", extenderRequiredMods.OrderBy(x => x.Index)),
					"Mods Require the Script Extender", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
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

	private async Task<bool> ExportLoadOrderAsync()
	{
		if (!Settings.GameMasterModeEnabled)
		{
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				string outputPath = Path.Join(SelectedProfile.FilePath, "modsettings.lsx");
				var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, mods.Items, Settings.AutoAddDependenciesWhenExporting, SelectedAdventureMod);
				var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.FilePath, finalOrder);

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

						List<string> orderList = new();
						if (SelectedAdventureMod != null) orderList.Add(SelectedAdventureMod.UUID);
						orderList.AddRange(SelectedModOrder.Order.Select(x => x.UUID));

						SelectedProfile.ModOrder.Clear();
						SelectedProfile.ModOrder.AddRange(orderList);
						SelectedProfile.ActiveMods.Clear();
						SelectedProfile.ActiveMods.AddRange(orderList.Select(x => ProfileActiveModDataFromUUID(x)));
						DisplayMissingMods(SelectedModOrder);
					}, RxApp.MainThreadScheduler);
					return true;
				}
				else
				{
					await Observable.Start(() =>
					{
						string msg = $"Problem exporting load order to '{outputPath}'. Is the file locked?";
						ShowAlert(msg, AlertType.Danger);
						View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
						View.MainWindowMessageBox_OK.Closed += this.MainWindowMessageBox_Closed_ResetColor;
						View.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
					}, RxApp.MainThreadScheduler);
				}
			}
			else
			{
				await Observable.Start(() =>
				{
					ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
				}, RxApp.MainThreadScheduler);
			}
		}
		else
		{
			if (SelectedGameMasterCampaign != null)
			{
				if (TryGetMod(DivinityApp.GAMEMASTER_UUID, out var gmAdventureMod))
				{
					var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, mods.Items, Settings.AutoAddDependenciesWhenExporting);
					if (SelectedGameMasterCampaign.Export(finalOrder))
					{
						// Need to still write to modsettings.lsx
						finalOrder.Insert(0, gmAdventureMod);
						await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.FilePath, finalOrder);

						await Observable.Start(() =>
						{
							ShowAlert($"Exported load order to '{SelectedGameMasterCampaign.FilePath}'", AlertType.Success, 15);

							if (DivinityModDataLoader.ExportedSelectedProfile(PathwayData.AppDataProfilesPath, SelectedProfile.UUID))
							{
								DivinityApp.Log($"Set active profile to '{SelectedProfile.Name}'");
							}
							else
							{
								DivinityApp.Log($"Could not set active profile to '{SelectedProfile.Name}'");
							}

							//Update the campaign's saved dependencies
							SelectedGameMasterCampaign.Dependencies.Clear();
							SelectedGameMasterCampaign.Dependencies.AddRange(finalOrder.Select(x => DivinityModDependencyData.FromModData(x)));

							List<string> orderList = new();
							if (SelectedAdventureMod != null) orderList.Add(SelectedAdventureMod.UUID);
							orderList.AddRange(SelectedModOrder.Order.Select(x => x.UUID));

							SelectedProfile.ModOrder.Clear();
							SelectedProfile.ModOrder.AddRange(orderList);
							SelectedProfile.ActiveMods.Clear();
							SelectedProfile.ActiveMods.AddRange(orderList.Select(x => ProfileActiveModDataFromUUID(x)));
							DisplayMissingMods(SelectedModOrder);

						}, RxApp.MainThreadScheduler);
						return true;
					}
					else
					{
						await Observable.Start(() =>
						{
							string msg = $"Problem exporting load order to '{SelectedGameMasterCampaign.FilePath}'";
							ShowAlert(msg, AlertType.Danger);
							this.View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
							this.View.MainWindowMessageBox_OK.Closed += this.MainWindowMessageBox_Closed_ResetColor;
							this.View.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);

						}, RxApp.MainThreadScheduler);
					}
				}
			}
			else
			{
				await Observable.Start(() =>
				{
					ShowAlert("SelectedGameMasterCampaign is null! Failed to export mod order", AlertType.Danger);
				}, RxApp.MainThreadScheduler);
			}
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
				var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
				MainProgressToken = new CancellationTokenSource();

				var importOptions = new ImportParameters(dialog.FileName, PathwayData.AppDataModsPath, MainProgressToken.Token, result)
				{
					BuiltinMods = builtinMods,
					OnlyMods = false,
					ReportProgress = amount => IncreaseMainProgressValue(amount),
					ShowAlert = (msg, t, timeout) => RxApp.MainThreadScheduler.Schedule(() => ShowAlert(msg, t, timeout))
				};

				await ImportUtils.ImportArchiveAsync(importOptions);

				if (importOptions.Result.Mods.Count > 0)
				{
					await Observable.Start(() =>
					{
						foreach (var mod in importOptions.Result.Mods)
						{
							AddImportedMod(mod, false);
						}
					}, RxApp.MainThreadScheduler);
				}

				if (result.Mods.Count > 0 && result.Mods.Any(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START))
				{
					await Services.Get<IModUpdaterService>().NexusMods.Update(result.Mods, MainProgressToken.Token);
				}
				await ctrl.Yield();
				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					OnMainProgressComplete();

					if (result.Errors.Count > 0)
					{
						var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
						var errorOutputPath = DivinityApp.GetAppDirectory("_Logs", $"ImportOrderFromArchive_{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}_Errors.log");
						var logsDir = Path.GetDirectoryName(errorOutputPath);
						if (!Directory.Exists(logsDir))
						{
							Directory.CreateDirectory(logsDir);
						}
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

	private void AddImportedMod(DivinityModData mod, bool toActiveList = false)
	{
		mod.SteamWorkshopEnabled = SteamWorkshopSupportEnabled;
		mod.NexusModsEnabled = NexusModsSupportEnabled;

		if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
		{
			mods.AddOrUpdate(mod);
			DivinityApp.Log($"Imported Override Mod: {mod}");
			return;
		}
		var existingMod = mods.Items.FirstOrDefault(x => x.UUID == mod.UUID);
		if (existingMod != null)
		{
			mod.IsSelected = existingMod.IsSelected;
			if (existingMod.IsActive)
			{
				mod.Index = existingMod.Index;
				ActiveMods.ReplaceOrAdd(existingMod, mod);
			}
			else
			{
				if (toActiveList)
				{
					InactiveMods.Remove(existingMod);
					mod.Index = ActiveMods.Count;
					ActiveMods.Add(mod);
				}
				else
				{
					InactiveMods.ReplaceOrAdd(existingMod, mod);
				}
			}
		}
		else
		{
			if (toActiveList)
			{
				mod.Index = ActiveMods.Count;
				ActiveMods.Add(mod);
			}
			else
			{
				InactiveMods.Add(mod);
			}
		}
		mods.AddOrUpdate(mod);
		UpdateModExtenderStatus(mod);
		DivinityApp.Log($"Imported Mod: {mod}");
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

	private async Task<bool> ExportLoadOrderToArchiveAsync(string outputPath, CancellationToken token)
	{
		var success = false;
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
			var gameDataFolder = Path.GetFullPath(Settings.GameDataPath);
			var tempDir = DivinityApp.GetAppDirectory("Temp");
			Directory.CreateDirectory(tempDir);

			if (String.IsNullOrEmpty(outputPath))
			{
				var baseOrderName = SelectedModOrder.Name;
				if (SelectedModOrder.IsModSettings)
				{
					baseOrderName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}";
				}
				var outputDir = DivinityApp.GetAppDirectory("Export");
				Directory.CreateDirectory(outputDir);
				outputPath = Path.Join(outputDir, $"{baseOrderName}-{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.zip");
			}

			var modPaks = new List<DivinityModData>(Mods.Where(x => SelectedModOrder.Order.Any(o => o.UUID == x.UUID)));
			modPaks.AddRange(ForceLoadedMods.Where(x => !x.IsForceLoadedMergedMod));

			var incrementProgress = 1d / modPaks.Count;

			try
			{
				using var stream = File.OpenWrite(outputPath);
				using var zipWriter = WriterFactory.Open(stream, ArchiveType.Zip, _exportWriterOptions);

				var orderFileName = DivinityModDataLoader.MakeSafeFilename(Path.Join(SelectedModOrder.Name + ".json"), '_');
				var contents = JsonConvert.SerializeObject(SelectedModOrder, Newtonsoft.Json.Formatting.Indented);

				using var ms = new System.IO.MemoryStream();
				using var swriter = new System.IO.StreamWriter(ms);

				await swriter.WriteAsync(contents);
				swriter.Flush();
				ms.Position = 0;
				zipWriter.Write(orderFileName, ms);

				foreach (var mod in modPaks)
				{
					if (token.IsCancellationRequested) return false;
					if (!mod.IsEditorMod)
					{
						var fileName = Path.GetFileName(mod.FilePath);
						await WriteZipAsync(zipWriter, fileName, mod.FilePath, token);
					}
					else
					{
						var outputPackage = Path.ChangeExtension(Path.Join(tempDir, mod.Folder), "pak");
						//Imported Classic Projects
						if (!mod.Folder.Contains(mod.UUID))
						{
							outputPackage = Path.ChangeExtension(Path.Join(tempDir, mod.Folder + "_" + mod.UUID), "pak");
						}

						var sourceFolders = new List<string>();

						var modsFolder = Path.Join(gameDataFolder, $"Mods/{mod.Folder}");
						var publicFolder = Path.Join(gameDataFolder, $"Public/{mod.Folder}");

						if (Directory.Exists(modsFolder)) sourceFolders.Add(modsFolder);
						if (Directory.Exists(publicFolder)) sourceFolders.Add(publicFolder);

						DivinityApp.Log($"Creating package for editor mod '{mod.Name}' - '{outputPackage}'.");

						if (await FileUtils.CreatePackageAsync(gameDataFolder, sourceFolders, outputPackage, token, FileUtils.IgnoredPackageFiles))
						{
							var fileName = Path.GetFileName(outputPackage);
							await WriteZipAsync(zipWriter, fileName, outputPackage, token);
							File.Delete(outputPackage);
						}
					}

					RxApp.MainThreadScheduler.Schedule(_ => MainProgressValue += incrementProgress);
				}

				RxApp.MainThreadScheduler.Schedule(() =>
				{
					ShowAlert($"Exported load order to '{outputPath}'", AlertType.Success, 15);
					var dir = Path.GetFullPath(Path.GetDirectoryName(outputPath));
					if (Directory.Exists(dir))
					{
						FileUtils.TryOpenPath(dir);
					}
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
		return $"{index}\t{mod.Name}\t{mod.AuthorDisplayName}\t{mod.OutputPakName}\t{String.Join(", ", mod.Tags)}\t{String.Join(", ", mod.Dependencies.Items.Select(y => y.Name))}\t{urls}";
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
			string profilePath = Path.GetFullPath(Path.Join(SelectedProfile.FilePath, "Savegames"));
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
			PathwayData.LastSaveFilePath = Path.GetDirectoryName(dialog.FileName);
			DivinityApp.Log($"Loading order from '{dialog.FileName}'.");
			var newOrder = DivinityModDataLoader.GetLoadOrderFromSave(dialog.FileName, Settings.LoadOrderPath);
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
			profileSavesDirectory = Path.GetFullPath(Path.Join(SelectedProfile.FilePath, "Savegames"));
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
			string profilePath = Path.GetFullPath(Path.Join(SelectedProfile.FilePath, "Savegames"));
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
				MainWindow.Self.DisplayError("Update Check Failed", "There was a problem reaching the update server. Please check your internet connection and try again later.", false);
			}
			else
			{
				MainWindow.Self.DisplayError($"Error occurred while checking for updates:\n{e.Error}");
			}
		}

		_userInvokedUpdate = false;
	}

	public void OnViewActivated(MainWindow window, DivinityModManager.Views.MainViewControl parentView)
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

		ModUpdatesViewVisible = ModUpdatesAvailable = false;
		MainProgressTitle = "Loading...";
		MainProgressValue = 0d;
		CanCancelProgress = false;
		MainProgressIsActive = true;
		Window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
		Window.TaskbarItemInfo.ProgressValue = 0;
		IsRefreshing = true;
		RxApp.TaskpoolScheduler.ScheduleAsync(RefreshAsync);
	}

	public bool AutoChangedOrder { get; set; }

	private readonly Regex filterPropertyPattern = new("@([^\\s]+?)([\\s]+)([^@\\s]*)");
	private readonly Regex filterPropertyPatternWithQuotes = new("@([^\\s]+?)([\\s\"]+)([^@\"]*)");

	[Reactive] public int TotalActiveModsHidden { get; set; }
	[Reactive] public int TotalInactiveModsHidden { get; set; }
	[Reactive] public int TotalOverrideModsHidden { get; set; }

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
				List<DivinityModFilterData> searchProps = new();

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
		else if (modDataList == ForceLoadedMods)
		{
			TotalOverrideModsHidden = totalHidden;
		}
		else if (modDataList == InactiveMods)
		{
			TotalInactiveModsHidden = totalHidden;
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

			var deleteFilesData = targetMods.Select(x => ModFileDeletionData.FromMod(x, false, isDeletingDuplicates, loadedMods));
			this.View.DeleteFilesView.ViewModel.IsDeletingDuplicates = isDeletingDuplicates;
			this.View.DeleteFilesView.ViewModel.Files.AddRange(deleteFilesData);

			var workshopMods = WorkshopMods.Where(wm => targetUUIDs.Contains(wm.UUID) && File.Exists(wm.FilePath)).Select(x => ModFileDeletionData.FromMod(x, true));
			this.View.DeleteFilesView.ViewModel.Files.AddRange(workshopMods);

			this.View.DeleteFilesView.ViewModel.IsVisible = true;
		}
	}

	public void DeleteMod(DivinityModData mod)
	{
		if (mod.CanDelete)
		{
			DeleteMods(new List<DivinityModData>() { mod });
		}
		else
		{
			ShowAlert("Unable to delete mod", AlertType.Danger, 30);
		}
	}

	public void DeleteSelectedMods(DivinityModData contextMenuMod)
	{
		var list = contextMenuMod.IsActive ? ActiveMods : InactiveMods;
		var targetMods = new List<DivinityModData>();
		targetMods.AddRange(list.Where(x => x.CanDelete && x.IsSelected));
		if (!contextMenuMod.IsSelected && contextMenuMod.CanDelete) targetMods.Add(contextMenuMod);
		if (targetMods.Count > 0)
		{
			DeleteMods(targetMods);
		}
		else
		{
			ShowAlert("Unable to delete selected mod(s)", AlertType.Danger, 30);
		}
	}

	public void RemoveDeletedMods(HashSet<string> deletedMods, HashSet<string> deletedWorkshopMods = null, bool removeFromLoadOrder = true)
	{
		mods.RemoveKeys(deletedMods);

		if (removeFromLoadOrder)
		{
			SelectedModOrder.Order.RemoveAll(x => deletedMods.Contains(x.UUID));
			SelectedProfile.ModOrder.RemoveMany(deletedMods);
			SelectedProfile.ActiveMods.RemoveAll(x => deletedMods.Contains(x.UUID));
			//SaveLoadOrder(true);
		}

		if (deletedWorkshopMods != null && deletedWorkshopMods.Count > 0)
		{
			workshopMods.RemoveKeys(deletedWorkshopMods);
		}

		InactiveMods.RemoveMany(InactiveMods.Where(x => deletedMods.Contains(x.UUID)));
		ActiveMods.RemoveMany(ActiveMods.Where(x => deletedMods.Contains(x.UUID)));
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
		var totalRemoved = SelectedModOrder != null ? SelectedModOrder.Order.RemoveAll(x => !ModExists(x.UUID)) : 0;

		if (totalRemoved > 0)
		{
			ShowAlert($"Removed {totalRemoved} missing mods from the current order. Save to confirm", AlertType.Warning);
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
			mod.Index = ActiveMods.Count - 1;
			SelectedModOrder.Add(mod);
		}
		InactiveMods.Remove(mod);
	}

	public void RemoveActiveMod(DivinityModData mod)
	{
		SelectedModOrder.Remove(mod);
		ActiveMods.Remove(mod);
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

	private static string NexusModsLimitToText(NexusModsObservableApiLimits limits)
	{
		return $"NexusMods Limits [Hourly ({limits.HourlyRemaining}/{limits.HourlyLimit}) Daily ({limits.DailyRemaining}/{limits.DailyLimit})]";
	}

	public MainWindowViewModel() : base()
	{
        Router = new RoutingState();

		Views = new ViewManager(Router, this);

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

		this.ModUpdatesViewData = new ModUpdatesViewData(this);

		var assembly = Assembly.GetExecutingAssembly();
		var productName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false)).Product;
		Version = assembly.GetName().Version.ToString();
		Title = $"{productName} {this.Version}";
		DivinityApp.Log($"{Title} initializing...");
		AutoUpdater.AppTitle = productName;

		this.DropHandler = new ModListDropHandler(this);
		this.DragHandler = new ModListDragHandler(this);

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
					await AddModFromFile(builtinMods, result, filePath, token, false);
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
			foreach (var mod in mods.Items)
			{
				mod.GitHubEnabled = x.Item1;
				mod.NexusModsEnabled = x.Item2;
				mod.SteamWorkshopEnabled = x.Item3;
			}
		});

		this.WhenAnyValue(x => x.IsDragging, x => x.IsRefreshing, x => x.IsLoadingOrder, (b1, b2, b3) => b1 || b2 || b3).ToUIProperty(this, x => x.IsLocked);
		this.WhenAnyValue(x => x.IsLoadingOrder, x => x.IsRefreshing, x => x.IsInitialized, (b1, b2, b3) => !b1 && !b2 && b3).ToUIProperty(this, x => x.AllowDrop, true);

		var whenRefreshing = _updater.WhenAnyValue(x => x.IsRefreshing);
		whenRefreshing.Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.UpdatingBusyIndicatorVisibility);
		whenRefreshing.Select(PropertyConverters.BoolToVisibilityReversed).ToUIProperty(this, x => x.UpdateCountVisibility);
		this.WhenAnyValue(x => x.ModUpdatesViewVisible).Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.UpdatesViewVisibility, Visibility.Collapsed);

		this.WhenAnyValue(x => x.Settings.DebugModeEnabled, x => x.Settings.ExtenderSettings.DeveloperMode)
		.Select(x => PropertyConverters.BoolToVisibility(x.Item1 || x.Item2)).ToUIProperty(this, x => x.DeveloperModeVisibility);

		this.WhenAnyValue(
			x => x.Settings.ExtenderSettings.LogCompile,
			x => x.Settings.ExtenderSettings.LogRuntime,
			x => x.Settings.ExtenderSettings.EnableLogging,
			x => x.Settings.ExtenderSettings.DeveloperMode,
			x => x.Settings.DebugModeEnabled)
		.Select(PropertyConverters.BoolTupleToVisibility).ToUIProperty(this, x => x.LogFolderShortcutButtonVisibility);

		_keys = new AppKeys(this);

		#region Keys Setup
		Keys.SaveDefaultKeybindings();

		var canExecuteSaveCommand = this.WhenAnyValue(x => x.CanSaveOrder, (canSave) => canSave == true);
		Keys.Save.AddAction(() => SaveLoadOrder(), canExecuteSaveCommand);

		var canExecuteSaveAsCommand = this.WhenAnyValue(x => x.CanSaveOrder, x => x.MainProgressIsActive, (canSave, p) => canSave && !p);
		Keys.SaveAs.AddAction(SaveLoadOrderAs, canExecuteSaveAsCommand);
		Keys.ImportMod.AddAction(OpenModImportDialog);
		Keys.ImportNexusModsIds.AddAction(OpenModIdsImportDialog);
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
			gameMasterCampaigns.Clear();
			Profiles.Clear();
			workshopMods.Clear();
			Window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
			Window.TaskbarItemInfo.ProgressValue = 0;
			IsRefreshing = true;
			View.ModLayout.SaveLayout();
			RxApp.TaskpoolScheduler.ScheduleAsync(RefreshAsync);
		}, canRefreshObservable, RxApp.MainThreadScheduler);

		Keys.Refresh.AddAction(() => RefreshCommand.Execute(Unit.Default).Subscribe(), canRefreshObservable);

		var canRefreshModUpdates = this.WhenAnyValue(x => x.IsRefreshing, x => x.IsRefreshingModUpdates, x => x.AppSettingsLoaded,
		(b1, b2, b3) => !b1 && !b2 && b3)
		.ObserveOn(RxApp.MainThreadScheduler).StartWith(false);

		RefreshModUpdatesCommand = ReactiveCommand.Create(() =>
		{
			ModUpdatesViewData?.Clear();
			ModUpdatesViewVisible = ModUpdatesAvailable = false;
			RefreshAllModUpdatesBackground();
		}, canRefreshModUpdates, RxApp.MainThreadScheduler);

		Keys.RefreshModUpdates.AddAction(() => RefreshModUpdatesCommand.Execute().Subscribe(), canRefreshModUpdates);

		Keys.OpenCollectionDownloaderWindow.AddAction(() => App.WM.CollectionDownload.Toggle(true));

		CheckForGitHubModUpdatesCommand = ReactiveCommand.Create(RefreshGitHubModsUpdatesBackground, this.WhenAnyValue(x => x.GitHubModSupportEnabled), RxApp.MainThreadScheduler);
		CheckForNexusModsUpdatesCommand = ReactiveCommand.Create(RefreshNexusModsUpdatesBackground, this.WhenAnyValue(x => x.NexusModsSupportEnabled), RxApp.MainThreadScheduler);
		CheckForSteamWorkshopUpdatesCommand = ReactiveCommand.Create(RefreshSteamWorkshopUpdatesBackground, this.WhenAnyValue(x => x.SteamWorkshopSupportEnabled), RxApp.MainThreadScheduler);
		FetchNexusModsInfoFromFilesCommand = ReactiveCommand.Create(OpenModIdsImportDialog, outputScheduler: RxApp.MainThreadScheduler);

		IObservable<bool> canStartExport = this.WhenAny(x => x.MainProgressToken, (t) => t != null).StartWith(false);
		Keys.ExportOrderToZip.AddAction(ExportLoadOrderToArchive_Start, canStartExport);
		Keys.ExportOrderToArchiveAs.AddAction(ExportLoadOrderToArchiveAs, canStartExport);

		var anyActiveObservable = ActiveMods.WhenAnyValue(x => x.Count).Select(x => x > 0);
		//var anyActiveObservable = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);
		Keys.ExportOrderToList.AddAction(ExportLoadOrderToTextFileAs, anyActiveObservable);
		ExportOrderAsListCommand = ReactiveCommand.Create(ExportLoadOrderToTextFileAs, anyActiveObservable);

		var canOpenDialogWindow = this.WhenAnyValue(x => x.MainProgressIsActive).Select(x => !x);
		Keys.ImportOrderFromSave.AddAction(ImportOrderFromSaveToCurrent, canOpenDialogWindow);
		Keys.ImportOrderFromSaveAsNew.AddAction(ImportOrderFromSaveAsNew, canOpenDialogWindow);
		Keys.ImportOrderFromFile.AddAction(ImportOrderFromFile, canOpenDialogWindow);
		Keys.ImportOrderFromZipFile.AddAction(ImportOrderFromArchive, canOpenDialogWindow);

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
					else
					{
						targetList = InactiveMods;
					}
				}
			}
			else
			{
				targetList = Mods;
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
		});

		var canDownloadNexusFiles = this.WhenAnyValue(x => x.Settings.UpdateSettings.NexusModsAPIKey, x => x.NexusModsSupportEnabled)
			.Select(x => !String.IsNullOrEmpty(x.Item1) && x.Item2);
		Keys.DownloadNXMLink.AddCanExecuteCondition(canDownloadNexusFiles);
		Keys.DownloadNXMLink.AddAction(() =>
		{
			App.WM.NxmDownload.Toggle();
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

		this.WhenAnyValue(x => x.SelectedProfileIndex).Select(x => Profiles.ElementAtOrDefault(x)).BindTo(this, x => x.SelectedProfile);
		var whenProfile = this.WhenAnyValue(x => x.SelectedProfile);
		var hasNonNullProfile = whenProfile.Select(x => x != null);
		hasNonNullProfile.ToUIProperty(this, x => x.HasProfile);

		Keys.ExportOrderToGame.AddAction(ExportLoadOrder, hasNonNullProfile);

		whenProfile.Subscribe(profile =>
		{
			if (profile != null && profile.ActiveMods != null && profile.ActiveMods.Count > 0)
			{
				var adventureModData = AdventureMods.FirstOrDefault(x => profile.ActiveMods.Any(y => y.UUID == x.UUID));
				//Migrate old profiles from Gustav to GustavDev
				if (adventureModData != null && adventureModData.UUID == "991c9c7a-fb80-40cb-8f0d-b92d4e80e9b1")
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

		this.WhenAnyValue(x => x.SelectedModOrderIndex).Select(x => ModOrderList.ElementAtOrDefault(x)).BindTo(this, x => x.SelectedModOrder);
		this.WhenAnyValue(x => x.SelectedModOrder).Select(x => x != null ? x.Name : "None").ToUIProperty(this, x => x.SelectedModOrderName);
		this.WhenAnyValue(x => x.SelectedModOrder).Select(x => x != null && x.IsModSettings).ToUIProperty(this, x => x.IsBaseLoadOrder);

		var lastProfileIndex = -1;
		var lastModOrderIndex = -1;

		this.WhenAnyValue(x => x.SelectedProfileIndex, x => x.SelectedModOrderIndex).Throttle(TimeSpan.FromMilliseconds(25))
			.ObserveOn(RxApp.MainThreadScheduler).Subscribe(data =>
		{
			var profileIndex = data.Item1;
			var orderIndex = data.Item2;
			if (!IsRefreshing && !IsLoadingOrder)
			{
				if (lastProfileIndex != profileIndex)
				{
					lastProfileIndex = profileIndex;
					if (profileIndex > -1 && Profiles.ElementAtOrDefault(profileIndex) is var profile && profile != null)
					{
						if (orderIndex > -1)
						{
							BuildModOrderList(orderIndex);
						}
						else
						{
							BuildModOrderList(0);
						}
					}
				}
				else if (orderIndex != lastModOrderIndex)
				{
					lastModOrderIndex = orderIndex;

					if (orderIndex > -1 && ModOrderList.ElementAtOrDefault(orderIndex) is var order && order != null)
					{
						if (!order.OrderEquals(ActiveMods.Select(x => x.UUID)))
						{
							if (LoadModOrder(order))
							{
								DivinityApp.Log($"Successfully loaded order {order.Name}.");
							}
							else
							{
								DivinityApp.Log($"Failed to load order {order.Name}.");
							}
						}
						else
						{
							DivinityApp.Log($"Order changed to {order.Name}. Skipping list loading since the orders match.");
						}
					}
				}
			}
		});

		var modsConnection = mods.Connect();
		modsConnection.Publish();

		modsConnection.Filter(x => x.IsUserMod).Bind(out _userMods).Subscribe();
		modsConnection.AutoRefresh(x => x.CanAddToLoadOrder).Filter(x => x.CanAddToLoadOrder).Bind(out addonMods).Subscribe();
		var forceLoadedObs = modsConnection.AutoRefresh(x => x.ForceAllowInLoadOrder)
			.Filter(x => x.IsForceLoaded && !x.IsForceLoadedMergedMod && !x.ForceAllowInLoadOrder)
			.ObserveOn(RxApp.MainThreadScheduler);
		forceLoadedObs.Bind(out _forceLoadedMods).Subscribe();
		forceLoadedObs.CountChanged().Select(_ => _forceLoadedMods.Count > 0).ToUIProperty(this, x => x.HasForceLoadedMods);

		//Throttle filters so they only happen when typing stops for 500ms

		this.WhenAnyValue(x => x.ActiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
			Subscribe((s) => { OnFilterTextChanged(s, ActiveMods); });

		this.WhenAnyValue(x => x.InactiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
			Subscribe((s) => { OnFilterTextChanged(s, InactiveMods); });

		this.WhenAnyValue(x => x.OverrideModsFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
			Subscribe((s) => { OnFilterTextChanged(s, ForceLoadedMods); });

		ActiveMods.WhenAnyPropertyChanged(nameof(DivinityModData.Index)).Throttle(TimeSpan.FromMilliseconds(25)).Subscribe(_ =>
		{
			SelectedModOrder?.Sort(SortModOrder);
		});

		var selectedModsConnection = modsConnection.AutoRefresh(x => x.IsSelected, TimeSpan.FromMilliseconds(25)).AutoRefresh(x => x.IsActive, TimeSpan.FromMilliseconds(25)).Filter(x => x.IsSelected);

		selectedModsConnection.Filter(x => x.IsActive).Count().ToUIProperty(this, x => x.ActiveSelected);
		selectedModsConnection.Filter(x => !x.IsActive).Count().ToUIProperty(this, x => x.InactiveSelected);
		ForceLoadedMods.ToObservableChangeSet().AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected).Count().ToUIProperty(this, x => x.OverrideModsSelected);

		this.WhenAnyValue(x => x.ActiveSelected, x => x.TotalActiveModsHidden).Select(x => SelectedToLabel(x.Item1, x.Item2)).ToUIProperty(this, x => x.ActiveSelectedText);
		this.WhenAnyValue(x => x.InactiveSelected, x => x.TotalInactiveModsHidden).Select(x => SelectedToLabel(x.Item1, x.Item2)).ToUIProperty(this, x => x.InactiveSelectedText);
		this.WhenAnyValue(x => x.OverrideModsSelected, x => x.TotalOverrideModsHidden).Select(x => SelectedToLabel(x.Item1, x.Item2)).ToUIProperty(this, x => x.OverrideModsSelectedText);
		//TODO Change .Count to CollectionChanged?
		this.WhenAnyValue(x => x.TotalActiveModsHidden).Select(x => HiddenToLabel(x, ActiveMods.Count)).ToUIProperty(this, x => x.ActiveModsFilterResultText);
		this.WhenAnyValue(x => x.TotalInactiveModsHidden).Select(x => HiddenToLabel(x, InactiveMods.Count)).ToUIProperty(this, x => x.InactiveModsFilterResultText);
		this.WhenAnyValue(x => x.TotalOverrideModsHidden).Select(x => HiddenToLabel(x, ForceLoadedMods.Count)).ToUIProperty(this, x => x.OverrideModsFilterResultText);

		DivinityApp.Events.OrderNameChanged += OnOrderNameChanged;

		modsConnection.Filter(x => x.ModType == "Adventure" && (!x.IsHidden || x.UUID == DivinityApp.MAIN_CAMPAIGN_UUID)).Bind(out adventureMods).DisposeMany().Subscribe();
		this.WhenAnyValue(x => x.SelectedAdventureModIndex, x => x.AdventureMods.Count, (index, count) => index >= 0 && count > 0 && index < count).
		Where(b => b == true).Select(x => AdventureMods[SelectedAdventureModIndex]).ToPropertyEx(this, x => x.SelectedAdventureMod);

		this.WhenAnyValue(x => x.SelectedAdventureModIndex).Throttle(TimeSpan.FromMilliseconds(50)).Subscribe((i) =>
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

		var canRenameOrder = this.WhenAnyValue(x => x.SelectedModOrderIndex, (i) => i > 0);
		ToggleOrderRenamingCommand = ReactiveCommand.CreateFromTask<object, Unit>(ToggleRenamingLoadOrder, canRenameOrder, RxApp.MainThreadScheduler);

		var canDeleteOrder = this.WhenAnyValue(x => x.MainProgressIsActive, x => x.SelectedModOrderIndex).Select(x => !x.Item1 && x.Item2 > 0);
		DeleteOrderCommand = ReactiveCommand.Create<DivinityLoadOrder>(DeleteOrder, canDeleteOrder, RxApp.MainThreadScheduler);

		workshopMods.Connect().Bind(out workshopModsCollection).DisposeMany().Subscribe();

		modsConnection.AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected && !x.IsEditorMod && File.Exists(x.FilePath)).Bind(out selectedPakMods).Subscribe();

		// Blinky animation on the tools/download buttons if the extender is required by mods and is missing
		if (AppSettings.Features.ScriptExtender)
		{
			modsConnection.ObserveOn(RxApp.MainThreadScheduler).AutoRefresh(x => x.ExtenderModStatus).
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

		var anyPakModSelectedObservable = this.WhenAnyValue(x => x.SelectedPakMods.Count, (count) => count > 0);
		Keys.ExtractSelectedMods.AddAction(ExtractSelectedMods_Start, anyPakModSelectedObservable);

		var canExtractAdventure = this.WhenAnyValue(x => x.SelectedAdventureMod, x => x.Settings.GameMasterModeEnabled, (m, b) => !b && m != null && !m.IsEditorMod && !m.IsLarianMod);
		Keys.ExtractSelectedAdventure.AddAction(ExtractSelectedAdventure, canExtractAdventure);

		this.WhenAnyValue(x => x.ModUpdatesViewData.TotalUpdates, total => total > 0).BindTo(this, x => x.ModUpdatesAvailable);

		ModUpdatesViewData.CloseView = new Action<bool>((bool refresh) =>
		{
			ModUpdatesViewData.Clear();
			if (refresh) RefreshCommand.Execute(Unit.Default).Subscribe();
			ModUpdatesViewVisible = false;
			Window.Activate();
		});

		//var canSpeakOrder = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);

		Keys.SpeakActiveModOrder.AddAction(() =>
		{
			if (ActiveMods.Count > 0)
			{
				string text = String.Join(", ", ActiveMods.Select(x => x.DisplayName));
				ScreenReaderHelper.Speak($"{ActiveMods.Count} mods in the active order, including:", true);
				ScreenReaderHelper.Speak(text, false);
				//ShowAlert($"Active mods: {text}", AlertType.Info, 10);
			}
			else
			{
				//ShowAlert($"No mods in active order.", AlertType.Warning, 10);
				ScreenReaderHelper.Speak($"The active mods order is empty.");
			}
		});

		SaveSettingsSilentlyCommand = ReactiveCommand.Create(SaveSettings);

		#region DungeonMaster Support

		var gmModeChanged = Settings.WhenAnyValue(x => x.GameMasterModeEnabled);
		gmModeChanged.Select(PropertyConverters.BoolToVisibilityReversed).ToUIProperty(this, x => x.AdventureModBoxVisibility, Visibility.Visible);
		gmModeChanged.Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.GameMasterModeVisibility, Visibility.Collapsed);

		gameMasterCampaigns.Connect().Bind(out gameMasterCampaignsData).Subscribe();

		var justSelectedGameMasterCampaign = this.WhenAnyValue(x => x.SelectedGameMasterCampaignIndex, x => x.GameMasterCampaigns.Count);
		justSelectedGameMasterCampaign.Select(x => GameMasterCampaigns.ElementAtOrDefault(x.Item1)).ToUIProperty(this, x => x.SelectedGameMasterCampaign);

		Keys.ImportOrderFromSelectedGMCampaign.AddAction(() => LoadGameMasterCampaignModOrder(SelectedGameMasterCampaign), gmModeChanged);

		justSelectedGameMasterCampaign.ObserveOn(RxApp.MainThreadScheduler).Subscribe((d) =>
		{
			if (!IsRefreshing && IsInitialized && Settings.AutomaticallyLoadGMCampaignMods && d.Item1 > -1)
			{
				var selectedCampaign = GameMasterCampaigns.ElementAtOrDefault(d.Item1);
				if (selectedCampaign != null && !IsLoadingOrder)
				{
					if (LoadGameMasterCampaignModOrder(selectedCampaign))
					{
						DivinityApp.Log($"Successfully loaded GM campaign order {selectedCampaign.Name}.");
					}
					else
					{
						DivinityApp.Log($"Failed to load GM campaign order {selectedCampaign.Name}.");
					}
				}
			}
		});
		#endregion

		this.WhenAnyValue(x => x.View.DeleteFilesView.ViewModel.IsVisible).ToUIProperty(this, x => x.IsDeletingFiles);

		this.WhenAnyValue(x => x.MainProgressIsActive, x => x.IsDeletingFiles, (a, b) => a || b).ToUIProperty(this, x => x.HideModList, true);

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

		CanSaveOrder = true;
		LayoutMode = 0;

		var fwService = Services.Get<IFileWatcherService>();
		_modSettingsWatcher = fwService.WatchDirectory("", "*modsettings.lsx");
		//modSettingsWatcher.PauseWatcher(true);
		this.WhenAnyValue(x => x.SelectedProfile).WhereNotNull().Select(x => x.FilePath).Subscribe(path =>
		{
			_modSettingsWatcher.SetDirectory(path);
		});

		IDisposable checkModSettingsTask = null;

		_modSettingsWatcher.FileChanged.Subscribe(e =>
		{
			if (SelectedModOrder != null)
			{
				//var exeName = !Settings.LaunchDX11 ? "bg3" : "bg3_dx11";
				//var isGameRunning = Process.GetProcessesByName(exeName).Length > 0;
				checkModSettingsTask?.Dispose();
				checkModSettingsTask = RxApp.TaskpoolScheduler.ScheduleAsync(TimeSpan.FromSeconds(2), async (sch, cts) =>
				{
					var modSettingsData = await DivinityModDataLoader.LoadModSettingsFileAsync(e.FullPath);
					if (modSettingsData.ActiveMods.Count < this.SelectedModOrder.Order.Count)
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
				});
			}
		});
	}
}
