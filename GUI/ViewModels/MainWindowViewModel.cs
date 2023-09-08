using AutoUpdaterDotNET;

using DivinityModManager.Extensions;
using DivinityModManager.Models;
using DivinityModManager.Models.App;
using DivinityModManager.Util;
using DivinityModManager.Views;

using DynamicData;
using DynamicData.Binding;
using DynamicData.Aggregation;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using SharpCompress.Common;
using SharpCompress.Writers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using Alphaleonis.Win32.Filesystem;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;
using System.Reactive.Subjects;
using System.Windows.Markup;
using DivinityModManager.Models.Extender;
using DynamicData.Kernel;


namespace DivinityModManager.ViewModels
{
    public class MainWindowViewModel : BaseHistoryViewModel, IActivatableViewModel, IDivinityAppViewModel
    {
        [Reactive] public MainWindow View { get; private set; }

        public ModViewLayout Layout { get; set; }

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


        [Reactive] public string Title { get; set; }
        [Reactive] public string Version { get; set; }

        private readonly AppKeys _keys;
        public AppKeys Keys => _keys;

        [Reactive] public bool IsInitialized { get; private set; }

        protected readonly SourceCache<DivinityModData, string> mods = new SourceCache<DivinityModData, string>(mod => mod.UUID);

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

        private readonly ObservableAsPropertyHelper<DivinityModData> _selectedAdventureMod;
        public DivinityModData SelectedAdventureMod => _selectedAdventureMod.Value;

        private readonly ObservableAsPropertyHelper<Visibility> _adventureModBoxVisibility;
        public Visibility AdventureModBoxVisibility => _adventureModBoxVisibility.Value;

        protected ReadOnlyObservableCollection<DivinityModData> selectedPakMods;
        public ReadOnlyObservableCollection<DivinityModData> SelectedPakMods => selectedPakMods;

        protected readonly SourceCache<DivinityModData, string> workshopMods = new SourceCache<DivinityModData, string>(mod => mod.UUID);

        protected ReadOnlyObservableCollection<DivinityModData> workshopModsCollection;
        public ReadOnlyObservableCollection<DivinityModData> WorkshopMods => workshopModsCollection;

        private DivinityModManagerCachedWorkshopData CachedWorkshopData { get; set; } = new DivinityModManagerCachedWorkshopData();

        public DivinityPathwayData PathwayData { get; private set; } = new DivinityPathwayData();

        public ModUpdatesViewData ModUpdatesViewData { get; private set; }

        private IgnoredModsData ignoredModsData;

        public IgnoredModsData IgnoredMods => ignoredModsData;

        private readonly AppSettings appSettings = new AppSettings();

        public AppSettings AppSettings => appSettings;

        [Reactive] public DivinityModManagerSettings Settings { get; set; }

        private readonly ObservableCollectionExtended<DivinityModData> _activeMods = new ObservableCollectionExtended<DivinityModData>();
        public ObservableCollectionExtended<DivinityModData> ActiveMods => _activeMods;

        private readonly ObservableCollectionExtended<DivinityModData> _inactiveMods = new ObservableCollectionExtended<DivinityModData>();
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

        [Reactive] public int LayoutMode { get; set; }
        [Reactive] public bool CanSaveOrder { get; set; }
        [Reactive] public bool IsLoadingOrder { get; set; }
        [Reactive] public bool OrderJustLoaded { get; set; }
        [Reactive] public bool IsDragging { get; set; }
        [Reactive] public bool AppSettingsLoaded { get; set; }
        [Reactive] public bool IsRefreshing { get; private set; }
        [Reactive] public bool IsRefreshingWorkshop { get; private set; }

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
        [Reactive] public bool WorkshopSupportEnabled { get; set; }
        [Reactive] public bool CanMoveSelectedMods { get; set; }

        public IObservable<bool> canRenameOrder;

        private IObservable<bool> canOpenWorkshopFolder;
        private IObservable<bool> canOpenGameExe;
        private readonly IObservable<bool> canOpenDialogWindow;
        private IObservable<bool> canOpenLogDirectory;

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
        public ReactiveCommand<Unit, Unit> RefreshCommand { get; private set; }
        public ReactiveCommand<Unit, Unit> RefreshWorkshopCommand { get; private set; }

        private DivinityGameLaunchWindowAction actionOnGameLaunch = DivinityGameLaunchWindowAction.None;
        public DivinityGameLaunchWindowAction ActionOnGameLaunch
        {
            get => actionOnGameLaunch;
            set { this.RaiseAndSetIfChanged(ref actionOnGameLaunch, value); }
        }
        public EventHandler OnRefreshed { get; set; }

        #region DungeonMaster Support

        //TODO - Waiting for DM mode to be released

        private readonly ObservableAsPropertyHelper<Visibility> _gameMasterModeVisibility;
        public Visibility GameMasterModeVisibility => _gameMasterModeVisibility.Value;

        protected SourceList<DivinityGameMasterCampaign> gameMasterCampaigns = new SourceList<DivinityGameMasterCampaign>();

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

        private readonly ObservableAsPropertyHelper<DivinityGameMasterCampaign> _selectedGameMasterCampaign;
        public DivinityGameMasterCampaign SelectedGameMasterCampaign => _selectedGameMasterCampaign.Value;
        public ICommand OpenGameMasterCampaignInFileExplorerCommand { get; private set; }
        public ICommand CopyGameMasterCampaignPathToClipboardCommand { get; private set; }

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
                nextSelected = gameMasterCampaigns.Items.OrderByDescending(x => x.LastModified ?? DateTime.MinValue).FirstOrDefault();

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

            List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();
            if (campaign.Dependencies.Count > 0)
            {
                int index = 0;
                foreach (var entry in campaign.Dependencies)
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
                System.IO.Stream webStream = null;
                System.IO.Stream unzippedEntryStream = null;
                try
                {
                    RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Downloading {PathwayData.ScriptExtenderLatestReleaseUrl}...");
                    webStream = await WebHelper.DownloadFileAsStreamAsync(PathwayData.ScriptExtenderLatestReleaseUrl, MainProgressToken.Token);
                    if (webStream != null)
                    {
                        successes += 1;
                        IncreaseMainProgressValue(taskStepAmount);
                        RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Extracting zip to {exeDir}...");
                        ZipArchive archive = new ZipArchive(webStream);
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (MainProgressToken.IsCancellationRequested) break;
                            if (entry.Name.Equals(DivinityApp.EXTENDER_UPDATER_FILE, StringComparison.OrdinalIgnoreCase))
                            {
                                unzippedEntryStream = entry.Open(); // .Open will return a stream
                                using (var fs = File.Create(dllDestination, 4096, System.IO.FileOptions.Asynchronous))
                                {
                                    await unzippedEntryStream.CopyToAsync(fs, 4096, MainProgressToken.Token);
                                    successes += 1;
                                }
                                break;
                            }
                        }
                        IncreaseMainProgressValue(taskStepAmount);
                    }
                }
                catch (Exception ex)
                {
                    DivinityApp.Log($"Error extracting package: {ex}");
                }
                finally
                {
                    RxApp.MainThreadScheduler.Schedule(_ => MainProgressWorkText = $"Cleaning up...");
                    webStream?.Close();
                    unzippedEntryStream?.Close();
                    successes += 1;
                    IncreaseMainProgressValue(taskStepAmount);
                }
                await ctrl.Yield();
                RxApp.MainThreadScheduler.Schedule(_ => OnMainProgressComplete());

                RxApp.MainThreadScheduler.Schedule(() =>
                {
                    if (successes >= 3)
                    {
                        ShowAlert($"Successfully installed the Extender updater {DivinityApp.EXTENDER_UPDATER_FILE} to '{exeDir}'.", AlertType.Success, 20);
                        HighlightExtenderDownload = false;
                        Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = true;
                        Settings.ExtenderSettings.ExtenderVersion = 1;
                        if (Settings.ExtenderSettings.ExtenderVersion <= -1)
                        {
                            if (!String.IsNullOrWhiteSpace(PathwayData.ScriptExtenderLatestReleaseVersion))
                            {
                                var re = new Regex("v([0-9]+)");
                                var m = re.Match(PathwayData.ScriptExtenderLatestReleaseVersion);
                                if (m.Success)
                                {
                                    if (int.TryParse(m.Groups[1].Value, out int version))
                                    {
                                        Settings.ExtenderSettings.ExtenderVersion = version;
                                        DivinityApp.Log($"Set extender version to v{version},");
                                    }
                                }
                            }
                            else if (PathwayData.ScriptExtenderLatestReleaseUrl.Contains("v"))
                            {
                                var re = new Regex("v([0-9]+).*.zip");
                                var m = re.Match(PathwayData.ScriptExtenderLatestReleaseUrl);
                                if (m.Success)
                                {
                                    if (int.TryParse(m.Groups[1].Value, out int version))
                                    {
                                        if (version > Settings.ExtenderSettings.ExtenderVersion)
                                        {
                                            Settings.ExtenderSettings.ExtenderVersion = version;
                                            DivinityApp.Log($"Set extender version to v{version},");
                                        }
                                    }
                                }
                            }
                        }
                        CheckExtenderData();
                    }
                    else
                    {
                        ShowAlert($"Error occurred when installing the Extender updater {DivinityApp.EXTENDER_UPDATER_FILE}. Check the log.", AlertType.Danger, 30);
                    }
                });

