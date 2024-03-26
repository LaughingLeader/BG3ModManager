namespace ModManager.Models.Mod;

public interface IModEntry
{
	ModEntryType EntryType { get; }

	string UUID { get; }
	string DisplayName { get; }

	int Index { get; set; }
	bool IsActive { get; set; }
	bool IsVisible { get; set; }

	string Export(ModExportType exportType);
}
