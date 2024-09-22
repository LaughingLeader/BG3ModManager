using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.VisualTree;

using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;

using ModManager.Controls.TreeDataGrid;
using ModManager.Extensions;
using ModManager.Models;
using ModManager.Models.Mod;
using ModManager.Models.Settings;
using ModManager.Services;
using ModManager.Util;
using ModManager.ViewModels.Mods;

using Newtonsoft.Json;

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;

using TextCopy;

namespace ModManager.ViewModels.Main;
public class ModOrderViewModel : ReactiveObject, IRoutableViewModel, IModOrderViewModel
{
	public string UrlPathSegment => "modorder";
	public IScreen HostScreen { get; }

	private readonly ModImportService ModImporter;
	private readonly IModManagerService ModManager;
	private readonly IInteractionsService _interactions;
	private readonly IGlobalCommandsService _globalCommands;
	private readonly IDialogService _dialogs;

	private readonly IFileWatcherWrapper _modSettingsWatcher;

	public MainCommandBarViewModel CommandBar { get; }

	public static DivinityPathwayData PathwayData => AppServices.Pathways.Data;
	public static ModManagerSettings Settings => AppServices.Settings.ManagerSettings;
	public static AppSettings AppSettings => AppServices.Settings.AppSettings;

	//public IModViewLayout Layout { get; set; }

	//public ModListDropHandler DropHandler { get; }
	//public ModListDragHandler DragHandler { get; }

	private readonly SourceCache<DivinityProfileData, string> profiles = new(x => x.FilePath);

	private readonly ReadOnlyObservableCollection<DivinityProfileData> _uiprofiles;
	public ReadOnlyObservableCollection<DivinityProfileData> Profiles => _uiprofiles;

	public ObservableCollectionExtended<IModEntry> ActiveMods { get; }
	public ObservableCollectionExtended<IModEntry> InactiveMods { get; }

	private readonly ReadOnlyObservableCollection<IModEntry> _overrideMods;
	public ReadOnlyObservableCollection<IModEntry> OverrideMods => _overrideMods;

	private readonly ReadOnlyObservableCollection<DivinityModData> _adventureMods;
	public ReadOnlyObservableCollection<DivinityModData> AdventureMods => _adventureMods;

	public ModListViewModel ActiveModsView { get; }
	public ModListViewModel OverrideModsView { get; }
	public ModListViewModel InactiveModsView { get; }


	public ObservableCollectionExtended<DivinityLoadOrder> ModOrderList { get; }
	public List<DivinityLoadOrder> ExternalModOrders { get; }

	[ObservableAsProperty] public ObservableCollectionExtended<IModEntry>? FocusedList { get; }

	[Reactive] public bool IsRenamingOrder { get; set; }
	[Reactive] public bool IsRefreshing { get; private set; }
	[Reactive] public bool IsLoadingOrder { get; set; }
	[Reactive] public bool IsLocked { get; private set; }

	[Reactive] public bool CanMoveSelectedMods { get; set; }
	[Reactive] public bool CanSaveOrder { get; set; }

	[Reactive] public int SelectedProfileIndex { get; set; }
	[Reactive] public int SelectedModOrderIndex { get; set; }
	[Reactive] public int SelectedAdventureModIndex { get; set; }

	[Reactive] public DivinityProfileData? SelectedProfile { get; set; }
	[Reactive] public DivinityLoadOrder? SelectedModOrder { get; set; }
	[Reactive] public DivinityModData? SelectedAdventureMod { get; set; }

	[ObservableAsProperty] public string SelectedModOrderName { get; }

	[ObservableAsProperty] public bool AdventureModBoxVisibility { get; }
	[ObservableAsProperty] public bool LogFolderShortcutButtonVisibility { get; }
	[ObservableAsProperty] public bool OverrideModsVisibility { get; }

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

	public ReactiveCommand<DivinityLoadOrder, Unit> DeleteOrderCommand { get; }
	public ReactiveCommand<object, Unit> ToggleOrderRenamingCommand { get; set; }
	public RxCommandUnit FocusFilterCommand { get; set; }
	public RxCommandUnit CopyOrderToClipboardCommand { get; }
	public RxCommandUnit ExportOrderAsListCommand { get; }
	public ReactiveCommand<DivinityLoadOrder, Unit> OrderJustLoadedCommand { get; set; }
	public RxCommandUnit OpenGameMasterCampaignInFileExplorerCommand { get; private set; }
	public RxCommandUnit CopyGameMasterCampaignPathToClipboardCommand { get; private set; }

	private static IObservable<bool> AllTrue(IObservable<bool> first, IObservable<bool> second) => first.CombineLatest(second).Select(x => x.First && x.Second);

