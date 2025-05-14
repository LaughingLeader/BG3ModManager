using ModManager.Models.Mod;

namespace ModManager.Services;
public interface IModSettingsExportService
{
	void SetGameVersion(Version gameVersion);
	void SetGameVersion(string exePath);
	Task<bool> ExportModSettingsToFileAsync(string folder, IEnumerable<ModData> order, CancellationToken token);
	string ToFormattedModuleShortDesc(ModData mod);
}
