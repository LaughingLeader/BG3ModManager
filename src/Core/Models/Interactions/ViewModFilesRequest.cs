using ModManager.Models.Mod;

namespace ModManager;
public record struct ViewModFilesRequest(IEnumerable<ModData>? Mods);