	private static string GetLaunchGameTooltip(ValueTuple<string?, bool, bool, bool> x)
	{
		var exePath = x.Item1;
		var limitToSingle = x.Item2;
		var isRunning = x.Item3;
		var canForce = x.Item4;
		if (exePath?.IsExistingFile() == true)
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

	/*private void SetupKeys(AppKeys keys, MainWindowViewModel main, IObservable<bool> canExecuteCommands)
	{
		var modImporter = AppServices.Get<ModImportService>();

		var canExecuteSaveCommand = AllTrue(canExecuteCommands, this.WhenAnyValue(x => x.CanSaveOrder));
		keys.Save.AddAsyncAction(SaveLoadOrderAsync, canExecuteSaveCommand);

		keys.SaveAs.AddAsyncAction(SaveLoadOrderAs, canExecuteSaveCommand);
		keys.ImportMod.AddAsyncAction(modImporter.OpenModImportDialog, canExecuteCommands);
		keys.ImportNexusModsIds.AddAsyncAction(modImporter.OpenModIdsImportDialog, canExecuteCommands);
		keys.NewOrder.AddAction(() => AddNewModOrder(), canExecuteCommands);

		var anyActiveObservable = ActiveMods.WhenAnyValue(x => x.Count).Select(x => x > 0);
		//var anyActiveObservable = this.WhenAnyValue(x => x.ActiveMods.Count, (c) => c > 0);
		keys.ExportOrderToList.AddAsyncAction(ExportLoadOrderToTextFileAs, anyActiveObservable);

		keys.ImportOrderFromSave.AddAsyncAction(ImportOrderFromSaveToCurrent, canExecuteCommands);
		keys.ImportOrderFromSaveAsNew.AddAsyncAction(ImportOrderFromSaveAsNew, canExecuteCommands);
		keys.ImportOrderFromFile.AddAsyncAction(ImportOrderFromFile, canExecuteCommands);
		keys.ImportOrderFromZipFile.AddAsyncAction(modImporter.ImportOrderFromArchive, canExecuteCommands);

		keys.ExportOrderToGame.AddAsyncAction(ExportLoadOrderAsync, AllTrue(canExecuteCommands, this.WhenAnyValue(x => x.SelectedProfile).Select(x => x != null)));

		keys.DeleteSelectedMods.AddAction(() =>
		{
			var allMods = ModManager.GetAllModsAsInterface();
			IEnumerable<IModEntry>? targetList = null;
			if (DivinityApp.IsKeyboardNavigating)
			{
				targetList = FocusedList;
			}
			else
			{
				targetList = allMods;
			}

			if (targetList != null)
			{
				var selectedMods = targetList.Where(x => x.IsSelected);
				var selectedEligableMods = selectedMods.Where(x => x.CanDelete).ToList();

				if (selectedEligableMods.Count > 0)
				{
					_interactions.DeleteMods.Handle(new(selectedEligableMods, false)).Subscribe();
				}

				if (selectedMods.Any(x => x.EntryType == ModEntryType.Mod && ((ModEntry)x)?.Data?.IsEditorMod == true))
				{
					AppServices.Commands.ShowAlert("Editor mods cannot be deleted with the Mod Manager", AlertType.Warning, 60);
				}
			}
		}, canExecuteCommands);

		keys.SpeakActiveModOrder.AddAction(() =>
		{
			//TODO Update since ScreenReaderHelper used native dlls like Tolk
			*//*if (ActiveMods.Count > 0)
			{
				var text = String.Join(", ", ActiveMods.Select(x => x.DisplayName));
				ScreenReaderHelper.Speak($"{ActiveMods.Count} mods in the active order, including:", true);
				ScreenReaderHelper.Speak(text, false);
				//ShowAlert($"Active mods: {text}", AlertType.Info, 10);
			}
			else
			{
				//ShowAlert($"No mods in active order.", AlertType.Warning, 10);
				ScreenReaderHelper.Speak($"The active mods order is empty.");
			}*//*
		}, canExecuteCommands);
	}*/

	private readonly SortExpressionComparer<DivinityProfileData> _profileSort = SortExpressionComparer<DivinityProfileData>.Ascending(p => p.FolderName != "Public").ThenByAscending(p => p.Name);

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

	public void Clear()
	{
		_lastProfile = null;

		profiles.Clear();
		ExternalModOrders.Clear();
		ModOrderList.Clear();
	}

	public void LoadCurrentProfile()
	{
		var profile = Profiles[SelectedProfileIndex];
		if(profile != null)
		{
			BuildModOrderList(profile, Math.Max(0, SelectedModOrderIndex));
		}
		else
		{
			DivinityApp.Log($"No profile found for index ({SelectedProfileIndex})");
		}
	}

	public async Task RefreshAsync(MainWindowViewModel main, CancellationToken token)
	{
		IsRefreshing = true;
		DivinityApp.Log($"Refreshing data asynchronously...");

		var taskStepAmount = 100 / 6;

		var modManager = ModManager;

		List<DivinityLoadOrderEntry>? lastActiveOrder = null;
		var lastOrderName = "";
		if (SelectedModOrder != null)
		{
			lastActiveOrder = [.. SelectedModOrder.Order];
			lastOrderName = SelectedModOrder.Name;
		}

		string? lastAdventureMod = null;
		if (SelectedAdventureMod != null) lastAdventureMod = SelectedAdventureMod.UUID;

		var selectedProfileUUID = "";
		if (SelectedProfile != null)
		{
			selectedProfileUUID = SelectedProfile.UUID;
		}

		if (Directory.Exists(PathwayData.AppDataGameFolder))
		{
			DivinityApp.Log("Loading mods...");
			main.Progress.WorkText = "Loading mods...";
			var loadedMods = await ModManager.LoadModsAsync(Settings.GameDataPath, PathwayData.AppDataModsPath, token);
			main.Progress.IncreaseValue(taskStepAmount);

			DivinityApp.Log("Loading profiles...");
			main.Progress.WorkText = "Loading profiles...";
			var loadedProfiles = await LoadProfilesAsync();
			main.Progress.IncreaseValue(taskStepAmount);

			if (String.IsNullOrEmpty(selectedProfileUUID) && (loadedProfiles != null && loadedProfiles.Count > 0))
			{
				DivinityApp.Log("Loading current profile...");
				main.Progress.WorkText = "Loading current profile...";
				selectedProfileUUID = await DivinityModDataLoader.GetSelectedProfileUUIDAsync(PathwayData.AppDataProfilesPath);
				main.Progress.IncreaseValue(taskStepAmount);
			}
			else
			{
				if ((loadedProfiles == null || loadedProfiles.Count == 0))
				{
					DivinityApp.Log("No profiles found?");
				}
				main.Progress.IncreaseValue(taskStepAmount);
			}

			//await SetMainProgressTextAsync("Loading GM Campaigns...");
			//var loadedGMCampaigns = await LoadGameMasterCampaignsAsync(taskStepAmount);
			//await IncreaseMainProgressValueAsync(taskStepAmount);

			DivinityApp.Log("Loading external load orders...");
			main.Progress.WorkText = "Loading external load orders...";
			var savedModOrderList = await LoadExternalLoadOrdersAsync();
			main.Progress.IncreaseValue(taskStepAmount);

			if (savedModOrderList.Count > 0)
			{
				DivinityApp.Log($"{savedModOrderList.Count} saved load orders found.");
			}
			else
			{
				DivinityApp.Log($"No saved orders found in {Settings.LoadOrderPath}");
			}

			DivinityApp.Log("Setting up mod lists...");
			main.Progress.WorkText = "Setting up profiles & orders...";

			await Observable.Start(() =>
			{
				if (loadedMods.Count > 0) ModManager.SetLoadedMods(loadedMods, main.NexusModsSupportEnabled);
				//SetLoadedGMCampaigns(loadedGMCampaigns);
				if (loadedProfiles != null) profiles.AddOrUpdate(loadedProfiles);
				ExternalModOrders.AddRange(savedModOrderList);
			}, RxApp.MainThreadScheduler);

			main.Progress.IncreaseValue(taskStepAmount);
			main.Progress.WorkText = "Finishing up...";
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

			IsLoadingOrder = false;

			ModManager.ApplyUserModConfig();
			IsRefreshing = false;
		}, RxApp.MainThreadScheduler);

		if (profiles.Count > 0)
		{
			RxApp.MainThreadScheduler.Schedule(() =>
			{
				var publicProfile = Profiles.FirstOrDefault(p => p.FolderName == "Public");
				var defaultIndex = 0;

				if (String.IsNullOrWhiteSpace(selectedProfileUUID) || selectedProfileUUID == publicProfile?.UUID)
				{
					SelectedProfileIndex = defaultIndex;
				}
				else
				{
					var element = Profiles.FirstOrDefault(p => p.UUID == selectedProfileUUID);
					var index = element != null ? Profiles.IndexOf(element) : defaultIndex;
					if (index > -1)
					{
						SelectedProfileIndex = index;
					}
					else
					{
						SelectedProfileIndex = defaultIndex;
						DivinityApp.Log($"Profile '{selectedProfileUUID}' not found.");
					}
				}

				if (Profiles.ElementAtOrDefault(SelectedProfileIndex) is DivinityProfileData profile) SelectedProfile = profile;
				if (ModOrderList.ElementAtOrDefault(SelectedModOrderIndex) is DivinityLoadOrder order) SelectedModOrder = order;
				if (AdventureMods.ElementAtOrDefault(SelectedAdventureModIndex) is DivinityModData adventureMod) SelectedAdventureMod = adventureMod;

				/*if (lastActiveOrder != null && lastActiveOrder.Count > 0)
				{
					SelectedModOrder?.SetOrder(lastActiveOrder);
				}*/

				DivinityApp.Log($"SelectedProfile({SelectedProfileIndex}:{SelectedProfile?.FolderName}) | SelectedModOrder({SelectedModOrderIndex}:{SelectedModOrder?.Name}) SelectedAdventureMod({SelectedAdventureModIndex}:{SelectedAdventureMod?.Name})");
			});
		}
	}

