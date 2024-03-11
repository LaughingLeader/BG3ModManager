using DivinityModManager.Models;
using DivinityModManager.Util;

using DynamicData;
using DynamicData.Binding;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Splat;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DivinityModManager.ViewModels.Main;
public class ModOrderViewModel : ReactiveObject, IRoutableViewModel
{
	public string UrlPathSegment => "modorder";
	public IScreen HostScreen { get; }

	public ObservableCollectionExtended<DivinityProfileData> Profiles { get; }
	public ObservableCollectionExtended<DivinityModData> ActiveMods { get; }
	public ObservableCollectionExtended<DivinityModData> InactiveMods { get; }

	public ObservableCollectionExtended<DivinityLoadOrder> ModOrderList { get; }

	public List<DivinityLoadOrder> SavedModOrderList { get; }

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
	[Reactive] public bool IsDragging { get; set; }
	[Reactive] public bool IsLoadingOrder { get; set; }

	[Reactive] public bool CanMoveSelectedMods { get; set; }
	[Reactive] public bool CanSaveOrder { get; set; }

	[Reactive] public int SelectedProfileIndex { get; set; }
	[Reactive] public int SelectedModOrderIndex { get; set; }
	[Reactive] public int SelectedAdventureModIndex { get; set; }

	[ObservableAsProperty] public string SelectedModOrderName { get; }
	[ObservableAsProperty] public DivinityProfileData SelectedProfile { get; }
	[ObservableAsProperty] public DivinityLoadOrder SelectedModOrder { get; }
	[ObservableAsProperty] public DivinityModData SelectedAdventureMod { get; }

	[ObservableAsProperty] public Visibility AdventureModBoxVisibility { get; }

	[ObservableAsProperty] public bool AllowDrop { get; }
	[ObservableAsProperty] public bool HasProfile { get; }
	[ObservableAsProperty] public bool IsBaseLoadOrder { get; }

	[ObservableAsProperty] public string ActiveSelectedText { get; }
	[ObservableAsProperty] public string InactiveSelectedText { get; }
	[ObservableAsProperty] public string OverrideModsSelectedText { get; }
	[ObservableAsProperty] public string ActiveModsFilterResultText { get; }
	[ObservableAsProperty] public string InactiveModsFilterResultText { get; }
	[ObservableAsProperty] public string OverrideModsFilterResultText { get; }

	public ICommand FocusFilterCommand { get; set; }

	public ReactiveCommand<DivinityLoadOrder, Unit> DeleteOrderCommand { get; }
	public ReactiveCommand<object, Unit> ToggleOrderRenamingCommand { get; set; }
	public ReactiveCommand<Unit, Unit> RefreshCommand { get; }

	public ICommand CopyOrderToClipboardCommand { get; }
	public ICommand ExportOrderAsListCommand { get; }

	private DivinityPathwayData PathwayData => Services.Pathways.Data;

	public ModOrderViewModel(IScreen host)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>();
		SelectedAdventureModIndex = 0;

		Profiles = [];
		ActiveMods = [];
		InactiveMods = [];
		ModOrderList = [];
		SavedModOrderList = [];

		var modManager = Services.Mods;

		this.WhenAnyValue(x => x.IsDragging, x => x.IsRefreshing, x => x.IsLoadingOrder, (b1, b2, b3) => b1 || b2 || b3).ToUIProperty(this, x => x.IsLocked);
		this.WhenAnyValue(x => x.IsLoadingOrder, x => x.IsRefreshing, x => x.IsInitialized, (b1, b2, b3) => !b1 && !b2 && b3).ToUIProperty(this, x => x.AllowDrop, true);

