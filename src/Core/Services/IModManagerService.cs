using DynamicData;

using ModManager.Models.Mod;

using System.Diagnostics.CodeAnalysis;
using System.Reactive.Subjects;

namespace ModManager;

public interface IModManagerService
{
	IEnumerable<ModData> AllMods { get; }
	ReadOnlyObservableCollection<ModData> AddonMods { get; }
	ReadOnlyObservableCollection<ModData> AdventureMods { get; }
	ReadOnlyObservableCollection<ModData> ForceLoadedMods { get; }
	ReadOnlyObservableCollection<ModData> UserMods { get; }
	ReadOnlyObservableCollection<ModData> SelectedPakMods { get; }
	string MainCampaignGuid { get; set; }

	int ActiveSelected { get; }
	int InactiveSelected { get; }
	int OverrideModsSelected { get; }
	IConnectableObservable<IChangeSet<ModData, string>> ModsConnection { get; }
	bool ModExists(string uuid);
	void Add(ModData mod);
	void RemoveByUUID(string uuid);
	void RemoveByUUID(IEnumerable<string> uuids);
	bool TryGetMod(string? guid, [NotNullWhen(true)] out ModData? mod);
	string GetModType(string guid);
	bool ModIsAvailable(IModData divinityModData);
	void DeselectAllMods();
	void Refresh();
	void ApplyUserModConfig();
	void SetLoadedMods(IEnumerable<ModData> loadedMods, bool nexusModsEnabled = false);
	Task<List<ModData>> LoadModsAsync(string gameDataPath, string userModsDirectoryPath, CancellationToken token);
	IEnumerable<IModEntry> GetAllModsAsInterface();
}