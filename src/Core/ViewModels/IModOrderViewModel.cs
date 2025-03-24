using DynamicData.Binding;

using ModManager.Models;
using ModManager.Models.Mod;

namespace ModManager.ViewModels;

public interface IModOrderViewModel
{
	ObservableCollectionExtended<IModEntry> ActiveMods { get; }
	ObservableCollectionExtended<IModEntry> InactiveMods { get; }
	ReadOnlyObservableCollection<DivinityModData> AdventureMods { get; }
	ReadOnlyObservableCollection<DivinityProfileData> Profiles { get; }
	//ReadOnlyObservableCollection<DivinityModData> Mods { get; }
	//ReadOnlyObservableCollection<DivinityModData> WorkshopMods { get; }
	ObservableCollectionExtended<DivinityLoadOrder> ModOrderList { get; }

	//bool IsDragging { get; }
	//bool IsRefreshing { get; }
	bool IsLocked { get; }

	int SelectedProfileIndex { get; set; }
	int SelectedModOrderIndex { get; set; }
	int SelectedAdventureModIndex { get; set; }

	DivinityProfileData? SelectedProfile { get; set; }
	DivinityLoadOrder? SelectedModOrder { get; set; }
	DivinityModData? SelectedAdventureMod { get; set; }

	//int ActiveSelected { get; }
	//int InactiveSelected { get; }

	//void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0);
	void DeleteMod(IModEntry mod);
	void DeleteSelectedMods(IModEntry contextMenuMod);
	void ClearMissingMods();
	void AddActiveMod(IModEntry mod);
	void RemoveActiveMod(IModEntry mod);
}