		modManager.WhenAnyValue(x => x.ActiveSelected).CombineLatest(this.WhenAnyValue(x => x.TotalActiveModsHidden)).Select(x => SelectedToLabel(x.First, x.Second)).ToUIProperty(this, x => x.ActiveSelectedText);
		modManager.WhenAnyValue(x => x.InactiveSelected).CombineLatest(this.WhenAnyValue(x => x.TotalInactiveModsHidden)).Select(x => SelectedToLabel(x.First, x.Second)).ToUIProperty(this, x => x.InactiveSelectedText);
		modManager.WhenAnyValue(x => x.OverrideModsSelected).CombineLatest(this.WhenAnyValue(x => x.TotalOverrideModsHidden)).Select(x => SelectedToLabel(x.First, x.Second)).ToUIProperty(this, x => x.OverrideModsSelectedText);
		//TODO Change .Count to CollectionChanged?
		this.WhenAnyValue(x => x.TotalActiveModsHidden).Select(x => HiddenToLabel(x, ActiveMods.Count)).ToUIProperty(this, x => x.ActiveModsFilterResultText);
		this.WhenAnyValue(x => x.TotalInactiveModsHidden).Select(x => HiddenToLabel(x, InactiveMods.Count)).ToUIProperty(this, x => x.InactiveModsFilterResultText);
		this.WhenAnyValue(x => x.TotalOverrideModsHidden).Select(x => HiddenToLabel(x, ForceLoadedMods.Count)).ToUIProperty(this, x => x.OverrideModsFilterResultText);

		this.WhenAnyValue(x => x.SelectedProfileIndex).Select(x => Profiles.ElementAtOrDefault(x)).BindTo(this, x => x.SelectedProfile);
		var whenProfile = this.WhenAnyValue(x => x.SelectedProfile);
		var hasNonNullProfile = whenProfile.Select(x => x != null);
		hasNonNullProfile.ToUIProperty(this, x => x.HasProfile);

		Keys.ExportOrderToGame.AddAction(ExportLoadOrder, hasNonNullProfile);

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

	private void ExportLoadOrder()
	{
		RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
		{
			await ExportLoadOrderAsync();
			return Disposable.Empty;
		});
	}

	private void DisplayMissingMods(DivinityLoadOrder order = null)
	{
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
				DivinityInteractions.ShowMessageBox.Handle(new(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK));
				//View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
				//View.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
				//View.MainWindowMessageBox_OK.ShowMessageBox(String.Join("\n", missingMods.OrderBy(x => x.Index)),
				//	"Missing Mods in Load Order", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
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

		if (settings.DisableMissingModWarnings != true && displayExtenderModWarning && AppSettings.Features.ScriptExtender)
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
				DivinityInteractions.ShowMessageBox.Handle(new(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK));
				//View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
				//View.MainWindowMessageBox_OK.Closed += MainWindowMessageBox_Closed_ResetColor;
				//View.MainWindowMessageBox_OK.ShowMessageBox("Functionality may be limited without the Script Extender.\n" + String.Join("\n", extenderRequiredMods.OrderBy(x => x.Index)),
				//	"Mods Require the Script Extender", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
			}
		}
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

					//View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
					//View.MainWindowMessageBox_OK.Closed += this.MainWindowMessageBox_Closed_ResetColor;
					//View.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);
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
						await Observable.Start(() =>
						{
							string message = $"Problem exporting load order to '{SelectedGameMasterCampaign.FilePath}'";
							DivinityApp.ShowAlert(message, AlertType.Danger);
							DivinityInteractions.ShowMessageBox.Handle(new(message, "Mod Order Export Failed", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK));
							//this.View.MainWindowMessageBox_OK.WindowBackground = new SolidColorBrush(Color.FromRgb(219, 40, 40));
							//this.View.MainWindowMessageBox_OK.Closed += this.MainWindowMessageBox_Closed_ResetColor;
							//this.View.MainWindowMessageBox_OK.ShowMessageBox(msg, "Mod Order Export Failed", MessageBoxButton.OK);

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

	public static void DeleteMod(DivinityModData mod)
	{
		if (mod.CanDelete)
		{
			DivinityInteractions.DeleteMods.Handle(new([mod], false));
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
			DivinityInteractions.DeleteMods.Handle(new(targetMods, false));
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
}