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
using System.Collections.Immutable;
using DivinityModManager.AppServices;

namespace DivinityModManager.ViewModels.Main;
public class ModOrderViewModel : BaseHistoryViewModel, IRoutableViewModel, IModOrderViewModel
{
	public string UrlPathSegment => "modorder";
	public IScreen HostScreen { get; }

	public AppKeys Keys { get; }

	public IModViewLayout Layout { get; set; }

	public ModListDropHandler DropHandler { get; }
	public ModListDragHandler DragHandler { get; }

	private readonly SourceCache<DivinityProfileData, string> profiles = new(x => x.FilePath);

	private readonly ReadOnlyObservableCollection<DivinityProfileData> _uiprofiles;
	public ReadOnlyObservableCollection<DivinityProfileData> Profiles => _uiprofiles;

	public ObservableCollectionExtended<DivinityModData> ActiveMods { get; }
	public ObservableCollectionExtended<DivinityModData> InactiveMods { get; }

	private readonly ReadOnlyObservableCollection<DivinityModData> _forceLoadedMods;
	public ReadOnlyObservableCollection<DivinityModData> ForceLoadedMods => _forceLoadedMods;

	public ObservableCollectionExtended<DivinityLoadOrder> ModOrderList { get; }
	public List<DivinityLoadOrder> ExternalModOrders { get; }

	private readonly Regex filterPropertyPattern = new("@([^\\s]+?)([\\s]+)([^@\\s]*)");
	private readonly Regex filterPropertyPatternWithQuotes = new("@([^\\s]+?)([\\s\"]+)([^@\"]*)");

	[Reactive] public int TotalActiveModsHidden { get; set; }
	[Reactive] public int TotalInactiveModsHidden { get; set; }
	[Reactive] public int TotalOverrideModsHidden { get; set; }

	[Reactive] public string ActiveModFilterText { get; set; }
	[Reactive] public string InactiveModFilterText { get; set; }
	[Reactive] public string OverrideModsFilterText { get; set; }

	private static string HiddenToLabel(int totalHidden, int totalCount)
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

	private static string SelectedToLabel(int total, int totalHidden)
	{
		if (totalHidden > 0)
		{
			return $", {total} Selected";
		}
		return $"{total} Selected";
	}


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

		if (String.IsNullOrEmpty(lastSelectedCampaignUUID))
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

		var modManager = Services.Mods;

