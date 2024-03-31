using DynamicData.Binding;

using ModManager.Models;
using ModManager.Models.Mod;

using System.Collections.ObjectModel;

namespace ModManager.ViewModels;

public interface IModOrderViewModel
{
	ObservableCollectionExtended<IModEntry> ActiveMods { get; }
	ObservableCollectionExtended<IModEntry> InactiveMods { get; }
	ReadOnlyObservableCollection<DivinityProfileData> Profiles { get; }
	//ReadOnlyObservableCollection<DivinityModData> Mods { get; }
	//ReadOnlyObservableCollection<DivinityModData> WorkshopMods { get; }

	//bool IsDragging { get; }
	//bool IsRefreshing { get; }
	bool IsLocked { get; }

	//int ActiveSelected { get; }
	//int InactiveSelected { get; }

	//void ShowAlert(string message, AlertType alertType = AlertType.Info, int timeout = 0);
	void DeleteMod(IModEntry mod);
	void DeleteSelectedMods(IModEntry contextMenuMod);
	void ClearMissingMods();
	void AddActiveMod(IModEntry mod);
	void RemoveActiveMod(IModEntry mod);
}
