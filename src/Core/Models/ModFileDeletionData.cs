using ModManager.Models.Mod;

namespace ModManager.Models;

public class ModFileDeletionData : ReactiveObject
{
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public bool IsWorkshop { get; set; }
	[Reactive] public string? FilePath { get; set; }
	[Reactive] public string? DisplayName { get; set; }
	[Reactive] public string? UUID { get; set; }
	[Reactive] public string? Duplicates { get; set; }

	public static ModFileDeletionData? FromModEntry(IModEntry entry, bool isWorkshopMod = false, bool isDeletingDuplicates = false, IEnumerable<DivinityModData>? loadedMods = null)
	{
		if (entry.EntryType is ModEntryType.Mod && entry is ModEntry modEntry && modEntry.Data != null)
		{
			var mod = modEntry.Data;
			var data = new ModFileDeletionData { FilePath = mod.FilePath, DisplayName = mod.DisplayName, IsSelected = true, UUID = mod.UUID, IsWorkshop = isWorkshopMod };
			if (isDeletingDuplicates && loadedMods != null)
			{
				var duplicatesStr = loadedMods.FirstOrDefault(x => x.UUID == entry.UUID)?.FilePath;
				if (!String.IsNullOrEmpty(duplicatesStr))
				{
					data.Duplicates = duplicatesStr;
				}
			}
			return data;
		}
		return null;
	}
}
