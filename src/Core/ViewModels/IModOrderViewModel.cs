using DivinityModManager.Models;

using DynamicData.Binding;

using System.Collections.ObjectModel;

namespace DivinityModManager.ViewModels;

public interface IModOrderViewModel
{
	ObservableCollectionExtended<DivinityModData> ActiveMods { get; }
	ObservableCollectionExtended<DivinityModData> InactiveMods { get; }
	ReadOnlyObservableCollection<DivinityProfileData> Profiles { get; }
	//ReadOnlyObservableCollection<DivinityModData> Mods { get; }
	//ReadOnlyObservableCollection<DivinityModData> WorkshopMods { get; }

	//bool IsDragging { get; }
	//bool IsRefreshing { get; }
	bool IsLocked { get; }

	//int ActiveSelected { get; }
	//int InactiveSelected { get; }

	//void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0);
	void DeleteMod(DivinityModData mod);
	void DeleteSelectedMods(DivinityModData contextMenuMod);
	void ClearMissingMods();
	void AddActiveMod(DivinityModData mod);
	void RemoveActiveMod(DivinityModData mod);
}
