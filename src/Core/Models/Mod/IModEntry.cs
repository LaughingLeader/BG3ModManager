using DynamicData.Binding;

namespace ModManager.Models.Mod;

public interface IModEntry : ISelectable
{
	ModEntryType EntryType { get; }

	string? UUID { get; }
	string? DisplayName { get; }
	string? Version { get; }
	string? Author { get; }
	string? LastUpdated { get; }

	string? SelectedColor { get; }
	string? PointerOverColor { get; }
	string? ListColor { get; }

	int Index { get; set; }
	bool IsActive { get; set; }
	bool IsExpanded { get; set; }
	bool CanDelete { get; }
	bool PreserveSelection { get; set; }

	IObservableCollection<IModEntry>? Children { get; }

	string? Export(ModExportType exportType);
}
