using ModManager.Models.Mod;
using ModManager.Models.Settings;

namespace ModManager;

public interface ISettingsService
{
	AppSettings AppSettings { get; }
	ModManagerSettings ManagerSettings { get; }
	UserModConfig ModConfig { get; }

	bool TrySaveAll(out List<Exception> errors);
	bool TryLoadAll(out List<Exception> errors);
	bool TryLoadAppSettings(out Exception error);
	void UpdateLastUpdated(IList<string> updatedModIds);
	void UpdateLastUpdated(IList<ModData> updatedMods);
}