		List<DivinityMissingModData> missingMods = new();
		if (campaign.Dependencies.Count > 0)
		{
			int index = 0;
			foreach (var entry in campaign.Dependencies.Items)
			{
				if (modManager.TryGetMod(entry.UUID, out var mod))
				{
					mod.IsActive = true;
					currentOrder.Add(mod);
					index++;
					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (!DivinityModDataLoader.IgnoreMod(dependency.UUID) && !modManager.ModExists(dependency.UUID) &&
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

	[Reactive] public bool IsRenamingOrder { get; set; }
	[Reactive] public bool IsRefreshing { get; private set; }
	[Reactive] public bool IsLoadingOrder { get; set; }
	[Reactive] public bool IsLocked { get; private set; }

	[Reactive] public bool CanMoveSelectedMods { get; set; }
	[Reactive] public bool CanSaveOrder { get; set; }

	[Reactive] public int SelectedProfileIndex { get; set; }
	[Reactive] public int SelectedModOrderIndex { get; set; }
	[Reactive] public int SelectedAdventureModIndex { get; set; }

	[Reactive] public DivinityProfileData SelectedProfile { get; set; }
	[Reactive] public DivinityLoadOrder SelectedModOrder { get; set; }
	[Reactive] public DivinityModData SelectedAdventureMod { get; set; }

	[ObservableAsProperty] public string SelectedModOrderName { get; }

	[ObservableAsProperty] public Visibility AdventureModBoxVisibility { get; }
	[ObservableAsProperty] public Visibility LogFolderShortcutButtonVisibility { get; }

	[ObservableAsProperty] public bool HasProfile { get; }
	[ObservableAsProperty] public bool IsBaseLoadOrder { get; }

	[ObservableAsProperty] public string ActiveSelectedText { get; }
	[ObservableAsProperty] public string InactiveSelectedText { get; }
	[ObservableAsProperty] public string OverrideModsSelectedText { get; }
	[ObservableAsProperty] public string ActiveModsFilterResultText { get; }
	[ObservableAsProperty] public string InactiveModsFilterResultText { get; }
	[ObservableAsProperty] public string OverrideModsFilterResultText { get; }

	[ObservableAsProperty] public string OpenGameButtonToolTip { get; }

	[ObservableAsProperty] public int TotalActiveMods { get; }
	[ObservableAsProperty] public int TotalInactiveMods { get; }
	[ObservableAsProperty] public Visibility OverrideModsVisibility { get; }

	public ReactiveCommand<DivinityLoadOrder, Unit> DeleteOrderCommand { get; }
	public ReactiveCommand<object, Unit> ToggleOrderRenamingCommand { get; set; }
	public ICommand FocusFilterCommand { get; set; }
	public ICommand CopyOrderToClipboardCommand { get; }
	public ICommand ExportOrderAsListCommand { get; }
	public ReactiveCommand<DivinityLoadOrder, Unit> OrderJustLoadedCommand { get; set; }

	private DivinityPathwayData PathwayData => Services.Pathways.Data;
	private ModManagerSettings Settings => Services.Settings.ManagerSettings;
	private AppSettings AppSettings => Services.Settings.AppSettings;

	private static IObservable<bool> AllTrue(IObservable<bool> first, IObservable<bool> second) => first.CombineLatest(second).Select(x => x.First && x.Second);

	private static string GetLaunchGameTooltip(ValueTuple<string, bool, bool, bool> x)
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

	private void SetupKeys(AppKeys keys, MainWindowViewModel main, IObservable<bool> canExecuteCommands)
	{
		var modImporter = Services.Get<ModImportService>();

		var canExecuteSaveCommand = AllTrue(canExecuteCommands, this.WhenAnyValue(x => x.CanSaveOrder));
		keys.Save.AddAction(() => SaveLoadOrder(), canExecuteSaveCommand);

		canExecuteSaveCommand.Subscribe(b =>
		{
			DivinityApp.Log($"canExecuteSaveCommand: {b}");
		});

		keys.SaveAs.AddAction(SaveLoadOrderAs, canExecuteSaveCommand);
		keys.ImportMod.AddAction(modImporter.OpenModImportDialog, canExecuteCommands);
		keys.ImportNexusModsIds.AddAction(modImporter.OpenModIdsImportDialog, canExecuteCommands);
		keys.NewOrder.AddAction(() => AddNewModOrder(), canExecuteCommands);

		var anyActiveObservable = ActiveMods.WhenAnyValue(x => x.Count).Select(x => x > 0);
		//var anyActiveObservable = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);
		keys.ExportOrderToList.AddAction(ExportLoadOrderToTextFileAs, anyActiveObservable);

		keys.ImportOrderFromSave.AddAction(ImportOrderFromSaveToCurrent, canExecuteCommands);
		keys.ImportOrderFromSaveAsNew.AddAction(ImportOrderFromSaveAsNew, canExecuteCommands);
		keys.ImportOrderFromFile.AddAction(ImportOrderFromFile, canExecuteCommands);
		keys.ImportOrderFromZipFile.AddAction(modImporter.ImportOrderFromArchive, canExecuteCommands);

		keys.ExportOrderToGame.AddAction(ExportLoadOrder, AllTrue(canExecuteCommands, this.WhenAnyValue(x => x.SelectedProfile).Select(x => x != null)));

		keys.DeleteSelectedMods.AddAction(() =>
		{
			IEnumerable<DivinityModData> targetList = null;
			if (DivinityApp.IsKeyboardNavigating)
			{
				var modLayout = Services.Get<ModOrderView>()?.ModLayout;
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
				targetList = Services.Mods.AllMods;
			}

			if (targetList != null)
			{
				var selectedMods = targetList.Where(x => x.IsSelected);
				var selectedEligableMods = selectedMods.Where(x => x.CanDelete).ToList();

				if (selectedEligableMods.Count > 0)
				{
					DivinityInteractions.DeleteMods.Handle(new DeleteModsRequestData(selectedEligableMods, false, Services.Mods.AllMods)).Subscribe();
				}

				if (selectedMods.Any(x => x.IsEditorMod))
				{
					DivinityApp.ShowAlert("Editor mods cannot be deleted with the Mod Manager", AlertType.Warning, 60);
				}
			}
		}, canExecuteCommands);

		keys.SpeakActiveModOrder.AddAction(() =>
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
		}, canExecuteCommands);
	}

	private readonly SortExpressionComparer<DivinityProfileData> _profileSort = SortExpressionComparer<DivinityProfileData>.Ascending(p => p.FolderName != "Public").ThenByAscending(p => p.Name);

	private IDisposable _profileChangesDisp;

	private void ListenForProfileChanges(bool enabled)
	{
		_profileChangesDisp?.Dispose();
		if(enabled)
		{
			_profileChangesDisp = this.WhenAnyValue(x => x.SelectedProfileIndex).SubscribeOn(RxApp.MainThreadScheduler).Subscribe(index =>
			{
				if (index < Profiles.Count)
				{
					var nextProfile = Profiles.ElementAtOrDefault(index);
					if (nextProfile != null)
					{
						SelectedProfile = nextProfile;
					}
				}
			});
		}
	}

	public ModOrderViewModel(MainWindowViewModel host)
	{
		DivinityApp.Commands.SetViewModel(this);

		HostScreen = host;
		SelectedAdventureModIndex = 0;

		ActiveMods = [];
		InactiveMods = [];
		ModOrderList = [];
		ExternalModOrders = [];

		DropHandler = new(host);
		DragHandler = new(host);

		CanSaveOrder = true;

		Keys = host.Keys;

		var modManager = Services.Mods;

		var isRefreshing = host.WhenAnyValue(x => x.IsRefreshing);

		host.WhenAnyValue(x => x.IsLocked).BindTo(this, x => x.IsLocked);

		var isActive = HostScreen.Router.CurrentViewModel.Select(x => x == this);
		var mainIsNotLocked = host.WhenAnyValue(x => x.IsLocked, b => !b);
		var canExecuteCommands = mainIsNotLocked.CombineLatest(isActive).Select(x => x.First && x.Second);

		profiles.Connect().Sort(_profileSort).ObserveOnDispatcher().Bind(out _uiprofiles).DisposeMany().Subscribe();

		modManager.WhenAnyValue(x => x.ActiveSelected).CombineLatest(this.WhenAnyValue(x => x.TotalActiveModsHidden)).Select(x => SelectedToLabel(x.First, x.Second)).ToUIProperty(this, x => x.ActiveSelectedText);
		modManager.WhenAnyValue(x => x.InactiveSelected).CombineLatest(this.WhenAnyValue(x => x.TotalInactiveModsHidden)).Select(x => SelectedToLabel(x.First, x.Second)).ToUIProperty(this, x => x.InactiveSelectedText);
		modManager.WhenAnyValue(x => x.OverrideModsSelected).CombineLatest(this.WhenAnyValue(x => x.TotalOverrideModsHidden)).Select(x => SelectedToLabel(x.First, x.Second)).ToUIProperty(this, x => x.OverrideModsSelectedText);
		//TODO Change .Count to CollectionChanged?
		this.WhenAnyValue(x => x.TotalActiveModsHidden).Select(x => HiddenToLabel(x, ActiveMods.Count)).ToUIProperty(this, x => x.ActiveModsFilterResultText);
		this.WhenAnyValue(x => x.TotalInactiveModsHidden).Select(x => HiddenToLabel(x, InactiveMods.Count)).ToUIProperty(this, x => x.InactiveModsFilterResultText);
		this.WhenAnyValue(x => x.TotalOverrideModsHidden).Select(x => HiddenToLabel(x, modManager.ForceLoadedMods.Count)).ToUIProperty(this, x => x.OverrideModsFilterResultText);

		var whenProfile = this.WhenAnyValue(x => x.SelectedProfile);
		var hasNonNullProfile = whenProfile.Select(x => x != null);
		hasNonNullProfile.ToUIProperty(this, x => x.HasProfile);

		ActiveMods.ToObservableChangeSet().CountChanged().Select(x => ActiveMods.Count).ToUIPropertyImmediate(this, x => x.TotalActiveMods);
		InactiveMods.ToObservableChangeSet().CountChanged().Select(x => InactiveMods.Count).ToUIPropertyImmediate(this, x => x.TotalInactiveMods);
		modManager.ForceLoadedMods.ToObservableChangeSet().Bind(out _forceLoadedMods).Subscribe();
		ForceLoadedMods.ToObservableChangeSet().CountChanged().Select(_ => PropertyConverters.IntToVisibility(ForceLoadedMods.Count)).ToUIPropertyImmediate(this, x => x.OverrideModsVisibility, Visibility.Collapsed);

		host.Settings.WhenAnyValue(
			x => x.ExtenderSettings.LogCompile,
			x => x.ExtenderSettings.LogRuntime,
			x => x.ExtenderSettings.EnableLogging,
			x => x.ExtenderSettings.DeveloperMode,
			x => x.DebugModeEnabled)
		.Select(PropertyConverters.BoolTupleToVisibility).ToUIProperty(this, x => x.LogFolderShortcutButtonVisibility);

		var whenGameExeProperties = host.WhenAnyValue(x => x.Settings.GameExecutablePath, x => x.Settings.LimitToSingleInstance, x => x.GameIsRunning, x => x.CanForceLaunchGame);
		whenGameExeProperties.Select(GetLaunchGameTooltip).ToUIProperty(this, x => x.OpenGameButtonToolTip, "Launch Game");

		var canRenameOrder = this.WhenAnyValue(x => x.SelectedModOrderIndex, (i) => i > 0);
		ToggleOrderRenamingCommand = ReactiveCommand.CreateFromTask<object, Unit>(ToggleRenamingLoadOrder, canRenameOrder, RxApp.MainThreadScheduler);

		var canDeleteOrder = AllTrue(canExecuteCommands, this.WhenAnyValue(x => x.SelectedModOrderIndex).Select(x => x > 0));
		DeleteOrderCommand = ReactiveCommand.CreateFromTask<DivinityLoadOrder>(DeleteOrder, canDeleteOrder);

		CopyOrderToClipboardCommand = ReactiveCommand.CreateFromObservable(() => Observable.Start(() =>
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
					DivinityApp.ShowAlert("Copied mod order to clipboard", AlertType.Info, 10);
				}
				else
				{
					DivinityApp.ShowAlert("Current order is empty", AlertType.Warning, 10);
				}
			}
			catch (Exception ex)
			{
				DivinityApp.ShowAlert($"Error copying order to clipboard: {ex}", AlertType.Danger, 15);
			}
		}, RxApp.MainThreadScheduler));

		ExportOrderAsListCommand = ReactiveCommand.Create(ExportLoadOrderToTextFileAs, this.WhenAnyValue(x => x.TotalActiveMods, x => x > 0));

		whenProfile.Subscribe(profile =>
		{
			if (profile != null && profile.ActiveMods != null && profile.ActiveMods.Count > 0)
			{
				var adventureModData = modManager.AdventureMods.FirstOrDefault(x => profile.ActiveMods.Any(y => y.UUID == x.UUID));
				//Migrate old profiles from Gustav to GustavDev
				if (adventureModData != null && adventureModData.UUID == "991c9c7a-fb80-40cb-8f0d-b92d4e80e9b1")
				{
					if (modManager.TryGetMod(DivinityApp.MAIN_CAMPAIGN_UUID, out var main))
					{
						adventureModData = main;
					}
				}
				if (adventureModData != null)
				{
					var nextAdventure = modManager.AdventureMods.IndexOf(adventureModData);
					DivinityApp.Log($"Found adventure mod in profile: {adventureModData.Name} | {nextAdventure}");
					if (nextAdventure > -1)
					{
						SelectedAdventureModIndex = nextAdventure;
					}
				}
			}
		});

		OrderJustLoadedCommand = ReactiveCommand.Create<DivinityLoadOrder>(order => { });

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
				if (!host.IsLocked && !IsLoadingOrder)
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

		//Throttle filters so they only happen when typing stops for 500ms

		this.WhenAnyValue(x => x.ActiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
			Subscribe((s) => { OnFilterTextChanged(s, ActiveMods); });

		this.WhenAnyValue(x => x.InactiveModFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
			Subscribe((s) => { OnFilterTextChanged(s, InactiveMods); });

		this.WhenAnyValue(x => x.OverrideModsFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
			Subscribe((s) => { OnFilterTextChanged(s, modManager.ForceLoadedMods); });

		ActiveMods.WhenAnyPropertyChanged(nameof(DivinityModData.Index)).Throttle(TimeSpan.FromMilliseconds(25)).Subscribe(_ =>
		{
			SelectedModOrder?.Sort(SortModOrder);
		});

		modManager.AdventureMods.ToObservableChangeSet().CountChanged().CombineLatest(this.WhenAnyValue(x => x.SelectedAdventureModIndex))
			.Select(x => x.Second >= 0 && modManager.AdventureMods.Count > 0 && x.Second < modManager.AdventureMods.Count)
			.Where(b => b == true)
			.Select(x => modManager.AdventureMods[SelectedAdventureModIndex]).BindTo(this, x => x.SelectedAdventureMod);

		this.WhenAnyValue(x => x.SelectedAdventureModIndex).Throttle(TimeSpan.FromMilliseconds(50)).Subscribe((i) =>
		{
			if (modManager.AdventureMods != null && SelectedAdventureMod != null && SelectedProfile != null && SelectedProfile.ActiveMods != null)
			{
				if (!SelectedProfile.ActiveMods.Any(m => m.UUID == SelectedAdventureMod.UUID))
				{
					SelectedProfile.ActiveMods.RemoveAll(r => modManager.AdventureMods.Any(y => y.UUID == r.UUID));
					SelectedProfile.ActiveMods.Insert(0, SelectedAdventureMod.ToProfileModData());
				}
			}
		});


		#region DungeonMaster Support

		var gmModeChanged = Settings.WhenAnyValue(x => x.GameMasterModeEnabled);
		gmModeChanged.Select(PropertyConverters.BoolToVisibilityReversed).ToUIProperty(this, x => x.AdventureModBoxVisibility, Visibility.Visible);
		gmModeChanged.Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.GameMasterModeVisibility, Visibility.Collapsed);

		gameMasterCampaigns.Connect().Bind(out gameMasterCampaignsData).Subscribe();

		var justSelectedGameMasterCampaign = this.WhenAnyValue(x => x.SelectedGameMasterCampaignIndex, x => x.GameMasterCampaigns.Count);
		justSelectedGameMasterCampaign.Select(x => GameMasterCampaigns.ElementAtOrDefault(x.Item1)).ToUIProperty(this, x => x.SelectedGameMasterCampaign);

		host.Keys.ImportOrderFromSelectedGMCampaign.AddAction(() => LoadGameMasterCampaignModOrder(SelectedGameMasterCampaign), gmModeChanged);

		justSelectedGameMasterCampaign.ObserveOn(RxApp.MainThreadScheduler).Subscribe((d) =>
		{
			if (!host.IsRefreshing && host.IsInitialized && Settings.AutomaticallyLoadGMCampaignMods && d.Item1 > -1)
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
						DivinityApp.ShowAlert("The active load order (modsettings.lsx) has been reset externally", AlertType.Danger);
						RxApp.MainThreadScheduler.Schedule(() =>
						{
							//Window.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.Indeterminate;
							host.Window.FlashTaskbar();
							var title = "Mod Order Reset";
							var message = "The active load order (modsettings.lsx) has been reset externally, which has deactivated your mods.\nOne or more mods may be invalid in your current load order.";
							DivinityInteractions.ShowMessageBox.Handle(new(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK)).Subscribe();
						});
					}
				});
			}
		});

		SetupKeys(host.Keys, host, canExecuteCommands);
	}

	private async static Task<TResult> RunTask<TResult>(Task<TResult> task, TResult defaultValue)
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

	public async Task RefreshAsync(MainWindowViewModel main, CancellationToken token)
	{
		IsRefreshing = true;
		ListenForProfileChanges(false);
		DivinityApp.Log($"Refreshing data asynchronously...");

		double taskStepAmount = 1.0 / 10;

		var modManager = Services.Mods;

		List<DivinityLoadOrderEntry> lastActiveOrder = null;
		string lastOrderName = "";
		if (SelectedModOrder != null)
		{
			lastActiveOrder = [.. SelectedModOrder.Order];
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
			await main.SetMainProgressTextAsync("Loading mods...");
			var loadedMods = await ModManagerService.LoadModsAsync(PathwayData.AppDataModsPath, new(main.SetMainProgressTextAsync, main.IncreaseMainProgressValueAsync), taskStepAmount);
			await main.IncreaseMainProgressValueAsync(taskStepAmount);

			DivinityApp.Log("Loading profiles...");
			await main.SetMainProgressTextAsync("Loading profiles...");
			var loadedProfiles = await LoadProfilesAsync();
			await main.IncreaseMainProgressValueAsync(taskStepAmount);

			if (String.IsNullOrEmpty(selectedProfileUUID) && (loadedProfiles != null && loadedProfiles.Count > 0))
			{
				DivinityApp.Log("Loading current profile...");
				await main.SetMainProgressTextAsync("Loading current profile...");
				selectedProfileUUID = await DivinityModDataLoader.GetSelectedProfileUUIDAsync(PathwayData.AppDataProfilesPath);
				await main.IncreaseMainProgressValueAsync(taskStepAmount);
			}
			else
			{
				if ((loadedProfiles == null || loadedProfiles.Count == 0))
				{
					DivinityApp.Log("No profiles found?");
				}
				await main.IncreaseMainProgressValueAsync(taskStepAmount);
			}

			//await SetMainProgressTextAsync("Loading GM Campaigns...");
			//var loadedGMCampaigns = await LoadGameMasterCampaignsAsync(taskStepAmount);
			//await IncreaseMainProgressValueAsync(taskStepAmount);

			DivinityApp.Log("Loading external load orders...");
			await main.SetMainProgressTextAsync("Loading external load orders...");
			var savedModOrderList = await RunTask(LoadExternalLoadOrdersAsync(), []);
			await main.IncreaseMainProgressValueAsync(taskStepAmount);

			if (savedModOrderList.Count > 0)
			{
				DivinityApp.Log($"{savedModOrderList.Count} saved load orders found.");
			}
			else
			{
				DivinityApp.Log("No saved orders found.");
			}

			DivinityApp.Log("Setting up mod lists...");
			await main.SetMainProgressTextAsync("Setting up mod lists...");

			await Observable.Start(() =>
			{
				Services.Mods.SetLoadedMods(loadedMods, main.NexusModsSupportEnabled);
				//SetLoadedGMCampaigns(loadedGMCampaigns);

				profiles.Clear();
				profiles.AddOrUpdate(loadedProfiles);

				ExternalModOrders.Clear();
				ExternalModOrders.AddRange(savedModOrderList);

				var publicProfile = profiles.Items.FirstOrDefault(p => p.FolderName == "Public");
				var defaultIndex = 0;

				var sortedProfiles = loadedProfiles.OrderBy(x => x.FolderName != "Public").ThenBy(x => x.Name).ToList();

				if (String.IsNullOrWhiteSpace(selectedProfileUUID) || selectedProfileUUID == publicProfile?.UUID)
				{
					SelectedProfileIndex = defaultIndex;
				}
				else
				{
					var index = sortedProfiles.IndexOf(sortedProfiles.FirstOrDefault(p => p.UUID == selectedProfileUUID));
					if (index > -1)
					{
						SelectedProfileIndex = index;
					}
					else
					{
						SelectedProfileIndex = defaultIndex;
						DivinityApp.Log($"Profile '{selectedProfileUUID}' not found {profiles.Count}/{loadedProfiles.Count}.");
					}
				}

				SelectedProfile = sortedProfiles.ElementAt(SelectedProfileIndex);

				DivinityApp.Log($"Set profile to ({SelectedProfile?.Name})[{SelectedProfileIndex}] ({Profiles.Count})");

				main.MainProgressWorkText = "Building mod order list...";

				if (lastActiveOrder != null && lastActiveOrder.Count > 0)
				{
					SelectedModOrder?.SetOrder(lastActiveOrder);
				}
				BuildModOrderList(0, lastOrderName);
				main.MainProgressValue += taskStepAmount;
			}, RxApp.MainThreadScheduler);

			await main.IncreaseMainProgressValueAsync(taskStepAmount);
			await main.SetMainProgressTextAsync("Finishing up...");
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
					var activeAdventureMod = SelectedModOrder?.Order.FirstOrDefault(x => modManager.GetModType(x.UUID) == "Adventure");
					if (activeAdventureMod != null)
					{
						lastAdventureMod = activeAdventureMod.UUID;
					}
				}

				if (modManager.AdventureMods.Count > 0)
				{
					var defaultAdventureIndex = modManager.AdventureMods.IndexOf(modManager.AdventureMods.FirstOrDefault(x => x.UUID == DivinityApp.MAIN_CAMPAIGN_UUID));
					if (defaultAdventureIndex == -1) defaultAdventureIndex = 0;
					if (lastAdventureMod != null)
					{
						DivinityApp.Log($"Setting selected adventure mod.");
						var nextAdventureMod = modManager.AdventureMods.FirstOrDefault(x => x.UUID == lastAdventureMod);
						if (nextAdventureMod != null)
						{
							SelectedAdventureModIndex = modManager.AdventureMods.IndexOf(nextAdventureMod);
							if (nextAdventureMod.UUID == DivinityApp.GAMEMASTER_UUID)
							{
								Services.Settings.ManagerSettings.GameMasterModeEnabled = true;
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
				else
				{
					SelectedAdventureModIndex = 0;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error setting active adventure mod:\n{ex}");
			}

			DivinityApp.Log($"Finalizing refresh operation.");

			Services.Get<ModOrderView>()?.ModLayout?.RestoreLayout();

			IsLoadingOrder = false;

			Services.Mods.ApplyUserModConfig();
			IsRefreshing = false;
			ListenForProfileChanges(true);
		}, RxApp.MainThreadScheduler);
	}

	private static DivinityProfileActiveModData ProfileActiveModDataFromUUID(string uuid)
	{
		if (Services.Mods.TryGetMod(uuid, out var mod))
		{
			return mod.ToProfileModData();
		}
		return new DivinityProfileActiveModData()
		{
			UUID = uuid
		};
	}

	private void DisplayMissingMods(DivinityLoadOrder order = null)
	{
		var appSettings = Services.Settings.AppSettings;
		var settings = Services.Settings.ManagerSettings;
		var modManager = Services.Mods;
		bool displayExtenderModWarning = false;

		order ??= SelectedModOrder;
		if (order != null && settings.DisableMissingModWarnings != true)
		{
			List<DivinityMissingModData> missingMods = [];

			for (int i = 0; i < order.Order.Count; i++)
			{
				var entry = order.Order[i];
				if (modManager.TryGetMod(entry.UUID, out var mod))
				{
					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (!DivinityModDataLoader.IgnoreMod(dependency.UUID) && !modManager.ModExists(dependency.UUID) &&
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
				var message = String.Join("\n", missingMods.OrderBy(x => x.Index));
				var title = "Missing Mods in Load Order";
				DivinityInteractions.ShowMessageBox.Handle(new(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK)).Subscribe();
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

		if (settings.DisableMissingModWarnings != true && displayExtenderModWarning && appSettings.Features.ScriptExtender)
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
								if (modManager.TryGetMod(dependency.UUID, out var dependencyMod))
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
				var message = "Functionality may be limited without the Script Extender.\n" + String.Join("\n", extenderRequiredMods.OrderBy(x => x.Index));
				var title = "Mods Require the Script Extender";
				DivinityInteractions.ShowMessageBox.Handle(new(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK)).Subscribe();
			}
		}
	}

	#region Load Orders

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
				DivinityApp.ShowAlert($"Failed to save mod load order to '{outputPath}': {ex.Message}", AlertType.Danger);
				result = false;
			}

			if (result && !skipSaveConfirmation)
			{
				DivinityApp.ShowAlert($"Saved mod load order to '{outputPath}'", AlertType.Success, 10);
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
		var startDirectory = ModImportService.GetInitialStartingDirectory(ordersDir);

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

		if (dialog.ShowDialog(App.Current.MainWindow) == true)
		{
			var modManager = Services.Mods;
			outputPath = dialog.FileName;
			modOrderName = Path.GetFileNameWithoutExtension(outputPath);
			// Save mods that aren't missing
			var tempOrder = new DivinityLoadOrder { Name = modOrderName };
			tempOrder.Order.AddRange(SelectedModOrder.Order.Where(x => modManager.ModExists(x.UUID)));
			if (DivinityModDataLoader.ExportLoadOrderToFile(outputPath, tempOrder))
			{
				DivinityApp.ShowAlert($"Saved mod load order to '{outputPath}'", AlertType.Success, 10);
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
				DivinityApp.ShowAlert($"Failed to save mod load order to '{outputPath}'", AlertType.Danger);
			}
		}
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
		var settings = Services.Settings.ManagerSettings;
		if (!settings.GameMasterModeEnabled)
		{
			if (SelectedProfile != null && SelectedModOrder != null)
			{
				string outputPath = Path.Join(SelectedProfile.FilePath, "modsettings.lsx");
				var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, Services.Mods.AllMods, Settings.AutoAddDependenciesWhenExporting, SelectedAdventureMod);
				var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.FilePath, finalOrder);

				var dir = Services.Pathways.GetLarianStudiosAppDataFolder();
				if (SelectedModOrder.Order.Count > 0)
				{
					await DivinityModDataLoader.UpdateLauncherPreferencesAsync(dir, false, false, true);
				}
				else
				{
					if (settings.DisableLauncherTelemetry || settings.DisableLauncherModWarnings)
					{
						await DivinityModDataLoader.UpdateLauncherPreferencesAsync(dir, !settings.DisableLauncherTelemetry, !settings.DisableLauncherModWarnings);
					}
				}

				if (result)
				{
					await Observable.Start(() =>
					{
						DivinityApp.ShowAlert($"Exported load order to '{outputPath}'", AlertType.Success, 15);

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
					string message = $"Problem exporting load order to '{outputPath}'. Is the file locked?";
					var title = "Mod Order Export Failed";
					DivinityApp.ShowAlert(message, AlertType.Danger);
					await DivinityInteractions.ShowMessageBox.Handle(new(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK));
				}
			}
			else
			{
				await Observable.Start(() =>
				{
					DivinityApp.ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
				}, RxApp.MainThreadScheduler);
			}
		}
		else
		{
			if (SelectedGameMasterCampaign != null)
			{
				if (Services.Mods.TryGetMod(DivinityApp.GAMEMASTER_UUID, out var gmAdventureMod))
				{
					var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, Services.Mods.AllMods, Settings.AutoAddDependenciesWhenExporting);
					if (SelectedGameMasterCampaign.Export(finalOrder))
					{
						// Need to still write to modsettings.lsx
						finalOrder.Insert(0, gmAdventureMod);
						await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.FilePath, finalOrder);

						await Observable.Start(() =>
						{
							DivinityApp.ShowAlert($"Exported load order to '{SelectedGameMasterCampaign.FilePath}'", AlertType.Success, 15);

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
						string message = $"Problem exporting load order to '{SelectedGameMasterCampaign.FilePath}'";
						DivinityApp.ShowAlert(message, AlertType.Danger);
						DivinityInteractions.ShowMessageBox.Handle(new(message, "Mod Order Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK)).Subscribe();
					}
				}
			}
			else
			{
				DivinityApp.ShowAlert("SelectedGameMasterCampaign is null! Failed to export mod order", AlertType.Danger);
			}
		}

		return false;
	}

	public void BuildModOrderList(int selectIndex = -1, string lastOrderName = "")
	{
		if (SelectedProfile != null)
		{
			IsLoadingOrder = true;

			DivinityApp.Log($"Changing profile to ({SelectedProfile.FolderName})");

			List<DivinityMissingModData> missingMods = [];

			DivinityLoadOrder currentOrder = new() { Name = "Current", FilePath = Path.Join(SelectedProfile.FilePath, "modsettings.lsx"), IsModSettings = true };

			var modManager = Services.Mods;

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
						if (modManager.TryGetMod(uuid, out var mod))
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

			ModOrderList.AddRange(ExternalModOrders);

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

						Services.Settings.ManagerSettings.LastOrder = nextOrder?.Name ?? "";
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

	private async Task DeleteOrder(DivinityLoadOrder order)
	{
		var data = new ShowMessageBoxData($"Delete load order '{order.Name}'? This cannot be undone.", "Confirm Order Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
		var result = await DivinityInteractions.ShowMessageBox.Handle(data);
		if (result == MessageBoxResult.Yes)
		{
			SelectedModOrderIndex = 0;
			ModOrderList.Remove(order);
			if (!String.IsNullOrEmpty(order.FilePath) && File.Exists(order.FilePath))
			{
				RecycleBinHelper.DeleteFile(order.FilePath, false, false);
				DivinityApp.ShowAlert($"Sent load order '{order.FilePath}' to the recycle bin", AlertType.Warning, 25);
			}
		}
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

	#endregion

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

	public void ClearMissingMods()
	{
		var modManager = Services.Mods;
		var totalRemoved = SelectedModOrder != null ? SelectedModOrder.Order.RemoveAll(x => !modManager.ModExists(x.UUID)) : 0;

		if (totalRemoved > 0)
		{
			DivinityApp.ShowAlert($"Removed {totalRemoved} missing mods from the current order. Save to confirm", AlertType.Warning);
		}
	}

	public void RemoveDeletedMods(HashSet<string> deletedMods, bool removeFromLoadOrder = true)
	{
		Services.Mods.RemoveByUUID(deletedMods);

		if (removeFromLoadOrder)
		{
			SelectedModOrder.Order.RemoveAll(x => deletedMods.Contains(x.UUID));
			SelectedProfile.ModOrder.RemoveMany(deletedMods);
			SelectedProfile.ActiveMods.RemoveAll(x => deletedMods.Contains(x.UUID));
		}

		InactiveMods.RemoveMany(InactiveMods.Where(x => deletedMods.Contains(x.UUID)));
		ActiveMods.RemoveMany(ActiveMods.Where(x => deletedMods.Contains(x.UUID)));
	}

	public void DeleteMod(DivinityModData mod)
	{
		if (mod.CanDelete)
		{
			DivinityInteractions.DeleteMods.Handle(new([mod], false)).Subscribe();
		}
		else
		{
			DivinityApp.ShowAlert("Unable to delete mod", AlertType.Danger, 30);
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
			DivinityInteractions.DeleteMods.Handle(new(targetMods, false)).Subscribe();
		}
		else
		{
			DivinityApp.ShowAlert("Unable to delete selected mod(s)", AlertType.Danger, 30);
		}
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

	private int SortModOrder(DivinityLoadOrderEntry a, DivinityLoadOrderEntry b)
	{
		var modManager = Services.Mods;

		if (a != null && b != null)
		{
			modManager.TryGetMod(a.UUID, out var moda);
			modManager.TryGetMod(b.UUID, out var modb);
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

	public void AddNewModOrder(DivinityLoadOrder newOrder = null)
	{
		var lastIndex = SelectedModOrderIndex;
		var lastOrders = ModOrderList.ToList();

		var nextOrders = new List<DivinityLoadOrder>
		{
			SelectedProfile.SavedLoadOrder
		};
		nextOrders.AddRange(ExternalModOrders);

		void undo()
		{
			ExternalModOrders.Clear();
			ExternalModOrders.AddRange(lastOrders);
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
			ExternalModOrders.Add(newOrder);
			BuildModOrderList(ExternalModOrders.Count); // +1 due to Current being index 0
		};

		this.CreateSnapshot(undo, redo);

		redo();
	}

	public bool LoadModOrder() => LoadModOrder(SelectedModOrder);

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

		var modManager = Services.Mods;

		modManager.DeselectAllMods();

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
				if (modManager.TryGetMod(entry.UUID, out var mod))
				{
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
						var nextIndex = modManager.AdventureMods.IndexOf(mod);
						if (nextIndex != -1) SelectedAdventureModIndex = nextIndex;
					}

					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (!String.IsNullOrWhiteSpace(dependency.UUID) && !DivinityModDataLoader.IgnoreMod(dependency.UUID) && !modManager.ModExists(dependency.UUID))
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
				else
				{
					missingMods[entry.UUID] = new DivinityMissingModData
					{
						Index = i,
						Name = entry.Name,
						UUID = entry.UUID
					};
					entry.Missing = true;
				}
			}
		}

		ActiveMods.Clear();
		ActiveMods.AddRange(modManager.AddonMods.Where(x => x.CanAddToLoadOrder && x.IsActive).OrderBy(x => x.Index));
		InactiveMods.Clear();
		InactiveMods.AddRange(modManager.AddonMods.Where(x => x.CanAddToLoadOrder && !x.IsActive));

		OnFilterTextChanged(ActiveModFilterText, ActiveMods);
		OnFilterTextChanged(InactiveModFilterText, InactiveMods);
		OnFilterTextChanged(OverrideModsFilterText, modManager.ForceLoadedMods);

		if (missingMods.Count > 0)
		{
			var orderedMissingMods = missingMods.Values.OrderBy(x => x.Index).ToList();

			DivinityApp.Log($"Missing mods: {String.Join(";", orderedMissingMods)}");
			if (Services.Settings.ManagerSettings.DisableMissingModWarnings == true)
			{
				DivinityApp.Log("Skipping missing mod display.");
			}
			else
			{
				DivinityInteractions.ShowMessageBox.Handle(new(String.Join("\n", orderedMissingMods), "Missing Mods in Load Order", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK)).Subscribe();
			}
		}

		IsLoadingOrder = false;
		OrderJustLoadedCommand.Execute(order);
		return true;
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
		else if (modDataList == Services.Mods.ForceLoadedMods)
		{
			TotalOverrideModsHidden = totalHidden;
		}
		else if (modDataList == InactiveMods)
		{
			TotalInactiveModsHidden = totalHidden;
		}
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
				InitialDirectory = ModImportService.GetInitialStartingDirectory()
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

			if (dialog.ShowDialog(App.Current.MainWindow) == true)
			{
				var exportMods = new List<IModEntry>(ActiveMods);
				exportMods.AddRange(Services.Mods.ForceLoadedMods.ToList().OrderBy(x => x.Name));

				var fileType = Path.GetExtension(dialog.FileName);
				string outputText = "";
				if (fileType.Equals(".json", StringComparison.OrdinalIgnoreCase))
				{
					var serializedMods = exportMods.Where(x => x.EntryType == ModEntryType.Mod).Select(x => DivinitySerializedModData.FromMod((DivinityModData)x)).ToList();
					outputText = JsonConvert.SerializeObject(serializedMods, Formatting.Indented, new JsonSerializerSettings
					{
						NullValueHandling = NullValueHandling.Ignore
					});
				}
				else if (fileType.Equals(".tsv", StringComparison.OrdinalIgnoreCase))
				{
					outputText = "Index\tName\tAuthor\tFileName\tTags\tDependencies\tURL\n";
					outputText += String.Join("\n", exportMods.Select(x => x.Export(ModExportType.TSV)).Where(x => !String.IsNullOrEmpty(x)));
				}
				else
				{
					//Text file format
					outputText = String.Join("\n", exportMods.Select(x => x.Export(ModExportType.TXT)).Where(x => !String.IsNullOrEmpty(x)));
				}
				try
				{
					File.WriteAllText(dialog.FileName, outputText);
					DivinityApp.ShowAlert($"Exported order to '{dialog.FileName}'", AlertType.Success, 20);
				}
				catch (Exception ex)
				{
					DivinityApp.ShowAlert($"Error exporting mod order to '{dialog.FileName}':\n{ex}", AlertType.Danger);
				}
			}
		}
		else
		{
			DivinityApp.Log($"SelectedProfile({SelectedProfile}) SelectedModOrder({SelectedModOrder})");
			DivinityApp.ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
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

		dialog.InitialDirectory = ModImportService.GetInitialStartingDirectory(startPath);

		if (dialog.ShowDialog(App.Current.MainWindow) == true)
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
				DivinityApp.ShowAlert($"No mod order found in save \"{Path.GetFileNameWithoutExtension(dialog.FileName)}\"", AlertType.Danger, 30);
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
			InitialDirectory = ModImportService.GetInitialStartingDirectory(Settings.LastLoadedOrderFilePath)
		};

		if (dialog.ShowDialog(App.Current.MainWindow) == true)
		{
			Settings.LastLoadedOrderFilePath = Path.GetDirectoryName(dialog.FileName);
			Services.Settings.TrySaveAll(out _);
			DivinityApp.Log($"Loading order from '{dialog.FileName}'.");
			var newOrder = DivinityModDataLoader.LoadOrderFromFile(dialog.FileName, Services.Mods.AllMods);
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
							DivinityApp.ShowAlert($"Successfully overwrote order '{SelectedModOrder.Name}' with with imported order", AlertType.Success, 20);
						}
						else
						{
							DivinityApp.ShowAlert($"Failed to reset order to '{dialog.FileName}'", AlertType.Danger, 60);
						}
					}
					else
					{
						AddNewModOrder(newOrder);
						LoadModOrder(newOrder);
						DivinityApp.ShowAlert($"Successfully imported order '{newOrder.Name}'", AlertType.Success, 20);
					}
				}
				else
				{
					AddNewModOrder(newOrder);
					LoadModOrder(newOrder);
					DivinityApp.ShowAlert($"Successfully imported order '{newOrder.Name}'", AlertType.Success, 20);
				}
			}
			else
			{
				DivinityApp.ShowAlert($"Failed to import order from '{dialog.FileName}'", AlertType.Danger, 60);
			}
		}
	}
}