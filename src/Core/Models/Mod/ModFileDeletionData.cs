namespace ModManager.Models.Mod;

public class ModFileDeletionData : ReactiveObject
{
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public string? FilePath { get; set; }
	[Reactive] public string? DisplayName { get; set; }
	[Reactive] public string? UUID { get; set; }
	[Reactive] public string? Duplicates { get; set; }

	public static ModFileDeletionData? FromModEntry(IModEntry entry, bool isDeletingDuplicates = false, IEnumerable<ModData>? loadedMods = null)
	{
		if (entry.EntryType is ModEntryType.Mod && entry is ModEntry modEntry && modEntry.Data != null)
		{
			var mod = modEntry.Data;
			var data = new ModFileDeletionData { FilePath = mod.FilePath, DisplayName = mod.DisplayName, IsSelected = true, UUID = mod.UUID };
			if (isDeletingDuplicates && loadedMods != null)
			{
				var duplicatesStr = loadedMods.FirstOrDefault(x => x.UUID == entry.UUID)?.FilePath;
				if (duplicatesStr.IsValid())
				{
					data.Duplicates = duplicatesStr;
				}
			}
			return data;
		}
		return null;
	}
}
