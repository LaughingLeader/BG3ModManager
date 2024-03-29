using DynamicData;

using ModManager.Models.Mod;
using ModManager.Models.View;

using ReactiveUI.Fody.Helpers;

using System.Collections.ObjectModel;
using System.Reactive.Subjects;

namespace ModManager;

public interface IModManagerService
{
	IEnumerable<DivinityModData> AllMods { get; }
	ReadOnlyObservableCollection<DivinityModData> AddonMods { get; }
	ReadOnlyObservableCollection<DivinityModData> AdventureMods { get; }
	ReadOnlyObservableCollection<DivinityModData> ForceLoadedMods { get; }
	ReadOnlyObservableCollection<DivinityModData> UserMods { get; }
	ReadOnlyObservableCollection<DivinityModData> SelectedPakMods { get; }
	[ObservableAsProperty] int ActiveSelected { get; }
	[ObservableAsProperty] int InactiveSelected { get; }
	[ObservableAsProperty] int OverrideModsSelected { get; }
	IConnectableObservable<IChangeSet<DivinityModData, string>> ModsConnection { get; }
	bool ModExists(string uuid);
	void Add(DivinityModData mod);
	void RemoveByUUID(string uuid);
	void RemoveByUUID(IEnumerable<string> uuids);
	bool TryGetMod(string guid, out DivinityModData mod);
	string GetModType(string guid);
	bool ModIsAvailable(IDivinityModData divinityModData);
	void DeselectAllMods();
	void Refresh();
	void ApplyUserModConfig();
	void SetLoadedMods(IEnumerable<DivinityModData> loadedMods, bool nexusModsEnabled = false);
	Task<List<DivinityModData>> LoadModsAsync(string userModsDirectoryPath, ProgressUpdateActions progress, double taskStepAmount);
}