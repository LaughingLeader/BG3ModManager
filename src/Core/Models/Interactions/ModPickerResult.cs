using ModManager.Models.Mod;

namespace ModManager;
public record struct ModPickerResult(List<ModData> Mods, bool Confirmed);