	private DivinityProfileActiveModData ProfileActiveModDataFromUUID(string uuid)
	{
		if (ModManager.TryGetMod(uuid, out var mod))
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
		var displayExtenderModWarning = false;

		order ??= SelectedModOrder;
		if (order != null && Settings.DisableMissingModWarnings != true)
		{
			List<DivinityMissingModData> missingMods = [];

			for (var i = 0; i < order.Order.Count; i++)
			{
				var entry = order.Order[i];
				if (ModManager.TryGetMod(entry.UUID, out var mod))
				{
					if (mod.Dependencies.Count > 0)
					{
						foreach (var dependency in mod.Dependencies.Items)
						{
							if (!DivinityModDataLoader.IgnoreMod(dependency.UUID) && !ModManager.ModExists(dependency.UUID) &&
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
				_interactions.ShowMessageBox.Handle(new(title, message, InteractionMessageBoxType.Error)).Subscribe();
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

		if (Settings.DisableMissingModWarnings != true && displayExtenderModWarning && AppSettings.Features.ScriptExtender)
		{
			//DivinityApp.LogMessage($"Mod Order: {String.Join("\n", order.Order.Select(x => x.Name))}");
			DivinityApp.Log("Checking mods for extender requirements.");
			List<DivinityMissingModData> extenderRequiredMods = [];
			for (var i = 0; i < order.Order.Count; i++)
			{
				var entry = order.Order[i];
				if (ModManager.TryGetMod(entry.UUID, out var mod))
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
								if (ModManager.TryGetMod(dependency.UUID, out var dependencyMod))
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
				_interactions.ShowMessageBox.Handle(new(title, message, InteractionMessageBoxType.Error)).Subscribe();
			}
		}
	}

	#region Load Orders

	private async Task<List<DivinityLoadOrder>> LoadExternalLoadOrdersAsync()
	{
		try
		{
			var loadOrderDirectory = Settings.LoadOrderPath;
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
			return [];
		}
	}

	public async Task<bool> SaveLoadOrderAsync() => await SaveLoadOrderAsync(false);

	public async Task<bool> SaveLoadOrderAsync(bool skipSaveConfirmation = false)
	{
		var result = false;
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			var outputDirectory = Settings.LoadOrderPath;

			if (String.IsNullOrWhiteSpace(outputDirectory))
			{
				outputDirectory = AppDomain.CurrentDomain.BaseDirectory;
			}

			if (!Directory.Exists(outputDirectory))
			{
				Directory.CreateDirectory(outputDirectory);
			}

			var outputPath = SelectedModOrder.FilePath;
			var outputName = DivinityModDataLoader.MakeSafeFilename(Path.Join(SelectedModOrder.Name + ".json"), '_');

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
				AppServices.Commands.ShowAlert($"Failed to save mod load order to '{outputPath}': {ex.Message}", AlertType.Danger);
				result = false;
			}

			if (result && !skipSaveConfirmation)
			{
				AppServices.Commands.ShowAlert($"Saved mod load order to '{outputPath}'", AlertType.Success, 10);
			}
		}

		return result;
	}

	public async Task<bool> SaveLoadOrderAs()
	{
		var ordersDir = Settings.LoadOrderPath;
		//Relative path
		if (Settings.LoadOrderPath.IndexOf(@":\") == -1)
		{
			ordersDir = DivinityApp.GetAppDirectory(Settings.LoadOrderPath);
			if (!Directory.Exists(ordersDir)) Directory.CreateDirectory(ordersDir);
		}
		var startDirectory = ModImportService.GetInitialStartingDirectory(ordersDir);

		var outputPath = Path.Join(SelectedModOrder.Name + ".json");
		if (SelectedModOrder.IsModSettings)
		{
			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-") + "_HH-mm-ss";
			outputPath = $"Current_{DateTime.Now.ToString(sysFormat)}.json";
		}

		outputPath = DivinityModDataLoader.MakeSafeFilename(outputPath, '_');
		var modOrderName = Path.GetFileNameWithoutExtension(outputPath);

		var result = await _dialogs.SaveFileAsync(new(
			"Save Load Order As...",
			Path.Join(startDirectory, outputPath),
			[CommonFileTypes.Json]
		));

		if (result.Success)
		{
			var modManager = ModManager;
			outputPath = result.File!;
			modOrderName = Path.GetFileNameWithoutExtension(outputPath)!;
			// Save mods that aren't missing
			var tempOrder = new DivinityLoadOrder { Name = modOrderName };
			tempOrder.Order.AddRange(SelectedModOrder.Order.Where(x => modManager.ModExists(x.UUID)));
			if (DivinityModDataLoader.ExportLoadOrderToFile(outputPath, tempOrder))
			{
				AppServices.Commands.ShowAlert($"Saved mod load order to '{outputPath}'", AlertType.Success, 10);
				var updatedOrder = false;
				var updatedOrderIndex = -1;
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
				return true;
			}
			else
			{
				AppServices.Commands.ShowAlert($"Failed to save mod load order to '{outputPath}'", AlertType.Danger);
			}
		}
		return false;
	}

	public async Task<bool> ExportLoadOrderAsync()
	{
		var settings = Settings;
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			var outputPath = Path.Join(SelectedProfile.FilePath, "modsettings.lsx");
			var finalOrder = DivinityModDataLoader.BuildOutputList(SelectedModOrder.Order, ModManager.AllMods, Settings.AutoAddDependenciesWhenExporting, SelectedAdventureMod);
			var result = await DivinityModDataLoader.ExportModSettingsToFileAsync(SelectedProfile.FilePath, finalOrder);

			var dir = AppServices.Pathways.GetLarianStudiosAppDataFolder();
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
					AppServices.Commands.ShowAlert($"Exported load order to '{outputPath}'", AlertType.Success, 15);

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
				}, RxApp.MainThreadScheduler);
				return true;
			}
			else
			{
				var message = $"Problem exporting load order to '{outputPath}'. Is the file locked?";
				var title = "Mod Order Export Failed";
				AppServices.Commands.ShowAlert(message, AlertType.Danger);
				await _interactions.ShowMessageBox.Handle(new(title, message, InteractionMessageBoxType.Error));
			}
		}
		else
		{
			await Observable.Start(() =>
			{
				AppServices.Commands.ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
			}, RxApp.MainThreadScheduler);
		}
		return false;
	}

	private DivinityProfileData? _lastProfile;

	public void BuildModOrderList(DivinityProfileData profile, int selectIndex = -1, string lastOrderName = "")
	{
		if (profile != null)
		{
			IsLoadingOrder = true;

			DivinityApp.Log($"Changing profile to ({profile.FolderName}|{profile.FilePath})");

			List<DivinityMissingModData> missingMods = [];

			DivinityLoadOrder currentOrder = new DivinityLoadOrder() { Name = "Current", FilePath = profile.ModSettingsFile, IsModSettings = true };

			var modManager = ModManager;

			var i = 0;
			foreach (var activeMod in profile.ActiveMods)
			{
				if (modManager.TryGetMod(activeMod.UUID, out var mod))
				{
					currentOrder.Add(mod);
				}
				else
				{
					var x = new DivinityMissingModData
					{
						Index = i,
						Name = activeMod.Name,
						UUID = activeMod.UUID
					};
					missingMods.Add(x);
				}
				i++;
			}

			ModOrderList.Clear();
			ModOrderList.Add(currentOrder);

			DivinityApp.Log($"Profile ({profile.Name}) order: {String.Join(";", profile.ActiveMods.Select(x => x.Name))}");

			ModOrderList.AddRange(ExternalModOrders);

			DivinityApp.Log($"ModOrderList: {String.Join(";", ModOrderList.Select(x => x.Name))}");

			if (!String.IsNullOrEmpty(lastOrderName))
			{
				var lastOrderIndex = ModOrderList.IndexOf(ModOrderList.FirstOrDefault(x => x.Name == lastOrderName));
				if (lastOrderIndex != -1) selectIndex = lastOrderIndex;
			}

			RxApp.MainThreadScheduler.Schedule(() =>
			{
				if (selectIndex != -1)
				{
					if (selectIndex >= ModOrderList.Count) selectIndex = ModOrderList.Count - 1;
					DivinityApp.Log($"Setting next order index to [{selectIndex}] ({ModOrderList.Count} total).");
					try
					{
						SelectedModOrderIndex = selectIndex;
						if (ModOrderList.ElementAtOrDefault(SelectedModOrderIndex) is DivinityLoadOrder order) SelectedModOrder = order;

						if(SelectedModOrder != null)
						{
							LoadModOrder(SelectedModOrder, missingMods);
						}
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
		var data = new ShowMessageBoxRequest("Confirm Order Deletion", 
			$"Delete load order '{order.Name}'? This cannot be undone.",
			InteractionMessageBoxType.Warning | InteractionMessageBoxType.YesNo);
		var result = await _interactions.ShowMessageBox.Handle(data);
		if (result)
		{
			SelectedModOrderIndex = 0;
			ModOrderList.Remove(order);
			if (!String.IsNullOrEmpty(order.FilePath) && File.Exists(order.FilePath))
			{
				RecycleBinHelper.DeleteFile(order.FilePath, false, false);
				AppServices.Commands.ShowAlert($"Sent load order '{order.FilePath}' to the recycle bin", AlertType.Warning, 25);
			}
		}
	}

	public async Task<List<DivinityProfileData>?> LoadProfilesAsync()
	{
		if (Directory.Exists(PathwayData.AppDataProfilesPath))
		{
			DivinityApp.Log($"Loading profiles from '{PathwayData.AppDataProfilesPath}'.");

			var profiles = await DivinityModDataLoader.LoadProfileDataAsync(PathwayData.AppDataProfilesPath);
			DivinityApp.Log($"Loaded '{profiles.Count}' profiles.\n{string.Join(';', profiles.Select(x => x.FolderName))}");
			return profiles;
		}
		else
		{
			DivinityApp.Log($"Profile folder not found at '{PathwayData.AppDataProfilesPath}'.");
		}
		return null;
	}

	#endregion

	public void AddActiveMod(IModEntry mod)
	{
		if (!ActiveMods.Any(x => x.UUID == mod.UUID))
		{
			ActiveMods.Add(mod);
			mod.Index = ActiveMods.Count - 1;
			SelectedModOrder.Add(mod);
		}
		InactiveMods.Remove(mod);
	}

	public void RemoveActiveMod(IModEntry mod)
	{
		SelectedModOrder.Remove(mod);
		ActiveMods.Remove(mod);
		if (mod.EntryType == ModEntryType.Mod && mod is ModEntry modEntry && (modEntry.Data.IsForceLoadedMergedMod || !modEntry.Data.IsForceLoaded))
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
		var modManager = ModManager;
		var totalRemoved = SelectedModOrder != null ? SelectedModOrder.Order.RemoveAll(x => !modManager.ModExists(x.UUID)) : 0;

		if (totalRemoved > 0)
		{
			AppServices.Commands.ShowAlert($"Removed {totalRemoved} missing mods from the current order. Save to confirm", AlertType.Warning);
		}
	}

	public void RemoveDeletedMods(HashSet<string> deletedMods, bool removeFromLoadOrder = true)
	{
		ModManager.RemoveByUUID(deletedMods);

		if (removeFromLoadOrder)
		{
			SelectedModOrder.Order.RemoveAll(x => deletedMods.Contains(x.UUID));
			SelectedProfile.ActiveMods.RemoveAll(x => deletedMods.Contains(x.UUID));
		}

		InactiveMods.RemoveMany(InactiveMods.Where(x => deletedMods.Contains(x.UUID)));
		ActiveMods.RemoveMany(ActiveMods.Where(x => deletedMods.Contains(x.UUID)));
	}

	public void DeleteMod(IModEntry mod)
	{
		if (mod.CanDelete)
		{
			_interactions.DeleteMods.Handle(new([mod], false)).Subscribe();
		}
		else
		{
			AppServices.Commands.ShowAlert("Unable to delete mod", AlertType.Danger, 30);
		}
	}

	public void DeleteSelectedMods(IModEntry contextMenuMod)
	{
		var list = contextMenuMod.IsActive ? ActiveMods : InactiveMods;
		var targetMods = new List<IModEntry>();
		targetMods.AddRange(list.Where(x => x.CanDelete && x.IsSelected));
		if (!contextMenuMod.IsSelected && contextMenuMod.CanDelete) targetMods.Add(contextMenuMod);
		if (targetMods.Count > 0)
		{
			_interactions.DeleteMods.Handle(new(targetMods, false)).Subscribe();
		}
		else
		{
			AppServices.Commands.ShowAlert("Unable to delete selected mod(s)", AlertType.Danger, 30);
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

	private async Task ToggleRenamingLoadOrder(object control)
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
				var tb = comboBox.GetVisualChildren().OfType<TextBox>().FirstOrDefault();
				if (tb != null)
				{
					tb.Focus();
					if (IsRenamingOrder)
					{
						tb.SelectAll();
					}
					else
					{
						tb.ClearSelection();
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
					tb.ClearSelection();
				}
			}
		});
	}

	private int SortModOrder(DivinityLoadOrderEntry a, DivinityLoadOrderEntry b)
	{
		var modManager = ModManager;

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

	public void AddNewModOrder(DivinityLoadOrder? newOrder = null)
	{
		if (newOrder == null)
		{
			newOrder = new DivinityLoadOrder()
			{
				Name = $"New{ExternalModOrders.Count + 1}",
				Order = ActiveMods.Where(x => x.EntryType == ModEntryType.Mod).Cast<ModEntry>().Select(m => m.Data.ToOrderEntry()).ToList()
			};
			newOrder.FilePath = Path.Join(Settings.LoadOrderPath, DivinityModDataLoader.MakeSafeFilename(Path.Join(newOrder.Name + ".json"), '_'));
		}
		ExternalModOrders.Add(newOrder);
		BuildModOrderList(SelectedProfile, ExternalModOrders.Count); // +1 due to Current being index 0
	}

	public bool LoadModOrder() => LoadModOrder(SelectedModOrder);

	public bool LoadModOrder(DivinityLoadOrder order, List<DivinityMissingModData>? missingModsFromProfileOrder = null)
	{
		if (order == null) return false;

		IsLoadingOrder = true;

		var loadFrom = order.Order;
		var modManager = ModManager;

		foreach (var mod in modManager.AddonMods)
		{
			mod.IsActive = false;
			mod.Index = -1;
		}

		modManager.DeselectAllMods();

		DivinityApp.Log($"Loading mod order '{order.Name}':\n{string.Join(";", order.Order.Select(x => x.Name))}");
		Dictionary<string, DivinityMissingModData> missingMods = [];
		if (missingModsFromProfileOrder != null && missingModsFromProfileOrder.Count > 0)
		{
			missingModsFromProfileOrder.ForEach(x => missingMods[x.UUID] = x);
			DivinityApp.Log($"Missing mods (from profile): {String.Join(";", missingModsFromProfileOrder)}");
		}

		var loadOrderIndex = 0;

		for (var i = 0; i < loadFrom.Count; i++)
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
						var nextIndex = AdventureMods.IndexOf(mod);
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
		var activeMods = modManager.AddonMods.Where(x => x.CanAddToLoadOrder && x.IsActive).OrderBy(x => x.Index).ToList();
		if(activeMods.Count > 0)
		{
			foreach(var mod in activeMods)
			{
				ActiveMods.Add(mod.ToModInterface());
			}
		}

		InactiveMods.Clear();
		var inactiveMods = modManager.AddonMods.Where(x => x.CanAddToLoadOrder && !x.IsActive).ToList();
		if(inactiveMods.Count > 0)
		{
			foreach (var mod in inactiveMods)
			{
				InactiveMods.Add(mod.ToModInterface());
			}
		}

		if (missingMods.Count > 0)
		{
			var orderedMissingMods = missingMods.Values.OrderBy(x => x.Index).ToList();

			DivinityApp.Log($"Missing mods: {String.Join(";", orderedMissingMods)}");
			if (Settings.DisableMissingModWarnings == true)
			{
				DivinityApp.Log("Skipping missing mod display.");
			}
			else
			{
				_interactions.ShowMessageBox.Handle(new("Missing Mods in Load Order", string.Join("\n", orderedMissingMods), InteractionMessageBoxType.Warning)).Subscribe();
			}
		}

		IsLoadingOrder = false;
		OrderJustLoadedCommand.Execute(order);

		order.IsLoaded = true;

		Settings.LastOrder = order.Name;

		return true;
	}

	private async Task ExportLoadOrderToTextFileAs()
	{
		if (SelectedProfile != null && SelectedModOrder != null)
		{
			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
			var baseOrderName = SelectedModOrder.Name;
			if (SelectedModOrder.IsModSettings)
			{
				baseOrderName = $"{SelectedProfile.Name}_{SelectedModOrder.Name}";
			}
			var outputName = $"{baseOrderName}-{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.tsv";

			var result = await _dialogs.SaveFileAsync(new(
				"Export Load Order As Text File...",
				Path.Join(ModImportService.GetInitialStartingDirectory(), DivinityModDataLoader.MakeSafeFilename(outputName, '_')),
				CommonFileTypes.ModOrderFileTypes
			));

			if (result.Success)
			{
				var filePath = result.File!;
				var exportMods = new List<IModEntry>(ActiveMods);
				exportMods.AddRange(ModManager.ForceLoadedMods.ToList().OrderBy(x => x.Name).ToModInterface());

				var fileType = Path.GetExtension(filePath)!;
				var outputText = "";
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
					File.WriteAllText(filePath, outputText);
					AppServices.Commands.ShowAlert($"Exported order to '{filePath}'", AlertType.Success, 20);
				}
				catch (Exception ex)
				{
					AppServices.Commands.ShowAlert($"Error exporting mod order to '{filePath}':\n{ex}", AlertType.Danger);
				}
			}
		}
		else
		{
			DivinityApp.Log($"SelectedProfile({SelectedProfile}) SelectedModOrder({SelectedModOrder})");
			AppServices.Commands.ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
		}
	}

	private async Task<DivinityLoadOrder?> ImportOrderFromSave()
	{
		var startPath = "";
		if (SelectedProfile != null)
		{
			var profilePath = Path.GetFullPath(Path.Join(SelectedProfile.FilePath, "Savegames"));
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

		var result = await _dialogs.OpenFileAsync(new(
			"Load Mod Order From Save...",
			ModImportService.GetInitialStartingDirectory(startPath),
			[CommonFileTypes.LarianSaveFile, CommonFileTypes.All]
		));

		if (result.Success)
		{
			PathwayData.LastSaveFilePath = Path.GetDirectoryName(result.File);
			DivinityApp.Log($"Loading order from '{result.File}'.");
			var newOrder = DivinityModDataLoader.GetLoadOrderFromSave(result.File, Settings.LoadOrderPath);
			if (newOrder != null)
			{
				DivinityApp.Log($"Imported mod order: {String.Join(Environment.NewLine + "\t", newOrder.Order.Select(x => x.Name))}");
				return newOrder;
			}
			else
			{
				DivinityApp.Log($"Failed to load order from '{result.File}'.");
				AppServices.Commands.ShowAlert($"No mod order found in save \"{Path.GetFileNameWithoutExtension(result.File)}\"", AlertType.Danger, 30);
			}
		}

		return null;
	}

	private async Task ImportOrderFromSaveAsNew()
	{
		var order = await ImportOrderFromSave();
		if (order != null)
		{
			AddNewModOrder(order);
		}
	}

	private async Task ImportOrderFromSaveToCurrent()
	{
		var order = await ImportOrderFromSave();
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

	private async Task ImportOrderFromFile()
	{
		var result = await _dialogs.OpenFileAsync(new(
			"Load Mod Order From File...",
			ModImportService.GetInitialStartingDirectory(Settings.LastLoadedOrderFilePath),
			CommonFileTypes.ModOrderFileTypes
		));

		if (result.Success)
		{
			Settings.LastLoadedOrderFilePath = Path.GetDirectoryName(result.File)!;
			Settings.Save(out _);
			DivinityApp.Log($"Loading order from '{result.File}'.");
			var newOrder = DivinityModDataLoader.LoadOrderFromFile(result.File, ModManager.AllMods);
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
							AppServices.Commands.ShowAlert($"Successfully overwrote order '{SelectedModOrder.Name}' with with imported order", AlertType.Success, 20);
						}
						else
						{
							AppServices.Commands.ShowAlert($"Failed to reset order to '{result.File}'", AlertType.Danger, 60);
						}
					}
					else
					{
						AddNewModOrder(newOrder);
						LoadModOrder(newOrder);
						AppServices.Commands.ShowAlert($"Successfully imported order '{newOrder.Name}'", AlertType.Success, 20);
					}
				}
				else
				{
					AddNewModOrder(newOrder);
					LoadModOrder(newOrder);
					AppServices.Commands.ShowAlert($"Successfully imported order '{newOrder.Name}'", AlertType.Success, 20);
				}
			}
			else
			{
				AppServices.Commands.ShowAlert($"Failed to import order from '{result.File}'", AlertType.Danger, 60);
			}
		}
	}

	public ModOrderViewModel(MainWindowViewModel host,
		IModManagerService modManagerService,
		IFileWatcherService fileWatcherService,
		IInteractionsService interactionsService,
		IGlobalCommandsService globalCommands,
		IDialogService dialogService,
		ModImportService modImportService,
		MainCommandBarViewModel commandBar
		)
	{
		ModManager = modManagerService;
		ModImporter = modImportService;
		_interactions = interactionsService;
		_globalCommands = globalCommands;
		_dialogs = dialogService;

		CommandBar = commandBar;
		CommandBar.ModOrder = this;

		HostScreen = host;
		SelectedAdventureModIndex = 0;

		ActiveMods = [];
		InactiveMods = [];
		ModOrderList = [];
		ExternalModOrders = [];

		modManagerService.AdventureMods.ToObservableChangeSet()
			.Filter(x => x.UUID == DivinityApp.MAIN_CAMPAIGN_UUID) // For now, only display GustaDev
			.ObserveOn(RxApp.MainThreadScheduler).Bind(out _adventureMods).Subscribe();

		modManagerService.ForceLoadedMods.ToObservableChangeSet().Transform(x => x.ToModInterface()).Bind(out _overrideMods).Subscribe();

		ObservableCollectionExtended<IModEntry> readonlyActiveMods = [];
		ActiveMods.ToObservableChangeSet()
			.AutoRefresh(x => x.IsHidden)
			.Filter(x => !x.IsHidden)
			.ObserveOn(RxApp.MainThreadScheduler).Bind(readonlyActiveMods).Subscribe();

		ObservableCollectionExtended<IModEntry> readonlyOverrideMods = [];
		OverrideMods.ToObservableChangeSet()
			.AutoRefresh(x => x.IsHidden)
			.Filter(x => !x.IsHidden)
			.ObserveOn(RxApp.MainThreadScheduler).Bind(readonlyOverrideMods).Subscribe();

		ObservableCollectionExtended<IModEntry> readonlyInactiveMods = [];
		InactiveMods.ToObservableChangeSet()
			.AutoRefresh(x => x.IsHidden)
			.Filter(x => !x.IsHidden)
			.ObserveOn(RxApp.MainThreadScheduler).Bind(readonlyInactiveMods).Subscribe();

		//Pass the connection to the original collections, so the view can observe the total count
		var activeModsConnection = ActiveMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);
		var overrideModsConnection = OverrideMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);
		var inactiveModsConnection = InactiveMods.ToObservableChangeSet().ObserveOn(RxApp.MainThreadScheduler);

		ActiveModsView = new(new HierarchicalTreeDataGridSource<IModEntry>(readonlyActiveMods)
		{
			Columns =
			{
				//Avalonia.Controls.Models.TreeDataGrid.
				new TextColumn<IModEntry, int>("Index", x => x.Index, GridLength.Auto),
				new HierarchicalExpanderColumn<IModEntry>(
					//new TextColumn<IModEntry, string>("Name", x => x.DisplayName, GridLength.Star),
					new ModEntryColumn("Name", GridLength.Star),
					x => x.Children, x => x.Children != null && x.Children.Count > 0, x => x.IsExpanded),
				new TextColumn<IModEntry, string>("Version", x => x.Version, GridLength.Auto),
				new TextColumn<IModEntry, string>("Author", x => x.Author, GridLength.Auto),
				new TextColumn<IModEntry, string>("Last Updated", x => x.LastUpdated, GridLength.Auto),
			},
		}, ActiveMods, readonlyActiveMods, activeModsConnection, "Active");

		OverrideModsView = new(new HierarchicalTreeDataGridSource<IModEntry>(readonlyOverrideMods)
		{
			Columns =
			{
				new HierarchicalExpanderColumn<IModEntry>(
					new ModEntryColumn("Name", GridLength.Star),
					x => x.Children),
				new TextColumn<IModEntry, string>("Version", x => x.Version, GridLength.Auto),
				new TextColumn<IModEntry, string>("Author", x => x.Author, GridLength.Auto),
				new TextColumn<IModEntry, string>("Last Updated", x => x.LastUpdated, GridLength.Auto),
			}
		}, OverrideMods, readonlyOverrideMods, overrideModsConnection, "Overrides");

		InactiveModsView = new(new HierarchicalTreeDataGridSource<IModEntry>(readonlyInactiveMods)
		{
			Columns =
			{
				new HierarchicalExpanderColumn<IModEntry>(
					new ModEntryColumn("Name", GridLength.Star),
					x => x.Children),
				new TextColumn<IModEntry, string>("Version", x => x.Version, new GridLength(80d)),
				new TextColumn<IModEntry, string>("Author", x => x.Author, new GridLength(100d)),
				new TextColumn<IModEntry, string>("Last Updated", x => x.LastUpdated, new GridLength(200d)),
			}
		}, InactiveMods, readonlyInactiveMods, inactiveModsConnection, "Inactive");

		CanSaveOrder = true;

		var isRefreshing = host.WhenAnyValue(x => x.IsRefreshing);

		host.WhenAnyValue(x => x.IsLocked).BindTo(this, x => x.IsLocked);

		var isActive = HostScreen.Router.CurrentViewModel.Select(x => x == this);
		var mainIsNotLocked = host.WhenAnyValue(x => x.IsLocked, b => !b);
		var canExecuteCommands = mainIsNotLocked.CombineLatest(isActive).Select(x => x.First && x.Second);

		profiles.Connect().SortAndBind(out _uiprofiles, _profileSort).DisposeMany().Subscribe();

		var whenProfile = this.WhenAnyValue(x => x.SelectedProfile);
		var hasNonNullProfile = whenProfile.Select(x => x != null);
		hasNonNullProfile.ToUIProperty(this, x => x.HasProfile);

		ActiveMods.ToObservableChangeSet().CountChanged().Select(x => ActiveMods.Count).ToUIPropertyImmediate(this, x => x.TotalActiveMods);
		InactiveMods.ToObservableChangeSet().CountChanged().Select(x => InactiveMods.Count).ToUIPropertyImmediate(this, x => x.TotalInactiveMods);
		OverrideMods.ToObservableChangeSet().CountChanged().Select(_ => OverrideMods.Count > 0).ToUIPropertyImmediate(this, x => x.OverrideModsVisibility);

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
		ToggleOrderRenamingCommand = ReactiveCommand.CreateFromTask<object>(ToggleRenamingLoadOrder, canRenameOrder, RxApp.MainThreadScheduler);

		var canDeleteOrder = AllTrue(canExecuteCommands, this.WhenAnyValue(x => x.SelectedModOrderIndex).Select(x => x > 0));
		DeleteOrderCommand = ReactiveCommand.CreateFromTask<DivinityLoadOrder>(DeleteOrder, canDeleteOrder);

		CopyOrderToClipboardCommand = ReactiveCommand.CreateFromObservable(() => Observable.Start(() =>
		{
			try
			{
				if (ActiveMods.Count > 0)
				{
					var text = "";
					for (var i = 0; i < ActiveMods.Count; i++)
					{
						var mod = ActiveMods[i];
						text += $"{mod.Index}. {mod.DisplayName}";
						if (i < ActiveMods.Count - 1) text += Environment.NewLine;
					}
					ClipboardService.SetText(text);
					AppServices.Commands.ShowAlert("Copied mod order to clipboard", AlertType.Info, 10);
				}
				else
				{
					AppServices.Commands.ShowAlert("Current order is empty", AlertType.Warning, 10);
				}
			}
			catch (Exception ex)
			{
				AppServices.Commands.ShowAlert($"Error copying order to clipboard: {ex}", AlertType.Danger, 15);
			}
		}, RxApp.MainThreadScheduler));

		ExportOrderAsListCommand = ReactiveCommand.CreateFromTask(ExportLoadOrderToTextFileAs, this.WhenAnyValue(x => x.TotalActiveMods, x => x > 0));

		whenProfile.Subscribe(profile =>
		{
			if (profile != null && profile.ActiveMods != null && profile.ActiveMods.Count > 0)
			{
				var adventureModData = modManagerService.AdventureMods.FirstOrDefault(x => profile.ActiveMods.Any(y => y.UUID == x.UUID));
				//Migrate old profiles from Gustav to GustavDev
				if (adventureModData != null && adventureModData.UUID == "991c9c7a-fb80-40cb-8f0d-b92d4e80e9b1")
				{
					if (modManagerService.TryGetMod(DivinityApp.MAIN_CAMPAIGN_UUID, out var main))
					{
						adventureModData = main;
					}
				}
				if (adventureModData != null)
				{
					var nextAdventure = modManagerService.AdventureMods.IndexOf(adventureModData);
					DivinityApp.Log($"Found adventure mod in profile: {adventureModData.Name} | {nextAdventure}");
					if (nextAdventure > -1)
					{
						SelectedAdventureModIndex = nextAdventure;
					}
				}
			}
		});

		OrderJustLoadedCommand = ReactiveCommand.Create<DivinityLoadOrder>(order => { });

		/*Profiles.ToObservableChangeSet().CountChanged()
			.CombineLatest(this.WhenAnyValue(x => x.SelectedProfileIndex))
			//.ThrottleFirst(TimeSpan.FromMilliseconds(10))
			.Select(x => x.First.ElementAtOrDefault(x.Second)?.Item.Current)
			.WhereNotNull()
			.ObserveOn(RxApp.MainThreadScheduler)
			.ToUIPropertyImmediate(this, x => x.SelectedProfile);

		ModOrderList.ToObservableChangeSet().CountChanged()
			.CombineLatest(this.WhenAnyValue(x => x.SelectedModOrderIndex))
			//.ThrottleFirst(TimeSpan.FromMilliseconds(10))
			.Select(x => x.First.ElementAtOrDefault(x.Second)?.Item.Current)
			.WhereNotNull()
			.ObserveOn(RxApp.MainThreadScheduler)
			.ToUIPropertyImmediate(this, x => x.SelectedModOrder);*/

		/*modManagerService.AdventureMods.ToObservableChangeSet()
			.CountChanged()
			.CombineLatest(this.WhenAnyValue(x => x.SelectedAdventureModIndex))
			//.ThrottleFirst(TimeSpan.FromMilliseconds(50))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Select(x => x.First.ElementAtOrDefault(x.Second)?.Item.Current)
			.WhereNotNull()
			.ToUIPropertyImmediate(this, x => x.SelectedAdventureMod);*/

		var whenModOrder = this.WhenAnyValue(x => x.SelectedModOrder);

		whenModOrder.Select(x => x != null ? x.Name : "None").ToUIProperty(this, x => x.SelectedModOrderName);
		whenModOrder.Select(x => x != null && x.IsModSettings).ToUIProperty(this, x => x.IsBaseLoadOrder);

		whenModOrder.Buffer(2, 1).Subscribe(changes =>
		{
			if (changes[0] is { } previous && previous != null)
			{
				previous.IsLoaded = false;
			}
		});

		var whenNotRefreshing = this.WhenAnyValue(x => x.IsRefreshing, b => !b);

		IDisposable? _buildListTask = null;

		this.WhenAnyValue(x => x.SelectedProfile, x => x.SelectedModOrder)
			.ThrottleFirst(TimeSpan.FromMilliseconds(50))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(x =>
		{
			var profile = x.Item1;
			var order = x.Item2;

			if(profile != null)
			{
				if(profile != _lastProfile)
				{
					BuildModOrderList(profile, Math.Max(0, SelectedModOrderIndex));
					_lastProfile = profile;
				}
				else if (!IsLoadingOrder && order != null && !order.IsLoaded)
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
			}
			
		});

		//this.WhenAnyValue(x => x.OverrideModsFilterText).Throttle(TimeSpan.FromMilliseconds(500)).ObserveOn(RxApp.MainThreadScheduler).
		//	Subscribe((s) => { OnFilterTextChanged(s, modManagerService.ForceLoadedMods); });

		ActiveMods.WhenAnyPropertyChanged(nameof(DivinityModData.Index)).Throttle(TimeSpan.FromMilliseconds(25)).Subscribe(_ =>
		{
			SelectedModOrder?.Sort(SortModOrder);
		});

		this.WhenAnyValue(x => x.SelectedAdventureModIndex).Throttle(TimeSpan.FromMilliseconds(50)).Subscribe((i) =>
		{
			if (modManagerService.AdventureMods != null && SelectedAdventureMod != null && SelectedProfile != null && SelectedProfile.ActiveMods != null)
			{
				if (!SelectedProfile.ActiveMods.Any(m => m.UUID == SelectedAdventureMod.UUID))
				{
					SelectedProfile.ActiveMods.RemoveAll(r => modManagerService.AdventureMods.Any(y => y.UUID == r.UUID));
					SelectedProfile.ActiveMods.Insert(0, SelectedAdventureMod.ToProfileModData());
				}
			}
		});

		_modSettingsWatcher = fileWatcherService.WatchDirectory("", "*modsettings.lsx");
		//modSettingsWatcher.PauseWatcher(true);
		this.WhenAnyValue(x => x.SelectedProfile).WhereNotNull().Select(x => x.FilePath).Subscribe(path =>
		{
			_modSettingsWatcher.SetDirectory(path);
		});

		IDisposable? checkModSettingsTask = null;

		_modSettingsWatcher.FileChanged.Subscribe(e =>
		{
			if (SelectedModOrder != null)
			{
				//var exeName = !Settings.LaunchDX11 ? "bg3" : "bg3_dx11";
				//var isGameRunning = Process.GetProcessesByName(exeName).Length > 0;
				checkModSettingsTask?.Dispose();
				checkModSettingsTask = RxApp.TaskpoolScheduler.ScheduleAsync(TimeSpan.FromSeconds(2), async (sch, cts) =>
				{
					var activeCount = ActiveMods.Count;
					var modSettingsData = await DivinityModDataLoader.LoadModSettingsFileAsync(e.FullPath);
					if (activeCount > 0 && modSettingsData.CountActive() < activeCount)
					{
						AppServices.Commands.ShowAlert("The active load order (modsettings.lsx) has been reset externally", AlertType.Danger, 270);
						RxApp.MainThreadScheduler.Schedule(() =>
						{
							var title = "Mod Order Reset";
							var message = "The active load order (modsettings.lsx) has been reset externally, which has deactivated your mods.\nOne or more mods may be invalid in your current load order.";
							_interactions.ShowMessageBox.Handle(new(title, message, InteractionMessageBoxType.Error)).Subscribe();
						});
					}
				});
			}
		});

		//SetupKeys(host.Keys, host, canExecuteCommands);
	}
}