                return Disposable.Empty;
            });
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

                    var result = AdonisUI.Controls.MessageBox.Show(new AdonisUI.Controls.MessageBoxModel
                    {
                        Text = messageText,
                        Caption = "Download & Install the Script Extender?",
                        Buttons = AdonisUI.Controls.MessageBoxButtons.YesNo(),
                        Icon = AdonisUI.Controls.MessageBoxImage.Question
                    });

                    if (result == AdonisUI.Controls.MessageBoxResult.Yes)
                    {
                        DownloadScriptExtender(exeDir);
                    }
                }
                else
                {
                    ShowAlert("The 'Game Executable Path' is not set or is not valid.", AlertType.Danger);
                }
            }
            else
            {
                DivinityApp.Log($"Getting a release download link failed for some reason. Opening repo url: {DivinityApp.EXTENDER_LATEST_URL}");
                DivinityFileUtils.TryOpenPath(DivinityApp.EXTENDER_LATEST_URL);
            }
        }

        private async Task<Unit> LoadExtenderSettingsAsync(CancellationToken t)
        {
            try
            {
                string latestReleaseZipUrl = "";
                DivinityApp.Log($"Checking for latest {DivinityApp.EXTENDER_UPDATER_FILE} release at 'https://github.com/{DivinityApp.EXTENDER_REPO_URL}'.");
                var latestReleaseData = await GithubHelper.GetLatestReleaseDataAsync(DivinityApp.EXTENDER_REPO_URL);
                if (!String.IsNullOrEmpty(latestReleaseData))
                {
                    var jsonData = DivinityJsonUtils.SafeDeserialize<Dictionary<string, object>>(latestReleaseData);
                    if (jsonData != null)
                    {
                        if (jsonData.TryGetValue("assets", out var assetsArray))
                        {
                            JArray assets = (JArray)assetsArray;
                            foreach (var obj in assets.Children<JObject>())
                            {
                                if (obj.TryGetValue("browser_download_url", StringComparison.OrdinalIgnoreCase, out var browserUrl))
                                {
                                    latestReleaseZipUrl = browserUrl.ToString();
                                }
                            }
                        }
                        if (jsonData.TryGetValue("tag_name", out var tagName))
                        {
                            PathwayData.ScriptExtenderLatestReleaseVersion = (string)tagName;
                        }
                    }
                    if (!String.IsNullOrEmpty(latestReleaseZipUrl))
                    {
                        OpenRepoLinkToDownload = false;
                        PathwayData.ScriptExtenderLatestReleaseUrl = latestReleaseZipUrl;
                        DivinityApp.Log($"Script Extender latest release url found: {latestReleaseZipUrl}");
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

            await Observable.Start(() =>
            {

                try
                {
                    string extenderSettingsJson = PathwayData.ScriptExtenderSettingsFile(Settings);
                    if (extenderSettingsJson.IsExistingFile())
                    {
                        var osirisExtenderSettings = DivinityJsonUtils.SafeDeserializeFromPath<ScriptExtenderSettings>(extenderSettingsJson);
                        if (osirisExtenderSettings != null)
                        {
                            DivinityApp.Log($"Loaded extender settings from '{extenderSettingsJson}'.");
                            Settings.ExtenderSettings.Set(osirisExtenderSettings);
                        }
                    }
                }
                catch (Exception ex)
                {
                    DivinityApp.Log($"Error loading extender settings: {ex}");
                }

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
                            Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = true;
                            DivinityApp.Log($"Found the Extender at '{extenderUpdaterPath}'.");
                            FileVersionInfo extenderInfo = FileVersionInfo.GetVersionInfo(extenderUpdaterPath);
                            if (!String.IsNullOrEmpty(extenderInfo.FileVersion))
                            {
                                var version = extenderInfo.FileVersion.Split('.')[0];
                                if (int.TryParse(version, out int intVersion))
                                {
                                    if (intVersion >= 1)
                                    {
                                        //Assume we're using the latest release version since we have the updater
                                        Settings.ExtenderSettings.ExtenderVersion = DivinityApp.EXTENDER_DEFAULT_VERSION;
                                    }
                                }
                                else
                                {
                                    Settings.ExtenderSettings.ExtenderVersion = -1;
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
                        Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = true;
                        DivinityApp.Log($"WARNING: {extenderUpdaterPath} is locked by a process.");
                    }
                    catch (Exception ex)
                    {
                        DivinityApp.Log($"Error reading: '{extenderUpdaterPath}'\n\t{ex}");
                    }
                }
                else
                {
                    Settings.ExtenderSettings.ExtenderUpdaterIsAvailable = false;
                    DivinityApp.Log($"Extender updater {DivinityApp.EXTENDER_UPDATER_FILE} not found.");
                }

                var extenderAppDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DivinityApp.EXTENDER_APPDATA_DIRECTORY);
                if (Directory.Exists(extenderAppDataDir))
                {
                    var files = Directory.EnumerateFiles(extenderAppDataDir, new DirectoryEnumerationFilters()
                    {
                        CancellationToken = t,
                        InclusionFilter = (f) => f.FileName.Equals(DivinityApp.EXTENDER_APPDATA_DLL, StringComparison.OrdinalIgnoreCase)
                    });
                    var hasExtenderInstalled = false;
                    int extenderVersion = -1;
                    foreach (var f in files)
                    {
                        hasExtenderInstalled = true;
                        try
                        {
                            FileVersionInfo extenderInfo = FileVersionInfo.GetVersionInfo(f);
                            if (!String.IsNullOrEmpty(extenderInfo.FileVersion))
                            {
                                var version = extenderInfo.FileVersion.Split('.').FirstOrDefault();
                                if (!String.IsNullOrEmpty(version) && int.TryParse(version, out int intVersion))
                                {
                                    if (intVersion > extenderVersion)
                                    {
                                        DivinityApp.Log($"Script Extender version found: '{Settings.ExtenderSettings.ExtenderVersion}'.");
                                        extenderVersion = intVersion;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DivinityApp.Log($"Error getting file info from: '{f}'\n\t{ex}");
                        }
                    }
                    if (extenderVersion > -1)
                    {
                        Settings.ExtenderSettings.ExtenderIsAvailable = hasExtenderInstalled;
                        Settings.ExtenderSettings.ExtenderVersion = extenderVersion;
                    }
                }
                return Unit.Default;
            }, RxApp.MainThreadScheduler);

            return Unit.Default;
        }

        private void LoadExtenderSettings()
        {
            if (File.Exists(Settings.GameExecutablePath))
            {
                RxApp.TaskpoolScheduler.ScheduleAsync(async (c, t) =>
                {
                    await LoadExtenderSettingsAsync(t);
                    await c.Yield();
                    RxApp.MainThreadScheduler.Schedule(CheckExtenderData);
                    return Disposable.Empty;
                });
            }
            else
            {
                CheckExtenderData();
            }
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
            try
            {
                Process proc = new Process();
                proc.StartInfo.FileName = exePath;
                proc.StartInfo.Arguments = launchParams;
                proc.StartInfo.WorkingDirectory = Directory.GetParent(exePath).FullName;
                proc.Start();
            }
            catch (Exception ex)
            {
                View.ToggleLogging(true);
                DivinityApp.Log($"Error starting game exe:\n{ex}");
                ShowAlert("Error occurred when trying to start the game. Check the log.", AlertType.Danger);
            }
        }

        private bool LoadSettings()
        {
            Settings?.Dispose();

            var loaded = false;
            var settingsFile = DivinityApp.GetAppDirectory("Data", "settings.json");
            try
            {
                if (File.Exists(settingsFile))
                {
                    using (var reader = File.OpenText(settingsFile))
                    {
                        var fileText = reader.ReadToEnd();
                        Settings = DivinityJsonUtils.SafeDeserialize<DivinityModManagerSettings>(fileText);
                        loaded = Settings != null;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowAlert($"Error loading settings at '{settingsFile}': {ex}", AlertType.Danger);
                Settings = null;
            }

            if (Settings == null)
            {
                Settings = new DivinityModManagerSettings();
                SaveSettings();
            }

            LoadAppConfig();

            canOpenWorkshopFolder = this.WhenAnyValue(x => x.WorkshopSupportEnabled, x => x.Settings.WorkshopPath,
                (b, p) => (b && !String.IsNullOrEmpty(p) && Directory.Exists(p))).StartWith(false);

            if (AppSettings.FeatureEnabled("Workshop"))
            {
                if (!String.IsNullOrWhiteSpace(Settings.WorkshopPath))
                {
                    var baseName = Path.GetFileNameWithoutExtension(Settings.WorkshopPath);
                    if (baseName == "steamapps")
                    {
                        var newFolder = Path.Combine(Settings.WorkshopPath, $"workshop/content/{AppSettings.DefaultPathways.Steam.AppID}");
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
                        SaveSettings();
                    }
                }
                else if (Directory.Exists(Settings.WorkshopPath))
                {
                    DivinityApp.Log($"Found workshop folder at: '{Settings.WorkshopPath}'.");
                }
                WorkshopSupportEnabled = true;
            }
            else
            {
                WorkshopSupportEnabled = false;
                Settings.WorkshopPath = "";
            }

            canOpenGameExe = this.WhenAnyValue(x => x.Settings.GameExecutablePath, p => !String.IsNullOrEmpty(p) && File.Exists(p)).StartWith(false);
            canOpenLogDirectory = this.WhenAnyValue(x => x.Settings.ExtenderLogDirectory, (f) => Directory.Exists(f)).StartWith(false);

            var canDownloadScriptExtender = this.WhenAnyValue(x => x.PathwayData.ScriptExtenderLatestReleaseUrl, (p) => !String.IsNullOrEmpty(p));
            Keys.DownloadScriptExtender.AddAction(() => AskToDownloadScriptExtender(), canDownloadScriptExtender);

            var canOpenModsFolder = this.WhenAnyValue(x => x.PathwayData.AppDataModsPath, (p) => !String.IsNullOrEmpty(p) && Directory.Exists(p));
            Keys.OpenModsFolder.AddAction(() =>
            {
                DivinityFileUtils.TryOpenPath(PathwayData.AppDataModsPath);
            }, canOpenModsFolder);

            var canOpenGameFolder = this.WhenAnyValue(x => x.Settings.GameExecutablePath, (p) => !String.IsNullOrEmpty(p) && File.Exists(p));
            Keys.OpenGameFolder.AddAction(() =>
            {
                var folder = Path.GetDirectoryName(Settings.GameExecutablePath);
                if (Directory.Exists(folder))
                {
                    DivinityFileUtils.TryOpenPath(folder);
                }
            }, canOpenGameFolder);

            Keys.OpenLogsFolder.AddAction(() =>
            {
                DivinityFileUtils.TryOpenPath(Settings.ExtenderLogDirectory);
            }, canOpenLogDirectory);

            Keys.OpenWorkshopFolder.AddAction(() =>
            {
                //DivinityApp.Log($"WorkshopSupportEnabled:{WorkshopSupportEnabled} canOpenWorkshopFolder CanExecute:{OpenWorkshopFolderCommand.CanExecute(null)}");
                if (!String.IsNullOrEmpty(Settings.WorkshopPath) && Directory.Exists(Settings.WorkshopPath))
                {
                    DivinityFileUtils.TryOpenPath(Settings.WorkshopPath);
                }
            }, canOpenWorkshopFolder);

            Keys.LaunchGame.AddAction(() =>
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
                            ShowAlert("No game executable path set.", AlertType.Danger, 30);
                        }
                        else
                        {
                            ShowAlert($"Failed to find game exe at, \"{Settings.GameExecutablePath}\"", AlertType.Danger, 90);
                        }
                        return;
                    }
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

                if (Settings.SkipLauncher && launchParams.IndexOf("skip-launcher") < 0)
                {
                    if (String.IsNullOrWhiteSpace(launchParams))
                    {
                        launchParams = "--skip-launcher";
                    }
                    else
                    {
                        launchParams = "--skip-launcher " + launchParams;
                    }
                }

                if (!Settings.LaunchThroughSteam)
                {
                    var exePath = Settings.GameExecutablePath;
                    var exeDir = Path.GetDirectoryName(exePath);

                    if (Settings.LaunchDX11)
                    {
                        var nextExe = Path.Combine(exeDir, "bg3_dx11.exe");
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
                    DivinityFileUtils.TryOpenPath(steamUrl);
                }

                if (Settings.ActionOnGameLaunch != DivinityGameLaunchWindowAction.None)
                {
                    switch (Settings.ActionOnGameLaunch)
                    {
                        case DivinityGameLaunchWindowAction.Minimize:
                            this.View.WindowState = WindowState.Minimized;
                            break;
                        case DivinityGameLaunchWindowAction.Close:
                            App.Current.Shutdown();
                            break;
                    }
                }

            }, canOpenGameExe);

            Settings.SaveSettingsCommand = ReactiveCommand.Create(() =>
            {
                try
                {
                    System.IO.FileAttributes attr = File.GetAttributes(Settings.GameExecutablePath);

                    if (attr.HasFlag(System.IO.FileAttributes.Directory))
                    {
                        string exeName = "";
                        if (!DivinityRegistryHelper.IsGOG)
                        {
                            exeName = Path.GetFileName(AppSettings.DefaultPathways.Steam.ExePath);
                        }
                        else
                        {
                            exeName = Path.GetFileName(AppSettings.DefaultPathways.GOG.ExePath);
                        }

                        var exe = Path.Combine(Settings.GameExecutablePath, exeName);
                        if (File.Exists(exe))
                        {
                            Settings.GameExecutablePath = exe;
                        }
                    }

                    if (Settings.SelectedTabIndex == 1)
                    {
                        // Help for people confused about needing to click the export button to save the json
                        Settings.ExportExtenderSettingsCommand?.Execute(null);
                    }
                }
                catch (Exception) { }
                if (SaveSettings())
                {
                    ShowAlert($"Saved settings to '{settingsFile}'.", AlertType.Success, 10);
                }
            }).DisposeWith(Settings.Disposables);

            Settings.OpenSettingsFolderCommand = ReactiveCommand.Create(() =>
            {
                DivinityFileUtils.TryOpenPath(DivinityApp.GetAppDirectory(DivinityApp.DIR_DATA));
            }).DisposeWith(Settings.Disposables);

            Settings.ExportExtenderSettingsCommand = ReactiveCommand.Create(() =>
            {
                string outputFile = Path.Combine(Path.GetDirectoryName(Settings.GameExecutablePath), "ScriptExtenderSettings.json");
                try
                {
                    var jsonSettings = new JsonSerializerSettings
                    {
                        DefaultValueHandling = DefaultValueHandling.Ignore,
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.Indented
                    };

                    if (Settings.ExportDefaultExtenderSettings)
                    {
                        jsonSettings.DefaultValueHandling = DefaultValueHandling.Include;
                    }
                    string contents = JsonConvert.SerializeObject(Settings.ExtenderSettings, jsonSettings);
                    File.WriteAllText(outputFile, contents);
                    ShowAlert($"Saved Script Extender settings to '{outputFile}'.", AlertType.Success, 20);
                }
                catch (Exception ex)
                {
                    ShowAlert($"Error saving Script Extender settings to '{outputFile}':\n{ex}", AlertType.Danger);
                }
            }).DisposeWith(Settings.Disposables);

            var canResetExtenderSettingsObservable = this.WhenAny(x => x.Settings.ExtenderSettings, (extenderSettings) => extenderSettings != null).StartWith(false);
            Settings.ResetExtenderSettingsToDefaultCommand = ReactiveCommand.Create(() =>
            {
                MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(View.SettingsWindow, $"Reset Extender Settings to Default?\nCurrent Extender Settings will be lost.", "Confirm Extender Settings Reset",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, View.MainWindowMessageBox_OK.Style);
                if (result == MessageBoxResult.Yes)
                {
                    Settings.ExportDefaultExtenderSettings = false;
                    Settings.ExtenderSettings.SetToDefault();
                }
            }, canResetExtenderSettingsObservable).DisposeWith(Settings.Disposables);

            Settings.ResetKeybindingsCommand = ReactiveCommand.Create(() =>
            {
                MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show((Window)this.View.SettingsWindow, $"Reset Keybindings to Default?\nCurrent keybindings may be lost.", "Confirm Reset",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, (Style)this.View.MainWindowMessageBox_OK.Style);
                if (result == MessageBoxResult.Yes)
                {
                    Keys.SetToDefault();
                }
            });

            Settings.ClearWorkshopCacheCommand = ReactiveCommand.Create(() =>
            {
                var filePath = DivinityApp.GetAppDirectory("Data", "workshopdata.json");
                if (File.Exists(filePath))
                {
                    MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(View.SettingsWindow, $"Delete local workshop cache?\nThis cannot be undone.\nRefresh to download tag data once more.", "Confirm Delete Cache",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, View.MainWindowMessageBox_OK.Style);
                    if (result == MessageBoxResult.Yes)
                    {
                        try
                        {
                            RecycleBinHelper.DeleteFile(filePath, false, true);
                            ShowAlert($"Deleted local workshop cache at '{filePath}'.", AlertType.Success, 20);
                        }
                        catch (Exception ex)
                        {
                            ShowAlert($"Error deleting workshop cache:\n{ex}", AlertType.Danger);
                        }
                    }
                }
            }).DisposeWith(Settings.Disposables);

            Settings.AddLaunchParamCommand = ReactiveCommand.Create((string param) =>
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
            }).DisposeWith(Settings.Disposables);

            Settings.ClearLaunchParamsCommand = ReactiveCommand.Create(() =>
            {
                Settings.GameLaunchParams = "";
            }).DisposeWith(Settings.Disposables);

            this.WhenAnyValue(x => x.Settings.LogEnabled).Subscribe((logEnabled) =>
            {
                View.ToggleLogging(logEnabled);
            }).DisposeWith(Settings.Disposables);

            this.WhenAnyValue(x => x.Settings.DarkThemeEnabled).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
            {
                View.UpdateColorTheme(b);
                SaveSettings();
            }).DisposeWith(Settings.Disposables);

            // Updating extender requirement display
            this.WhenAnyValue(x => x.Settings.ExtenderSettings.EnableExtensions).ObserveOn(RxApp.MainThreadScheduler).Subscribe((b) =>
            {
                CheckExtenderData();
            }).DisposeWith(Settings.Disposables);

            ActionOnGameLaunch = Settings.ActionOnGameLaunch;

            var actionLaunchChanged = this.WhenAnyValue(x => x.ActionOnGameLaunch).ObserveOn(RxApp.MainThreadScheduler);
            actionLaunchChanged.Subscribe((action) =>
            {
                SaveSettings();
            }).DisposeWith(Settings.Disposables);
            actionLaunchChanged.BindTo(this, x => x.Settings.ActionOnGameLaunch).DisposeWith(Settings.Disposables);

            this.WhenAnyValue(x => x.Settings.DisplayFileNames).Subscribe((b) =>
            {
                if (View.MenuItems.TryGetValue("ToggleFileNameDisplay", out var menuItem))
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
            }).DisposeWith(Settings.Disposables);

            this.WhenAnyValue(x => x.Settings.DocumentsFolderPathOverride).Skip(1).Subscribe((x) =>
            {
                if (!IsLocked)
                {
                    SetGamePathways(Settings.GameDataPath, x);
                    ShowAlert($"Larian folder changed to '{PathwayData.AppDataGameFolder}'. Make sure to refresh.", AlertType.Warning, 60);
                }
            }).DisposeWith(Settings.Disposables);

            this.WhenAnyValue(x => x.Settings.SaveWindowLocation).Subscribe(View.ToggleWindowPositionSaving).DisposeWith(Settings.Disposables);

            if (Settings.LogEnabled)
            {
                View.ToggleLogging(true);
            }

            SetGamePathways(Settings.GameDataPath, Settings.DocumentsFolderPathOverride);

            if (loaded)
            {
                Settings.CanSaveSettings = false;
            }

            return loaded;
        }

        private void OnOrderNameChanged(object sender, OrderNameChangedArgs e)
        {
            if (Settings.LastOrder == e.LastName)
            {
                Settings.LastOrder = e.NewName;
                SaveSettings();
            }
        }

        public bool SaveSettings()
        {
            string settingsFile = DivinityApp.GetAppDirectory("Data", "settings.json");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(settingsFile));
                string contents = JsonConvert.SerializeObject(Settings, Formatting.Indented);
                File.WriteAllText(settingsFile, contents);
                Settings.CanSaveSettings = false;
                Keys.SaveKeybindings(this);
                return true;
            }
            catch (Exception ex)
            {
                ShowAlert($"Error saving settings at '{settingsFile}': {ex}", AlertType.Danger);
            }
            return false;
        }

        object deferSave = null;

        public void QueueSave()
        {
            if (deferSave == null)
            {
                deferSave = RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(250), () =>
                {
                    SaveSettings();
                    deferSave = null;
                });
            }
        }

        public async Task<List<DivinityModData>> LoadWorkshopModsAsync(CancellationToken cts)
        {
            List<DivinityModData> newWorkshopMods = new List<DivinityModData>();

            if (Directory.Exists(Settings.WorkshopPath))
            {
                newWorkshopMods = await DivinityModDataLoader.LoadModPackageDataAsync(Settings.WorkshopPath, cts);
                if (cts.IsCancellationRequested)
                {
                    return newWorkshopMods;
                }
                foreach (var workshopMod in newWorkshopMods)
                {
                    string workshopID = Directory.GetParent(workshopMod.FilePath)?.Name;
                    if (!String.IsNullOrEmpty(workshopID))
                    {
                        workshopMod.WorkshopData.ID = workshopID;
                    }
                }

                return newWorkshopMods.OrderBy(m => m.Name).ToList();
            }

            return newWorkshopMods;
        }

        public void CheckForModUpdates(CancellationToken cts)
        {
            ModUpdatesViewData.Clear();

            int count = 0;
            foreach (var workshopMod in WorkshopMods)
            {
                if (cts.IsCancellationRequested)
                {
                    break;
                }
                if (TryGetMod(workshopMod.UUID, out var pakMod))
                {
                    pakMod.WorkshopData.ID = workshopMod.WorkshopData.ID;
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

                            ModUpdatesViewData.Updates.Add(new DivinityModUpdateData()
                            {
                                LocalMod = pakMod,
                                WorkshopMod = workshopMod
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
                    ModUpdatesViewData.NewMods.Add(workshopMod);
                    count++;
                }
            }
            if (count > 0)
            {
                ModUpdatesViewData.SelectAll(true);
                DivinityApp.Log($"'{count}' mod updates pending.");
            }
            ModUpdatesViewData.OnLoaded?.Invoke();
            IsRefreshingWorkshop = false;
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
                        appDataFolder = Path.Combine(userFolder, "AppData", "Local", "Larian Studios");
                    }
                }
                else
                {
                    appDataFolder = Path.Combine(appDataFolder, "Larian Studios");
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

                string gameDataFolder = Path.Combine(localAppDataFolder, AppSettings.DefaultPathways.DocumentsGameFolder);

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
                        localAppDataFolder = Path.Combine(userFolder, "AppData", "Local");
                        gameDataFolder = Path.Combine(localAppDataFolder, AppSettings.DefaultPathways.DocumentsGameFolder);
                    }
                }

                string modPakFolder = Path.Combine(gameDataFolder, "Mods");
                string gmCampaignsFolder = Path.Combine(gameDataFolder, "GMCampaigns");
                string profileFolder = Path.Combine(gameDataFolder, "PlayerProfiles");

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
                    ShowAlert("Failed to find %LOCALAPPDATA% folder. This is weird.", AlertType.Danger);
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
                        }
                        else
                        {
                            DivinityApp.Log($"Failed to find game data path at '{gameDataPath}'.");
                        }
                    }
                }
                else
                {
                    string installPath = Path.GetFullPath(Path.Combine(Settings.GameDataPath, @"..\..\"));
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

                    if (dialog.ShowDialog(View.Main) == true)
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
                            ShowAlert("Failed to find Data folder with given installation directory.", AlertType.Danger);
                        }
                        if (File.Exists(exePath))
                        {
                            Settings.GameExecutablePath = exePath;
                        }
                        PathwayData.InstallPath = dialog.SelectedPath;
                    }
                }

                if (AppSettings.FeatureEnabled("ScriptExtender"))
                {
                    LoadExtenderSettings();
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
            foreach (var m in loadedMods)
            {
                if (m.IsLarianMod)
                {
                    var existingIgnoredMod = DivinityApp.IgnoredMods.FirstOrDefault(x => x.UUID == m.UUID);
                    if (existingIgnoredMod != null && existingIgnoredMod != m)
                    {
                        DivinityApp.IgnoredMods.Remove(existingIgnoredMod);
                    }
                    DivinityApp.IgnoredMods.Add(m);
                }
                if (TryGetMod(m.UUID, out var existingMod))
                {
                    if (m.Version.VersionInt > existingMod.Version.VersionInt)
                    {
                        mods.AddOrUpdate(m);
                        DivinityApp.Log($"Updated mod data from pak: Name({m.Name}) UUID({m.UUID}) Type({m.ModType}) Version({m.Version.VersionInt})");
                    }
                }
                else
                {
                    mods.AddOrUpdate(m);
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
                    if (existing.Version.VersionInt < mod.Version.VersionInt)
                    {
                        finalMods.Remove(existing);
                        finalMods.Add(existing);
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
            List<DivinityModData> finalMods = new List<DivinityModData>();
            List<DivinityModData> modPakData = null;
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
            if (baseMods == null)
            {
                baseMods = new List<DivinityModData>();
            }

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
                modPakData = await RunTask(DivinityModDataLoader.LoadModPackageDataAsync(PathwayData.AppDataModsPath, cancelTokenSource.Token), null);
                cancelTokenSource = GetCancellationToken(int.MaxValue);
                await IncreaseMainProgressValueAsync(taskStepAmount);
            }

            if (baseMods != null) MergeModLists(finalMods, baseMods);
            if (modPakData != null) MergeModLists(finalMods, modPakData);
            if (projects != null) MergeModLists(finalMods, projects);

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

                List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();

                DivinityLoadOrder currentOrder = new DivinityLoadOrder() { Name = "Current", FilePath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx"), IsModSettings = true };

                if (this.SelectedModOrder != null && this.SelectedModOrder.IsModSettings)
                {
                    currentOrder.SetOrder(this.SelectedModOrder);
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
							else
							{
								LoadModOrder(nextOrder, missingMods);
							}*/

                            //Adds mods that will always be "enabled"
                            //ForceLoadedMods.AddRange(Mods.Where(x => !x.IsActive && x.IsForceLoaded));

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

        private async Task<ImportOperationResults> AddModFromFile(string filePath, CancellationToken t, bool toActiveList = false)
        {
            var result = new ImportOperationResults();
            if (Path.GetExtension(filePath).Equals(".pak", StringComparison.OrdinalIgnoreCase))
            {
                var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
                var outputFilePath = Path.Combine(PathwayData.AppDataModsPath, Path.GetFileName(filePath));
                try
                {
                    using (System.IO.FileStream sourceStream = File.Open(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, 8192, true))
                    {
                        using (System.IO.FileStream destinationStream = File.Create(outputFilePath))
                        {
                            await sourceStream.CopyToAsync(destinationStream, 8192, t);
                            result.Success = true;
                        }
                    }

                    if (result.Success)
                    {
                        var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, builtinMods, t);
                        if (mod != null)
                        {
                            result.Mods.Add(mod);
                            await Observable.Start(() =>
                            {
                                AddImportedMod(mod, toActiveList);
                                return Unit.Default;
                            }, RxApp.MainThreadScheduler);
                        }
                    }
                }
                catch (System.IO.IOException ex)
                {
                    DivinityApp.Log($"File may be in use by another process:\n{ex}");
                    ShowAlert($"Failed to copy file '{Path.GetFileName(filePath)}. It may be locked by another process.'", AlertType.Danger);
                }
                catch (Exception ex)
                {
                    DivinityApp.Log($"Error reading file ({filePath}):\n{ex}");
                }
            }
            else
            {
                result.Success = await ImportOrderZipFileAsync(filePath, true, t, toActiveList);
            }
            return result;
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
                RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
                {
                    MainProgressToken = new CancellationTokenSource();
                    int successes = 0;
                    int total = 0;
                    foreach (var f in files)
                    {
                        total++;
                        var result = await AddModFromFile(f, MainProgressToken.Token, toActiveList);
                        if (result.Success)
                        {
                            successes++;
                        }
                    }

                    await ctrl.Yield();
                    RxApp.MainThreadScheduler.Schedule(_ =>
                    {
                        IsRefreshing = false;
                        OnMainProgressComplete();
                        if (successes == total)
                        {
                            if (total > 1)
                            {
                                ShowAlert($"Successfully imported {total} mods.", AlertType.Success, 20);
                            }
                            else if (total == 1)
                            {
                                ShowAlert($"Successfully imported '{files.First()}'.", AlertType.Success, 20);
                            }
                            else
                            {
                                ShowAlert("Skipped importing mod.", AlertType.Success, 20);
                            }
                        }
                        else
                        {
                            //view.AlertBar.SetDangerAlert($"Only imported {successes}/{total} mods. Check the log.", 20);
                        }
                    });
                    return Disposable.Empty;
                });
            }
        }

        private string GetInitialStartingDirectory(string fallback = "")
        {
            var directory = fallback;

            if (!String.IsNullOrEmpty(PathwayData.LastSaveFilePath) && DivinityFileUtils.TryGetDirectoryOrParent(PathwayData.LastSaveFilePath, out var lastDir))
            {
                directory = lastDir;
            }

            if (String.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                directory = DivinityApp.GetAppDirectory();
            }

            return directory;
        }

        private void OpenModImportDialog()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".zip",
                Filter = "All formats (*.pak;*.zip;*.7z)|*.pak;*.zip;*.7z;*.7zip;*.tar;*.bzip2;*.gzip;*.lzip|Mod package (*.pak)|*.pak|Archive file (*.zip;*.7z)|*.zip;*.7z;*.7zip;*.tar;*.bzip2;*.gzip;*.lzip|All files (*.*)|*.*",
                Title = "Import Mods from Archive...",
                ValidateNames = true,
                ReadOnlyChecked = true,
                Multiselect = true,
                InitialDirectory = GetInitialStartingDirectory()
            };

            if (dialog.ShowDialog(View) == true)
            {
                PathwayData.LastSaveFilePath = Path.GetDirectoryName(dialog.FileName);
                ImportMods(dialog.FileNames);
            }
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
                    newOrder.FilePath = Path.Combine(Settings.LoadOrderPath, DivinityModDataLoader.MakeSafeFilename(Path.Combine(newOrder.Name + ".json"), '_'));
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
            Dictionary<string, DivinityMissingModData> missingMods = new Dictionary<string, DivinityMissingModData>();
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

        private void CheckModForExtenderData(DivinityModData mod)
        {
            if (mod.ScriptExtenderData != null && mod.ScriptExtenderData.HasAnySettings)
            {
                // Assume an Lua-only mod actually requires the extender, otherwise functionality is limited.
                bool onlyUsesLua = mod.ScriptExtenderData.FeatureFlags.Contains("Lua") && !mod.ScriptExtenderData.FeatureFlags.Contains("Osiris");

                if (!mod.ScriptExtenderData.FeatureFlags.Contains("Preprocessor") || onlyUsesLua)
                {
                    if (!Settings.ExtenderSettings.EnableExtensions)
                    {
                        mod.ExtenderModStatus = DivinityExtenderModStatus.REQUIRED_DISABLED;
                    }
                    else
                    {
                        if (Settings.ExtenderSettings != null && Settings.ExtenderSettings.ExtenderVersion > -1 && Settings.ExtenderSettings.ExtenderUpdaterIsAvailable)
                        {
                            if (mod.ScriptExtenderData.RequiredExtensionVersion > -1 && Settings.ExtenderSettings.ExtenderVersion < mod.ScriptExtenderData.RequiredExtensionVersion)
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

        private void CheckExtenderData()
        {
            if (Settings != null && Mods.Count > 0)
            {
                DivinityModData.CurrentExtenderVersion = Settings.ExtenderSettings.ExtenderVersion;
                foreach (var mod in Mods)
                {
                    CheckModForExtenderData(mod);
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

        private readonly List<string> ignoredModProjectNames = new List<string> { "Test", "Debug" };
        private bool CanFetchWorkshopData(DivinityModData mod)
        {
            if (CachedWorkshopData.NonWorkshopMods.Contains(mod.UUID))
            {
                return false;
            }
            if (mod.IsEditorMod && (ignoredModProjectNames.Any(x => mod.Folder.IndexOf(x, StringComparison.OrdinalIgnoreCase) > -1) ||
                String.IsNullOrEmpty(mod.Author) || String.IsNullOrEmpty(mod.Description)))
            {
                return false;
            }
            else if (mod.Author == "Larian" || String.IsNullOrEmpty(mod.DisplayName))
            {
                return false;
            }
            return String.IsNullOrEmpty(mod.WorkshopData.ID) || !CachedWorkshopData.Mods.Any(x => x.UUID == mod.UUID);
        }

        private async Task<bool> CacheAllWorkshopModsAsync()
        {
            var success = await DivinityWorkshopDataLoader.GetAllWorkshopDataAsync(CachedWorkshopData, AppSettings.DefaultPathways.Steam.AppID);
            if (success)
            {
                var cachedGUIDs = CachedWorkshopData.Mods.Select(x => x.UUID).ToHashSet();
                var nonWorkshopMods = UserMods.Where(x => !cachedGUIDs.Contains(x.UUID)).ToList();
                if (nonWorkshopMods.Count > 0)
                {
                    foreach (var m in nonWorkshopMods)
                    {
                        CachedWorkshopData.AddNonWorkshopMod(m.UUID);
                    }
                }
            }
            return success;
        }


        private void UpdateModDataWithCachedData()
        {
            foreach (var mod in UserMods)
            {
                var cachedMods = CachedWorkshopData.Mods.Where(x => x.UUID == mod.UUID);
                if (cachedMods != null)
                {
                    foreach (var cachedMod in cachedMods)
                    {
                        if (String.IsNullOrEmpty(mod.WorkshopData.ID) || mod.WorkshopData.ID == cachedMod.WorkshopID)
                        {
                            mod.WorkshopData.ID = cachedMod.WorkshopID;
                            mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(cachedMod.Created);
                            mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(cachedMod.LastUpdated);
                            mod.WorkshopData.Tags = cachedMod.Tags;
                            mod.AddTags(cachedMod.Tags);
                            if (cachedMod.LastUpdated > 0)
                            {
                                mod.LastUpdated = mod.WorkshopData.UpdatedDate;
                            }
                        }
                    }
                }
            }
        }

        private void LoadWorkshopModDataBackground()
        {
            bool workshopCacheFound = false;
            IsRefreshingWorkshop = true;

            RxApp.TaskpoolScheduler.ScheduleAsync((Func<IScheduler, CancellationToken, Task>)(async (s, token) =>
            {
                workshopModLoadingCancelToken = token;
                var loadedWorkshopMods = await LoadWorkshopModsAsync(workshopModLoadingCancelToken);
                await Observable.Start(() =>
                {
                    workshopMods.AddOrUpdate(loadedWorkshopMods);
                    DivinityApp.Log($"Loaded '{workshopMods.Count}' workshop mods from '{Settings.WorkshopPath}'.");
                    if (!workshopModLoadingCancelToken.IsCancellationRequested)
                    {
                        CheckForModUpdates(workshopModLoadingCancelToken);
                    }
                    return Unit.Default;
                }, RxApp.MainThreadScheduler);

                var filePath = DivinityApp.GetAppDirectory("Data", "workshopdata.json");

                if (File.Exists(filePath))
                {
                    DivinityModManagerCachedWorkshopData cachedData = DivinityJsonUtils.SafeDeserializeFromPath<DivinityModManagerCachedWorkshopData>(filePath);
                    if (cachedData != null)
                    {
                        CachedWorkshopData = cachedData;
                        if (String.IsNullOrEmpty(CachedWorkshopData.LastVersion) || CachedWorkshopData.LastVersion != this.Version)
                        {
                            CachedWorkshopData.LastUpdated = -1;
                        }
                        UpdateModDataWithCachedData();
                        workshopCacheFound = true;
                    }
                }

                if (!Settings.DisableWorkshopTagCheck)
                {
                    if (!workshopCacheFound || CachedWorkshopData.LastUpdated == -1 || (DateTimeOffset.Now.ToUnixTimeSeconds() - CachedWorkshopData.LastUpdated >= 3600))
                    {
                        RxApp.MainThreadScheduler.Schedule(() =>
                        {
                            StatusBarRightText = "Downloading workshop data...";
                            StatusBarBusyIndicatorVisibility = Visibility.Visible;
                        });
                        bool success = await CacheAllWorkshopModsAsync();
                        if (success)
                        {
                            UpdateModDataWithCachedData();
                            CachedWorkshopData.LastUpdated = DateTimeOffset.Now.ToUnixTimeSeconds();
                        }
                    }
                    else
                    {
                        DivinityApp.Log("Checking for mods missing workshop data.");
                        var targetMods = UserMods.Where(x => CanFetchWorkshopData(x)).ToList();
                        if (targetMods.Count > 0)
                        {
                            RxApp.MainThreadScheduler.Schedule(() =>
                            {
                                StatusBarRightText = $"Downloading workshop data for {targetMods.Count} mods...";
                                StatusBarBusyIndicatorVisibility = Visibility.Visible;
                            });
                            var totalSuccesses = await DivinityWorkshopDataLoader.FindWorkshopDataAsync(targetMods, CachedWorkshopData, AppSettings.DefaultPathways.Steam.AppID);
                            if (totalSuccesses > 0)
                            {
                                UpdateModDataWithCachedData();
                            }
                        }
                    }

                    CachedWorkshopData.LastVersion = this.Version;

                    RxApp.MainThreadScheduler.Schedule(() =>
                    {
                        StatusBarRightText = "";
                        StatusBarBusyIndicatorVisibility = Visibility.Collapsed;
                        string updateMessage = !CachedWorkshopData.CacheUpdated ? "cached " : "";
                        ShowAlert($"Loaded {updateMessage}workshop data ({CachedWorkshopData.Mods.Count} mods).", AlertType.Success, 60);
                    });

                    if (CachedWorkshopData.CacheUpdated)
                    {
                        await DivinityFileUtils.WriteFileAsync(filePath, CachedWorkshopData.Serialize());
                        CachedWorkshopData.CacheUpdated = false;
                    }
                }
            }));
        }

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
                        ShowAlert("Game Data folder is not valid. Please set it in the preferences window and refresh.", AlertType.Danger);
                        View.OpenPreferences(false, true);
                    }
                    return Unit.Default;
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

                OnMainProgressComplete();
                OnRefreshed?.Invoke(this, new EventArgs());

                if (AppSettings.FeatureEnabled("ScriptExtender"))
                {
                    if (IsInitialized)
                    {
                        DivinityApp.Log($"Loading extender settings.");
                        LoadExtenderSettings();
                    }
                    else
                    {
                        DivinityApp.Log($"Checking extender data.");
                        CheckExtenderData();
                    }
                }

                IsRefreshing = false;
                IsLoadingOrder = false;
                IsInitialized = true;

                if (AppSettings.FeatureEnabled("Workshop"))
                {
                    LoadWorkshopModDataBackground();
                }

                return Unit.Default;
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
                string outputName = DivinityModDataLoader.MakeSafeFilename(Path.Combine(SelectedModOrder.Name + ".json"), '_');

                if (String.IsNullOrWhiteSpace(SelectedModOrder.FilePath))
                {
                    SelectedModOrder.FilePath = Path.Combine(Settings.LoadOrderPath, outputName);
                    outputPath = SelectedModOrder.FilePath;
                }

                try
                {
                    if (SelectedModOrder.IsModSettings)
                    {
                        //When saving the "Current" order, write this to modsettings.lsx instead of a json file.
                        result = await ExportLoadOrderAsync();
                        outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
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
            var startDirectory = Path.GetFullPath(!String.IsNullOrEmpty(Settings.LoadOrderPath) ? Settings.LoadOrderPath : GetInitialStartingDirectory(Directory.GetCurrentDirectory()));

            if (!Directory.Exists(startDirectory))
            {
                Directory.CreateDirectory(startDirectory);
            }

            var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = ".json",
                Filter = "JSON file (*.json)|*.json",
                InitialDirectory = startDirectory
            };

            string outputName = Path.Combine(SelectedModOrder.Name + ".json");
            if (SelectedModOrder.IsModSettings)
            {
                outputName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}.json";
            }

            //dialog.RestoreDirectory = true;
            dialog.FileName = DivinityModDataLoader.MakeSafeFilename(outputName, '_');
            dialog.CheckFileExists = false;
            dialog.CheckPathExists = false;
            dialog.OverwritePrompt = true;
            dialog.Title = "Save Load Order As...";

            if (dialog.ShowDialog(View) == true)
            {
                // Save mods that aren't missing
                var tempOrder = new DivinityLoadOrder
                {
                    Name = SelectedModOrder.Name,
                };
                tempOrder.Order.AddRange(SelectedModOrder.Order.Where(x => Mods.Any(y => y.UUID == x.UUID)));
                bool result = false;
                if (SelectedModOrder.IsModSettings)
                {
                    tempOrder.Name = $"Current ({SelectedProfile.Name})";
                    result = DivinityModDataLoader.ExportLoadOrderToFile(dialog.FileName, tempOrder);
                }
                else
                {
                    result = DivinityModDataLoader.ExportLoadOrderToFile(dialog.FileName, tempOrder);
                }

                if (result)
                {
                    ShowAlert($"Saved mod load order to '{dialog.FileName}'", AlertType.Success, 10);
                    foreach (var order in this.ModOrderList)
                    {
                        if (order.FilePath == dialog.FileName)
                        {
                            order.SetOrder(tempOrder);
                            DivinityApp.Log($"Updated saved order '{order.Name}' from '{dialog.FileName}'.");
                        }
                    }
                }
                else
                {
                    ShowAlert($"Failed to save mod load order to '{dialog.FileName}'", AlertType.Danger);
                }
            }
        }

        private void DisplayMissingMods(DivinityLoadOrder order = null)
        {
            bool displayExtenderModWarning = false;

            if (order == null) order = SelectedModOrder;
            if (order != null && Settings?.DisableMissingModWarnings != true)
            {
                List<DivinityMissingModData> missingMods = new List<DivinityMissingModData>();

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

            if (Settings?.DisableMissingModWarnings != true && displayExtenderModWarning && AppSettings.FeatureEnabled("ScriptExtender"))
            {
                //DivinityApp.LogMessage($"Mod Order: {String.Join("\n", order.Order.Select(x => x.Name))}");
                DivinityApp.Log("Checking mods for extender requirements.");
                List<DivinityMissingModData> extenderRequiredMods = new List<DivinityMissingModData>();
                for (int i = 0; i < order.Order.Count; i++)
                {
                    var entry = order.Order[i];
                    var mod = ActiveMods.FirstOrDefault(m => m.UUID == entry.UUID);
                    if (mod != null)
                    {
                        if (mod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED || mod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
                        {
                            DivinityApp.Log($"{mod.Name} | ExtenderModStatus: {mod.ExtenderModStatus}");
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
                                        if (dependencyMod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED || dependencyMod.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING)
                                        {
                                            DivinityApp.Log($"{mod.Name} | ExtenderModStatus: {mod.ExtenderModStatus}");
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
                    View.MainWindowMessageBox_OK.ShowMessageBox(String.Join("\n", extenderRequiredMods.OrderBy(x => x.Index)),
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

        private async Task<bool> ExportLoadOrderAsync()
        {
            if (!Settings.GameMasterModeEnabled)
            {
                if (SelectedProfile != null && SelectedModOrder != null)
                {
                    string outputPath = Path.Combine(SelectedProfile.Folder, "modsettings.lsx");
                    var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, mods.Items, Settings.AutoAddDependenciesWhenExporting, SelectedAdventureMod);
                    var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, finalOrder);

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
                                DivinityApp.Log($"Set active profile to '{SelectedProfile.Name}'.");
                            }
                            else
                            {
                                DivinityApp.Log($"Could not set active profile to '{SelectedProfile.Name}'.");
                            }

                            //Update "Current" order
                            if (!SelectedModOrder.IsModSettings)
                            {
                                this.ModOrderList.First(x => x.IsModSettings)?.SetOrder(SelectedModOrder.Order);
                            }

                            List<string> orderList = new List<string>();
                            if (SelectedAdventureMod != null) orderList.Add(SelectedAdventureMod.UUID);
                            orderList.AddRange(SelectedModOrder.Order.Select(x => x.UUID));

                            SelectedProfile.ModOrder.Clear();
                            SelectedProfile.ModOrder.AddRange(orderList);
                            SelectedProfile.ActiveMods.Clear();
                            SelectedProfile.ActiveMods.AddRange(orderList.Select(x => ProfileActiveModDataFromUUID(x)));
                            DisplayMissingMods(SelectedModOrder);

                            return Unit.Default;
                        }, RxApp.MainThreadScheduler);
                        return true;
                    }
                    else
                    {
                        await Observable.Start((Func<Unit>)(() =>
                        {
                            string msg = $"Problem exporting load order to '{outputPath}'. Is the file locked?";
                            ShowAlert(msg, AlertType.Danger);
                            this.View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
                            this.View.MainWindowMessageBox_OK.Closed += this.MainWindowMessageBox_Closed_ResetColor;
                            this.View.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
                            return Unit.Default;
                        }), RxApp.MainThreadScheduler);
                    }
                }
                else
                {
                    await Observable.Start(() =>
                    {
                        ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order.", AlertType.Danger);
                        return Unit.Default;
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
                            await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.Folder, finalOrder);

                            await Observable.Start(() =>
                            {
                                ShowAlert($"Exported load order to '{SelectedGameMasterCampaign.FilePath}'", AlertType.Success, 15);

                                if (DivinityModDataLoader.ExportedSelectedProfile(PathwayData.AppDataProfilesPath, SelectedProfile.UUID))
                                {
                                    DivinityApp.Log($"Set active profile to '{SelectedProfile.Name}'.");
                                }
                                else
                                {
                                    DivinityApp.Log($"Could not set active profile to '{SelectedProfile.Name}'.");
                                }

                                //Update the campaign's saved dependencies
                                SelectedGameMasterCampaign.Dependencies.Clear();
                                SelectedGameMasterCampaign.Dependencies.AddRange(finalOrder.Select(x => DivinityModDependencyData.FromModData(x)));

                                List<string> orderList = new List<string>();
                                if (SelectedAdventureMod != null) orderList.Add(SelectedAdventureMod.UUID);
                                orderList.AddRange(SelectedModOrder.Order.Select(x => x.UUID));

                                SelectedProfile.ModOrder.Clear();
                                SelectedProfile.ModOrder.AddRange(orderList);
                                SelectedProfile.ActiveMods.Clear();
                                SelectedProfile.ActiveMods.AddRange(orderList.Select(x => ProfileActiveModDataFromUUID(x)));
                                DisplayMissingMods(SelectedModOrder);

                                return Unit.Default;
                            }, RxApp.MainThreadScheduler);
                            return true;
                        }
                        else
                        {
                            await Observable.Start((Func<Unit>)(() =>
                            {
                                string msg = $"Problem exporting load order to '{SelectedGameMasterCampaign.FilePath}'";
                                ShowAlert(msg, AlertType.Danger);
                                this.View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
                                this.View.MainWindowMessageBox_OK.Closed += this.MainWindowMessageBox_Closed_ResetColor;
                                this.View.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
                                return Unit.Default;
                            }), RxApp.MainThreadScheduler);
                        }
                    }
                }
                else
                {
                    await Observable.Start(() =>
                    {
                        ShowAlert("SelectedGameMasterCampaign is null! Failed to export mod order.", AlertType.Danger);
                        return Unit.Default;
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

        private static readonly ArchiveEncoding _archiveEncoding = new ArchiveEncoding(Encoding.UTF8, Encoding.UTF8);
        private static readonly ReaderOptions _importReaderOptions = new ReaderOptions { ArchiveEncoding = _archiveEncoding };
        private static readonly WriterOptions _exportWriterOptions = new WriterOptions(CompressionType.Deflate) { ArchiveEncoding = _archiveEncoding };

        //TODO: Extract zip mods to the Mods folder, possibly import a load order if a json exists.
        private void ImportOrderFromArchive()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                DefaultExt = ".zip",
                Filter = "Archive file (*.zip;*.7z)|*.zip;*.7z;*.7zip;*.tar;*.bzip2;*.gzip;*.lzip|All files (*.*)|*.*",
                Title = "Import Mods from Archive...",
                ValidateNames = true,
                ReadOnlyChecked = true,
                Multiselect = false,
                InitialDirectory = GetInitialStartingDirectory()
            };

            if (dialog.ShowDialog(View) == true)
            {
                //if(!Path.GetExtension(dialog.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
                //{
                //	view.AlertBar.SetDangerAlert($"Currently only .zip format archives are supported.", -1);
                //	return;
                //}
                MainProgressTitle = $"Importing mods from '{dialog.FileName}'.";
                MainProgressWorkText = "";
                MainProgressValue = 0d;
                MainProgressIsActive = true;
                RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
                {
                    MainProgressToken = new CancellationTokenSource();
                    bool success = await ImportOrderZipFileAsync(dialog.FileName, false, MainProgressToken.Token);
                    await ctrl.Yield();
                    RxApp.MainThreadScheduler.Schedule(_ =>
                    {
                        OnMainProgressComplete();
                        if (success)
                        {
                            ShowAlert($"Successfully extracted archive.", AlertType.Success, 20);
                        }
                    });
                    return Disposable.Empty;
                });
            }
        }

        private void AddImportedMod(DivinityModData mod, bool toActiveList = false)
        {
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
            CheckModForExtenderData(mod);
            DivinityApp.Log($"Imported Mod: {mod}");
        }

        private async Task<bool> ImportSevenZipArchiveAsync(string outputDirectory, System.IO.Stream stream,
            Dictionary<string, string> jsonFiles, CancellationToken t, bool toActiveList = false)
        {
            int successes = 0;
            int total = 0;
            stream.Position = 0;
            var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
            using (var archiveStream = SevenZipArchive.Open(stream, _importReaderOptions))
            {
                foreach (var entry in archiveStream.Entries)
                {
                    if (t.IsCancellationRequested) return false;
                    if (!entry.IsDirectory)
                    {
                        if (entry.Key.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                        {
                            total += 1;
                            string outputFilePath = Path.Combine(outputDirectory, entry.Key);
                            var success = false;
                            using (var entryStream = entry.OpenEntryStream())
                            {
                                using (var fs = File.Create(outputFilePath, 4096, System.IO.FileOptions.Asynchronous))
                                {
                                    try
                                    {
                                        await entryStream.CopyToAsync(fs, 4096, MainProgressToken.Token);
                                        success = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        DivinityApp.Log($"Error copying file '{entry.Key}' from archive to '{outputFilePath}':\n{ex}");
                                    }
                                }
                            }
                            if (success)
                            {
                                var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, builtinMods, t);
                                if (mod != null)
                                {
                                    successes += 1;
                                    await Observable.Start(() =>
                                    {
                                        AddImportedMod(mod, toActiveList);
                                        return Unit.Default;
                                    }, RxApp.MainThreadScheduler);
                                }
                            }
                        }
                        else if (entry.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            total += 1;
                            using (var entryStream = entry.OpenEntryStream())
                            {
                                try
                                {
                                    int length = (int)entry.CompressedSize;
                                    var result = new byte[length];
                                    await entryStream.ReadAsync(result, 0, length);
                                    string text = Encoding.UTF8.GetString(result);
                                    if (!String.IsNullOrWhiteSpace(text))
                                    {
                                        jsonFiles.Add(Path.GetFileNameWithoutExtension(entry.Key), text);
                                    }
                                    successes += 1;
                                }
                                catch (Exception ex)
                                {
                                    DivinityApp.Log($"Error reading json file '{entry.Key}' from archive:\n{ex}");
                                }
                            }
                        }
                    }
                }
            }
            return successes >= total;
        }

        private async Task<bool> ImportGenericArchiveAsync(string outputDirectory, System.IO.Stream stream,
            Dictionary<string, string> jsonFiles, CancellationToken t, bool toActiveList = false)
        {
            int successes = 0;
            int total = 0;
            stream.Position = 0;
            var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
            using (var reader = ReaderFactory.Open(stream, _importReaderOptions))
            {
                while (reader.MoveToNextEntry())
                {
                    if (t.IsCancellationRequested) return false;
                    if (!reader.Entry.IsDirectory)
                    {
                        if (reader.Entry.Key.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
                        {
                            total += 1;
                            string outputFilePath = Path.Combine(outputDirectory, reader.Entry.Key);
                            bool success = false;
                            using (var entryStream = reader.OpenEntryStream())
                            {
                                using (var fs = File.Create(outputFilePath, 4096, System.IO.FileOptions.Asynchronous))
                                {
                                    try
                                    {
                                        await entryStream.CopyToAsync(fs, 4096, MainProgressToken.Token);
                                        success = true;
                                        successes += 1;
                                    }
                                    catch (Exception ex)
                                    {
                                        DivinityApp.Log($"Error copying file '{reader.Entry.Key}' from archive to '{outputFilePath}':\n{ex}");
                                    }
                                }
                            }

                            if (success)
                            {
                                var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, builtinMods, t);
                                if (mod != null)
                                {
                                    successes += 1;
                                    await Observable.Start(() =>
                                    {
                                        AddImportedMod(mod, toActiveList);
                                        return Unit.Default;
                                    }, RxApp.MainThreadScheduler);
                                }
                            }
                        }
                        else if (reader.Entry.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            total += 1;
                            using (var entryStream = reader.OpenEntryStream())
                            {
                                try
                                {
                                    int length = (int)reader.Entry.CompressedSize;
                                    var result = new byte[length];
                                    await entryStream.ReadAsync(result, 0, length);
                                    string text = Encoding.UTF8.GetString(result);
                                    if (!String.IsNullOrWhiteSpace(text))
                                    {
                                        jsonFiles.Add(Path.GetFileNameWithoutExtension(reader.Entry.Key), text);
                                    }
                                    successes += 1;
                                }
                                catch (Exception ex)
                                {
                                    DivinityApp.Log($"Error reading json file '{reader.Entry.Key}' from archive:\n{ex}");
                                }
                            }
                        }
                    }
                }
            }
            return successes >= total;
        }

        private async Task<bool> ImportOrderZipFileAsync(string archivePath, bool onlyMods, CancellationToken t, bool toActiveList = false)
        {
            System.IO.FileStream fileStream = null;
            string outputDirectory = PathwayData.AppDataModsPath;
            double taskStepAmount = 1.0 / 4;
            bool success = false;
            var jsonFiles = new Dictionary<string, string>();
            try
            {
                fileStream = File.Open(archivePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, 4096, true);
                if (fileStream != null)
                {
                    await fileStream.ReadAsync(new byte[fileStream.Length], 0, (int)fileStream.Length);
                    IncreaseMainProgressValue(taskStepAmount);

                    var extension = Path.GetExtension(archivePath);
                    if (extension.Equals(".7z", StringComparison.OrdinalIgnoreCase) || extension.Equals(".7zip", StringComparison.OrdinalIgnoreCase))
                    {
                        success = await ImportSevenZipArchiveAsync(outputDirectory, fileStream, jsonFiles, t, toActiveList);
                    }
                    else
                    {
                        success = await ImportGenericArchiveAsync(outputDirectory, fileStream, jsonFiles, t, toActiveList);
                    }

                    IncreaseMainProgressValue(taskStepAmount);
                }
            }
            catch (Exception ex)
            {
                DivinityApp.Log($"Error extracting package: {ex}");
                RxApp.MainThreadScheduler.Schedule(_ =>
                {
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

        private void ExportLoadOrderToArchive_Start()
        {
            //view.MainWindowMessageBox.Text = "Add active mods to a zip file?";
            //view.MainWindowMessageBox.Caption = "Depending on the number of mods, this may take some time.";
            MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(View, $"Save active mods to a zip file?{Environment.NewLine}Depending on the number of mods, this may take some time.", "Confirm Archive Creation",
                MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.Cancel, View.MainWindowMessageBox_OK.Style);
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
                    if (!Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
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
                            using (var swriter = new System.IO.StreamWriter(ms))
                            {
                                await swriter.WriteAsync(contents);
                                swriter.Flush();
                                ms.Position = 0;
                                zipWriter.Write(orderFileName, ms);
                            }
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

                                if (await DivinityFileUtils.CreatePackageAsync(gameDataFolder, sourceFolders, outputPackage, DivinityFileUtils.IgnoredPackageFiles, t))
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
                        ShowAlert($"Exported load order to '{outputPath}'.", AlertType.Success, 15);
                        var dir = Path.GetFullPath(Path.GetDirectoryName(outputPath));
                        if (Directory.Exists(dir))
                        {
                            DivinityFileUtils.TryOpenPath(dir);
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
                    ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order.", AlertType.Danger);
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

                if (dialog.ShowDialog(View) == true)
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
                ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order.", AlertType.Danger);
            }

        }

        private string ModToTSVLine(DivinityModData mod)
        {
            var index = mod.Index.ToString();
            if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
            {
                index = "Override";
            }
            if (WorkshopSupportEnabled)
            {
                return $"{index}\t{mod.Name}\t{mod.Author}\t{mod.OutputPakName}\t{String.Join(", ", mod.Tags)}\t{String.Join(", ", mod.Dependencies.Items.Select(y => y.Name))}\t{mod.GetURL()}";
            }
            return $"{index}\t{mod.Name}\t{mod.Author}\t{mod.OutputPakName}\t{String.Join(", ", mod.Tags)}\t{String.Join(", ", mod.Dependencies.Items.Select(y => y.Name))}";
        }

        private string ModToTextLine(DivinityModData mod)
        {
            var index = mod.Index.ToString() + ".";
            if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
            {
                index = "Override";
            }
            if (WorkshopSupportEnabled)
            {
                return $"{index} {mod.Name} ({mod.OutputPakName}) {mod.GetURL()}";
            }
            return $"{index} {mod.Name} ({mod.OutputPakName})";
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

                if (dialog.ShowDialog(View) == true)
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
                        if (WorkshopSupportEnabled)
                        {
                            outputText = "Index\tName\tAuthor\tFileName\tTags\tDependencies\tURL\n";
                            outputText += String.Join("\n", exportMods.Select(ModToTSVLine));
                        }
                        else
                        {
                            outputText = "Index\tName\tAuthor\tFileName\tTags\tDependencies\n";
                            outputText += String.Join("\n", exportMods.Select(ModToTSVLine));
                        }
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
                ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order.", AlertType.Danger);
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

            if (SelectedProfile != null)
            {
                string profilePath = Path.GetFullPath(Path.Combine(SelectedProfile.Folder, "Savegames"));
                string storyPath = Path.Combine(profilePath, "Story");
                if (Directory.Exists(storyPath))
                {
                    dialog.InitialDirectory = storyPath;
                }
                else
                {
                    dialog.InitialDirectory = profilePath;
                }
            }
            else
            {
                dialog.InitialDirectory = GetInitialStartingDirectory();
            }

            if (dialog.ShowDialog(View) == true)
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
                    ShowAlert($"No mod order found in save \"{Path.GetFileNameWithoutExtension(dialog.FileName)}\".", AlertType.Danger, 30);
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
                Title = "Load Mod Order From File..."
            };

            if (!String.IsNullOrEmpty(Settings.LastLoadedOrderFilePath))
            {
                if (Directory.Exists(Settings.LastLoadedOrderFilePath))
                {
                    dialog.InitialDirectory = Settings.LastLoadedOrderFilePath;
                }
                else if (File.Exists(Settings.LastLoadedOrderFilePath))
                {
                    dialog.InitialDirectory = Path.GetDirectoryName(Settings.LastLoadedOrderFilePath);
                }
            }
            else if (Directory.Exists(Settings.LoadOrderPath))
            {
                dialog.InitialDirectory = Settings.LoadOrderPath;
            }

            if (!Directory.Exists(dialog.InitialDirectory))
            {
                dialog.InitialDirectory = GetInitialStartingDirectory();
            }

            if (dialog.ShowDialog(View) == true)
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
                                DivinityApp.Log($"Successfully re-loaded order '{SelectedModOrder.Name}' with imported order.");
                            }
                            else
                            {
                                DivinityApp.Log($"Failed to load order '{SelectedModOrder.Name}'");
                            }
                        }
                        else
                        {
                            AddNewModOrder(newOrder);
                        }
                    }
                    else
                    {
                        AddNewModOrder(newOrder);
                    }
                }
                else
                {
                    DivinityApp.Log($"Failed to load order from '{dialog.FileName}'.");
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

            if (SelectedProfile != null)
            {
                string profilePath = Path.GetFullPath(Path.Combine(SelectedProfile.Folder, "Savegames"));
                string storyPath = Path.Combine(profilePath, "Story");
                if (Directory.Exists(storyPath))
                {
                    dialog.InitialDirectory = storyPath;
                }
                else
                {
                    dialog.InitialDirectory = profilePath;
                }
            }
            else
            {
                dialog.InitialDirectory = GetInitialStartingDirectory();
            }

            if (dialog.ShowDialog(View) == true)
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

                if (!Directory.Exists(dialog.InitialDirectory))
                {
                    dialog.InitialDirectory = GetInitialStartingDirectory();
                }

                if (renameDialog.ShowDialog(View) == true)
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

                            ShowAlert($"Successfully renamed '{dialog.FileName}' to '{renameDialog.FileName}'.", AlertType.Success, 15);
                        }
                        catch (Exception ex)
                        {
                            DivinityApp.Log($"Failed to rename '{dialog.FileName}' to '{renameDialog.FileName}':\n" + ex.ToString());
                        }
                    }
                    else
                    {
                        DivinityApp.Log($"Failed to rename '{dialog.FileName}' to '{renameDialog.FileName}'.");
                    }
                }
            }
        }

        public void CheckForUpdates(bool force = false)
        {
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
                AutoUpdater.ReportErrors = true;
                AutoUpdater.Start(DivinityApp.URL_UPDATE);
                Settings.LastUpdateCheck = DateTimeOffset.Now.ToUnixTimeSeconds();
                SaveSettings();
                RxApp.MainThreadScheduler.Schedule(TimeSpan.FromSeconds(10), () =>
                {
                    AutoUpdater.ReportErrors = false;
                });
            }
        }

        public void OnViewActivated(MainWindow parentView)
        {
            View = parentView;
            DivinityApp.Commands.SetViewModel(this);

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
            if (Settings.CheckForUpdates)
            {
                CheckForUpdates();
            }
            SaveSettings();

            if (loaded && Settings.SaveWindowLocation)
            {
                var window = Settings.Window;
                View.WindowStartupLocation = WindowStartupLocation.Manual;

                var screens = System.Windows.Forms.Screen.AllScreens;
                var screen = screens.FirstOrDefault();
                if (screen != null)
                {
                    if (window.Screen > -1 && window.Screen < screens.Length - 1)
                    {
                        screen = screens[window.Screen];
                    }

                    View.Left = Math.Max(screen.WorkingArea.Left, Math.Min(screen.WorkingArea.Right, screen.WorkingArea.Left + window.X));
                    View.Top = Math.Max(screen.WorkingArea.Top, Math.Min(screen.WorkingArea.Bottom, screen.WorkingArea.Top + window.Y));
                }

                if (window.Maximized)
                {
                    View.WindowState = WindowState.Maximized;
                }
            }

            Settings.Loaded = loaded;

            ModUpdatesViewVisible = ModUpdatesAvailable = false;
            MainProgressTitle = "Loading...";
            MainProgressValue = 0d;
            CanCancelProgress = false;
            MainProgressIsActive = true;
            View.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
            View.TaskbarItemInfo.ProgressValue = 0;
            IsRefreshing = true;
            RxApp.TaskpoolScheduler.ScheduleAsync(RefreshAsync);
        }

        public bool AutoChangedOrder { get; set; }
        public ViewModelActivator Activator { get; }

        private readonly Regex filterPropertyPattern = new Regex("@([^\\s]+?)([\\s]+)([^@\\s]*)");
        private readonly Regex filterPropertyPatternWithQuotes = new Regex("@([^\\s]+?)([\\s\"]+)([^@\"]*)");

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

        private Unit DeleteOrder(DivinityLoadOrder order)
        {
            MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(View, $"Delete load order '{order.Name}'? This cannot be undone.", "Confirm Order Deletion",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, View.MainWindowMessageBox_OK.Style);
            if (result == MessageBoxResult.Yes)
            {
                SelectedModOrderIndex = 0;
                this.ModOrderList.Remove(order);
                if (!String.IsNullOrEmpty(order.FilePath) && File.Exists(order.FilePath))
                {
                    RecycleBinHelper.DeleteFile(order.FilePath, false, false);
                    ShowAlert($"Sent load order '{order.FilePath}' to the recycle bin.", AlertType.Warning, 25);
                }
            }
            return Unit.Default;
        }

        private void DeleteMods(List<DivinityModData> targetMods)
        {
            if (!IsDeletingFiles)
            {
                var targetUUIDs = targetMods.Select(x => x.UUID).ToHashSet();

                var deleteFilesData = targetMods.Select(x => ModFileDeletionData.FromMod(x));
                this.View.DeleteFilesView.ViewModel.Files.AddRange(deleteFilesData);

                var workshopMods = WorkshopMods.Where(wm => targetUUIDs.Contains(wm.UUID) && File.Exists(wm.FilePath)).Select(x => ModFileDeletionData.FromMod(x, true));
                this.View.DeleteFilesView.ViewModel.Files.AddRange(workshopMods);

                this.View.DeleteFilesView.ViewModel.IsVisible = true;
            }
        }

        public void DeleteMod(DivinityModData mod)
        {
            DeleteMods(new List<DivinityModData>() { mod });
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
                Description = "Select folder to extract mod(s) to..."
            };

            if (Settings.LastExtractOutputPath.IsExistingDirectory())
            {
                dialog.SelectedPath = Settings.LastExtractOutputPath + "\\";
            }
            else
            {
                dialog.SelectedPath = GetInitialStartingDirectory();
            }

            if (dialog.ShowDialog(View) == true)
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
                            ShowAlert($"Successfully extracted all selected mods to '{dialog.SelectedPath}'.", AlertType.Success, 20);
                            DivinityFileUtils.TryOpenPath(openOutputPath);
                        }
                        else
                        {
                            ShowAlert($"Error occurred when extracting selected mods to '{dialog.SelectedPath}'.", AlertType.Danger, 30);
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
                MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show(View, $"Extract the following mods?\n'{String.Join("\n", SelectedPakMods.Select(x => $"{x.DisplayName}"))}", "Extract Mods?",
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, View.MainWindowMessageBox_OK.Style);
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
                ShowAlert($"Current adventure mod '{displayName}' is not extractable.", AlertType.Warning, 30);
                return;
            }

            var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                ShowNewFolderButton = true,
                UseDescriptionForTitle = true,
                Description = "Select folder to extract mod to..."
            };

            if (Settings.LastExtractOutputPath.IsExistingDirectory())
            {
                dialog.SelectedPath = Settings.LastExtractOutputPath + "\\";
            }
            else
            {
                dialog.SelectedPath = GetInitialStartingDirectory();
            }

            if (dialog.ShowDialog(View) == true)
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
                            ShowAlert($"Successfully extracted adventure mod to '{dialog.SelectedPath}'.", AlertType.Success, 20);
                            DivinityFileUtils.TryOpenPath(openOutputPath);
                        }
                        else
                        {
                            ShowAlert($"Error occurred when extracting adventure mod to '{dialog.SelectedPath}'.", AlertType.Danger, 30);
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
                ShowAlert($"Removed {totalRemoved} missing mods from the current order. Save to confirm.", AlertType.Warning);
            }
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
                                DivinityModDataLoader.IgnoreBuiltinPath.Add(path.Replace(Path.DirectorySeparator, "/"));
                            }
                        }
                    }
                    DivinityApp.IgnoredMods.Clear();
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
                            if (dict.TryGetValue("Targets", out var targets))
                            {
                                string tstr = (string)targets;
                                if (!String.IsNullOrEmpty(tstr))
                                {
                                    mod.Modes.Clear();
                                    var strTargets = tstr.Split(';');
                                    foreach (var t in strTargets)
                                    {
                                        mod.Modes.Add(t);
                                    }
                                }
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
                            var existingIgnoredMod = DivinityApp.IgnoredMods.FirstOrDefault(x => x.UUID == mod.UUID);
                            if (existingIgnoredMod == null)
                            {
                                DivinityApp.IgnoredMods.Add(mod);
                            }
                            else if (existingIgnoredMod.Version < mod.Version)
                            {
                                DivinityApp.IgnoredMods.Remove(existingIgnoredMod);
                                DivinityApp.IgnoredMods.Add(mod);
                            }

                            DivinityApp.Log($"Ignored mod added: Name({mod.Name}) UUID({mod.UUID})");
                        }
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


        class ActiveModsChangedObserver : IObserver<DivinityModData>
        {
            private const string NEXUS_MODS_API_KEY = "JLEbuBXmtLMK+d/zlkByYGYThdJGcW3lOC8ZlnroTqk0PzV9--fxVDZrWnpDd6skDK--jQiLzK0ftyINjzfwPU2pfA==";

            static ActiveModsChangedObserver()
            {
                NexusAPI.ApiManager.Init(apiKey: NEXUS_MODS_API_KEY, userAgent: "DivinityModManager", cacheDir: Path.Combine("nexus-cache"));
            }

            public void OnCompleted()
            {
                // Logic for when the provider has finished sending data.
            }

            public void OnError(Exception error)
            {
                // Logic for handling errors.
            }

            public void OnNext(DivinityModData value)
            {
                DivinityApp.Log($"ActiveModsChangedObserver.OnNext({value.DisplayName})");
                Task<NexusAPI.Responses.ModHashResult[]> modsByHash = NexusAPI.ApiManager.GetModByHash("baldurs-gate-3", value.MD5);
                if (modsByHash != null)
                {
                    // Print the mod name and version.
                    foreach (var modHashResult in modsByHash.Result)
                    {
                        DivinityApp.Log($"Mod: {modHashResult.Mod.Name} - {modHashResult.Mod.Version}");
                    }
                }
            }
        }

        public class HashUtility
        {
            public static string GetMD5Hash(string filePath)
            {
                using (var md5 = System.Security.Cryptography.MD5.Create())
                {
                    using (var stream = File.OpenRead(filePath))
                    {
                        return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", string.Empty);
                    }
                }
            }
        }

        public MainWindowViewModel() : base()
        {
            MainProgressValue = 0d;
            MainProgressIsActive = true;
            StatusBarBusyIndicatorVisibility = Visibility.Collapsed;

            exceptionHandler = new MainWindowExceptionHandler(this);
            RxApp.DefaultExceptionHandler = exceptionHandler;

            this.ModUpdatesViewData = new ModUpdatesViewData(this);

            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var productName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false)).Product;
            Version = assembly.GetName().Version.ToString();
            Title = $"{productName} {this.Version}";
            DivinityApp.Log($"{Title} initializing...");
            AutoUpdater.AppTitle = productName;

            this.DropHandler = new ModListDropHandler(this);
            this.DragHandler = new ModListDragHandler(this);

            Activator = new ViewModelActivator();

            DivinityApp.DependencyFilter = this.WhenAnyValue(x => x.Settings.DebugModeEnabled).Select(MakeDependencyFilter);

            this.WhenActivated((CompositeDisposable disposables) =>
            {
                if (!disposables.Contains(this.Disposables)) disposables.Add(this.Disposables);
            });

            _isLocked = this.WhenAnyValue(x => x.IsDragging, x => x.IsRefreshing, x => x.IsLoadingOrder, (b1, b2, b3) => b1 || b2 || b3).StartWith(false).ToProperty(this, nameof(IsLocked));
            _allowDrop = this.WhenAnyValue(x => x.IsLoadingOrder, x => x.IsRefreshing, x => x.IsInitialized, (b1, b2, b3) => !b1 && !b2 && b3).StartWith(true).ToProperty(this, nameof(AllowDrop));

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
                gameMasterCampaigns.Clear();
                Profiles.Clear();
                workshopMods.Clear();
                View.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Normal;
                View.TaskbarItemInfo.ProgressValue = 0;
                IsRefreshing = true;
                RxApp.TaskpoolScheduler.ScheduleAsync(RefreshAsync);
            }, canRefreshObservable, RxApp.MainThreadScheduler);

            Keys.Refresh.AddAction(() => RefreshCommand.Execute(Unit.Default).Subscribe(), canRefreshObservable);

            var canRefreshWorkshop = this.WhenAnyValue(x => x.IsRefreshing, x => x.IsRefreshingWorkshop, x => x.AppSettingsLoaded, (b1, b2, b3) => !b1 && !b2 && b3 && AppSettings.FeatureEnabled("Workshop")).StartWith(false);

            RefreshWorkshopCommand = ReactiveCommand.Create(() =>
            {
                ModUpdatesViewData?.Clear();
                ModUpdatesViewVisible = ModUpdatesAvailable = false;
                workshopMods.Clear();
                LoadWorkshopModDataBackground();
            }, canRefreshWorkshop, RxApp.MainThreadScheduler);

            Keys.RefreshWorkshop.AddAction(() => RefreshWorkshopCommand.Execute(Unit.Default).Subscribe(), canRefreshWorkshop);

            IObservable<bool> canStartExport = this.WhenAny(x => x.MainProgressToken, (t) => t != null).StartWith(false);
            Keys.ExportOrderToZip.AddAction(ExportLoadOrderToArchive_Start, canStartExport);
            Keys.ExportOrderToArchiveAs.AddAction(ExportLoadOrderToArchiveAs, canStartExport);

            var anyActiveObservable = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);
            Keys.ExportOrderToList.AddAction(ExportLoadOrderToTextFileAs, anyActiveObservable);

            canOpenDialogWindow = this.WhenAnyValue(x => x.MainProgressIsActive, (b) => !b);
            Keys.ImportOrderFromSave.AddAction(ImportOrderFromSaveToCurrent, canOpenDialogWindow);
            Keys.ImportOrderFromSaveAsNew.AddAction(ImportOrderFromSaveAsNew, canOpenDialogWindow);
            Keys.ImportOrderFromFile.AddAction(ImportOrderFromFile, canOpenDialogWindow);
            Keys.ImportOrderFromZipFile.AddAction(ImportOrderFromArchive, canOpenDialogWindow);

            Keys.OpenDonationLink.AddAction(() =>
            {
                DivinityFileUtils.TryOpenPath(DivinityApp.URL_DONATION);
            });

            Keys.OpenRepositoryPage.AddAction(() =>
            {
                DivinityFileUtils.TryOpenPath(DivinityApp.URL_REPO);
            });

            Keys.ToggleViewTheme.AddAction(() =>
            {
                if (Settings != null)
                {
                    Settings.DarkThemeEnabled = !Settings.DarkThemeEnabled;
                }
            });

            Keys.ToggleFileNameDisplay.AddAction(() =>
            {
                if (Settings != null)
                {
                    Settings.DisplayFileNames = !Settings.DisplayFileNames;

                    foreach (var m in Mods)
                    {
                        m.DisplayFileForName = Settings.DisplayFileNames;
                    }
                }
                else
                {
                    foreach (var m in Mods)
                    {
                        m.DisplayFileForName = !m.DisplayFileForName;
                    }
                }
            });

            Keys.DeleteSelectedMods.AddAction(() =>
            {
                IEnumerable<DivinityModData> targetList = null;
                if (DivinityApp.IsKeyboardNavigating)
                {
                    var modLayout = this.View.GetModLayout();
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
                        ShowAlert("Editor mods cannot be deleted with the Mod Manager.", AlertType.Warning, 60);
                    }
                }
            });

            #endregion

            DeleteOrderCommand = ReactiveCommand.Create<DivinityLoadOrder, Unit>(DeleteOrder, canOpenDialogWindow);

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
                    ShowAlert($"Copied '{path}' to clipboard.", 0, 10);
                }
                else
                {
                    ShowAlert($"Path not found.", AlertType.Danger, 30);
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
                        ShowAlert("Copied mod order to clipboard.", AlertType.Info, 10);
                    }
                    else
                    {
                        ShowAlert("Current order is empty.", AlertType.Warning, 10);
                    }
                }
                catch (Exception ex)
                {
                    ShowAlert($"Error copying order to clipboard: {ex}", AlertType.Danger, 15);
                }
            });

            var profileChanged = this.WhenAnyValue(x => x.SelectedProfileIndex, x => x.Profiles.Count).Select(x => Profiles.ElementAtOrDefault(x.Item1));
            _selectedProfile = profileChanged.ToProperty(this, nameof(SelectedProfile)).DisposeWith(this.Disposables);
            var hasNonNullProfile = this.WhenAnyValue(x => x.SelectedProfile).Select(x => x != null);
            _hasProfile = hasNonNullProfile.ToProperty(this, nameof(HasProfile)).DisposeWith(this.Disposables);

            Keys.ExportOrderToGame.AddAction(ExportLoadOrder, hasNonNullProfile);

            profileChanged.Subscribe((profile) =>
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

            this.WhenAnyValue(vm => vm.SelectedProfileIndex, (index) => index > -1 && index < Profiles.Count).Subscribe((b) =>
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

            // Whenever anything about ActiveMods changes, log it
            const string NEXUS_MODS_API_KEY = "JLEbuBXmtLMK+d/zlkByYGYThdJGcW3lOC8ZlnroTqk0PzV9--fxVDZrWnpDd6skDK--jQiLzK0ftyINjzfwPU2pfA==";
            ActiveMods.ActOnEveryObject(
                (mod) =>
                {
                    DivinityApp.Log($"ActiveMods changed: {mod.DisplayName} ({mod.UUID})");
                    // Calculate MD5 Hash using the filepath to the mod
                    var md5 = HashUtility.GetMD5Hash(mod.FilePath);
                    DivinityApp.Log($"MD5: {md5}");
                    Task<NexusAPI.Responses.ModHashResult[]> modsByHash = NexusAPI.ApiManager.GetModByHash("baldursgate3", md5);
                    if (modsByHash != null)
                    {
                        // Print the mod name and version.
                        foreach (var modHashResult in modsByHash.Result)
                        {
                            DivinityApp.Log($"FETCHED MOD BY HASH: {modHashResult.Mod.Name} - {modHashResult.Mod.Version}");
                        }
                    }
                },
                (mod) =>
                {

                }
            );
            var selectedModsConnection = modsConnection.AutoRefresh(x => x.IsSelected, TimeSpan.FromMilliseconds(25)).AutoRefresh(x => x.IsActive, TimeSpan.FromMilliseconds(25)).Filter(x => x.IsSelected);

            _activeSelected = selectedModsConnection.Filter(x => x.IsActive).Count().ToProperty(this, nameof(ActiveSelected), true, RxApp.MainThreadScheduler);
            _inactiveSelected = selectedModsConnection.Filter(x => !x.IsActive).Count().ToProperty(this, nameof(InactiveSelected), true, RxApp.MainThreadScheduler);

            _activeSelectedText = this.WhenAnyValue(x => x.ActiveSelected, x => x.TotalActiveModsHidden).Select(x => SelectedToLabel(x.Item1, x.Item2)).ToProperty(this, nameof(ActiveSelectedText), true, RxApp.MainThreadScheduler);
            _inactiveSelectedText = this.WhenAnyValue(x => x.InactiveSelected, x => x.TotalInactiveModsHidden).Select(x => SelectedToLabel(x.Item1, x.Item2)).ToProperty(this, nameof(InactiveSelectedText), true, RxApp.MainThreadScheduler);

            _activeModsFilterResultText = this.WhenAnyValue(x => x.TotalActiveModsHidden).Select(x => HiddenToLabel(x, ActiveMods.Count)).ToProperty(this, nameof(ActiveModsFilterResultText), true, RxApp.MainThreadScheduler);

            _inactiveModsFilterResultText = this.WhenAnyValue(x => x.TotalInactiveModsHidden).Select(x => HiddenToLabel(x, InactiveMods.Count)).ToProperty(this, nameof(InactiveModsFilterResultText), true, RxApp.MainThreadScheduler);

            DivinityApp.Events.OrderNameChanged += OnOrderNameChanged;

            modsConnection.Filter(x => x.ModType == "Adventure" && (!x.IsHidden || x.UUID == DivinityApp.MAIN_CAMPAIGN_UUID)).Bind(out adventureMods).DisposeMany().Subscribe();
            _selectedAdventureMod = this.WhenAnyValue(x => x.SelectedAdventureModIndex, x => x.AdventureMods.Count, (index, count) => index >= 0 && count > 0 && index < count).
                Where(b => b == true).Select(x => AdventureMods[SelectedAdventureModIndex]).
                ToProperty(this, x => x.SelectedAdventureMod).DisposeWith(this.Disposables);

            var adventureModCanOpenObservable = this.WhenAnyValue(x => x.SelectedAdventureMod, (mod) => mod != null && !mod.IsLarianMod);
            adventureModCanOpenObservable.Subscribe();

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

            OpenAdventureModInFileExplorerCommand = ReactiveCommand.Create<string>((path) =>
            {
                DivinityApp.Commands.OpenInFileExplorer(path);
            }, adventureModCanOpenObservable);

            CopyAdventureModPathToClipboardCommand = ReactiveCommand.Create<string>((path) =>
            {
                if (!String.IsNullOrWhiteSpace(path))
                {
                    Clipboard.SetText(path);
                    ShowAlert($"Copied '{path}' to clipboard.", 0, 10);
                }
                else
                {
                    ShowAlert($"Path not found.", AlertType.Danger, 30);
                }
            }, adventureModCanOpenObservable);

            var canCheckForUpdates = this.WhenAnyValue(x => x.MainProgressIsActive, b => b == false);
            void checkForUpdatesAction()
            {
                ShowAlert("Checking for updates...", AlertType.Info, 90);
                View.UserInvokedUpdate = true;
                CheckForUpdates(true);
            }
            CheckForAppUpdatesCommand = ReactiveCommand.Create(checkForUpdatesAction, canCheckForUpdates);
            Keys.CheckForUpdates.AddAction(checkForUpdatesAction, canCheckForUpdates);

            /////////////////////////////////////////////////////////////////////////////////////////////
            // Use Nexus Mods API to check for updates
            // Listen to ActiveMods, and when it changes, check for updates by mapping over the mods and
            // making an API call for each mod. If the API call returns a newer version, then add it to
            // the list of mods that have updates available.
            ActiveMods.Subscribe(new ActiveModsChangedObserver());
            //////////////////////////////////////////////////////////////////////////////////////////

            /////////////////////////////////////////////////////////////////////////////////////////////
            canRenameOrder = this.WhenAnyValue(x => x.SelectedModOrderIndex, (i) => i > 0);

            ToggleOrderRenamingCommand = ReactiveCommand.CreateFromTask<object, Unit>(ToggleRenamingLoadOrder, canRenameOrder);

            workshopMods.Connect().Bind(out workshopModsCollection).DisposeMany().Subscribe();

            modsConnection.AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected && !x.IsEditorMod && File.Exists(x.FilePath)).Bind(out selectedPakMods).Subscribe();

            // Blinky animation on the tools/download buttons if the extender is required by mods and is missing
            if (AppSettings.FeatureEnabled("ScriptExtender"))
            {
                modsConnection.ObserveOn(RxApp.MainThreadScheduler).AutoRefresh(x => x.ExtenderModStatus).
                    Filter(x => x.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_MISSING || x.ExtenderModStatus == DivinityExtenderModStatus.REQUIRED_DISABLED).
                    Select(x => x.Count).Subscribe(totalWithRequirements =>
                    {
                        if (totalWithRequirements > 0)
                        {
                            if (Settings.ExtenderSettings != null)
                            {
                                HighlightExtenderDownload = !Settings.ExtenderSettings.ExtenderUpdaterIsAvailable;
                            }
                            else
                            {
                                HighlightExtenderDownload = true;
                            }
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

            this.WhenAnyValue(x => x.ModUpdatesViewData.NewAvailable,
                x => x.ModUpdatesViewData.UpdatesAvailable, (b1, b2) => b1 || b2).BindTo(this, x => x.ModUpdatesAvailable);

            ModUpdatesViewData.CloseView = new Action<bool>((bool refresh) =>
            {
                ModUpdatesViewData.Clear();
                if (refresh) RefreshCommand.Execute(Unit.Default).Subscribe();
                ModUpdatesViewVisible = false;
                View.Activate();
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

            var gmModeChanged = this.WhenAnyValue(x => x.Settings.GameMasterModeEnabled);
            _adventureModBoxVisibility = gmModeChanged.Select(x => !x ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Visible).ToProperty(this, nameof(AdventureModBoxVisibility), true, RxApp.MainThreadScheduler);

            _gameMasterModeVisibility = gmModeChanged.Select(x => x ? Visibility.Visible : Visibility.Collapsed).StartWith(Visibility.Collapsed).ToProperty(this, nameof(GameMasterModeVisibility), true, RxApp.MainThreadScheduler);

            gameMasterCampaigns.Connect().Bind(out gameMasterCampaignsData).Subscribe();

            var justSelectedGameMasterCampaign = this.WhenAnyValue(x => x.SelectedGameMasterCampaignIndex, x => x.GameMasterCampaigns.Count);
            _selectedGameMasterCampaign = justSelectedGameMasterCampaign.Select(x => GameMasterCampaigns.ElementAtOrDefault(x.Item1)).ToProperty(this, nameof(SelectedGameMasterCampaign));

            Keys.ImportOrderFromSelectedGMCampaign.AddAction(() => LoadGameMasterCampaignModOrder(SelectedGameMasterCampaign), gmModeChanged);

            justSelectedGameMasterCampaign.ObserveOn(RxApp.MainThreadScheduler).Subscribe((d) =>
            {
                if (!this.IsRefreshing && IsInitialized && (Settings != null && Settings.AutomaticallyLoadGMCampaignMods) && d.Item1 > -1)
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

            _isDeletingFiles = this.WhenAnyValue(x => x.View.DeleteFilesView.ViewModel.IsVisible).ToProperty(this, nameof(IsDeletingFiles), true, RxApp.MainThreadScheduler);

            _hideModList = this.WhenAnyValue(x => x.MainProgressIsActive, x => x.IsDeletingFiles, (a, b) => a || b).StartWith(true).ToProperty(this, nameof(HideModList), false, RxApp.MainThreadScheduler);

            var forceLoadedModsConnection = this.ForceLoadedMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);
            _hasForceLoadedMods = forceLoadedModsConnection.Count().StartWith(0).Select(x => x > 0).ToProperty(this, nameof(HasForceLoadedMods), true, RxApp.MainThreadScheduler);

            DivinityInteractions.ConfirmModDeletion.RegisterHandler((Func<InteractionContext<DeleteFilesViewConfirmationData, bool>, Task>)(async interaction =>
            {
                var sentenceStart = interaction.Input.PermanentlyDelete ? "Permanently delete" : "Delete";
                var msg = $"{sentenceStart} {interaction.Input.Total} mod file(s)?";

                var confirmed = await Observable.Start((Func<bool>)(() =>
                {
                    MessageBoxResult result = Xceed.Wpf.Toolkit.MessageBox.Show((Window)this.View, msg, "Confirm Mod Deletion",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No, (Style)this.View.MainWindowMessageBox_OK.Style);
                    if (result == MessageBoxResult.Yes)
                    {
                        return true;
                    }
                    return false;
                }), RxApp.MainThreadScheduler);
                interaction.SetOutput(confirmed);
            }));

            CanSaveOrder = true;
            LayoutMode = 0;
        }
    }
}
