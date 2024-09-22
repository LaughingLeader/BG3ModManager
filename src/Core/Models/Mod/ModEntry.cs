﻿using DynamicData.Binding;

using System.Globalization;

namespace ModManager.Models.Mod;
public class ModEntry : ReactiveObject, IModEntry
{
	public ModEntryType EntryType => ModEntryType.Mod;

	[Reactive] public int Index { get; set; }

	[Reactive] public bool IsActive { get; set; }
	[Reactive] public bool IsHidden { get; set; }
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public bool IsExpanded { get; set; }
	[Reactive] public bool IsDraggable { get; set; }
	[Reactive] public bool PreserveSelection { get; set; }

	[ObservableAsProperty] public string? UUID { get; }
	[ObservableAsProperty] public string? DisplayName { get; }
	[ObservableAsProperty] public string? Version { get; }
	[ObservableAsProperty] public string? Author { get; }
	[ObservableAsProperty] public string? LastUpdated { get; }
	[ObservableAsProperty] public bool CanDelete { get; }

	public IObservableCollection<IModEntry>? Children => new ObservableCollectionExtended<IModEntry>();

	public string? Export(ModExportType exportType) => string.Empty;

	[Reactive] public DivinityModData? Data { get; set; }

	public ModEntry()
	{
		var whenMod = this.WhenAnyValue(x => x.Data).WhereNotNull();

		whenMod.Select(x => x.Index).BindTo(this, x => x.Index);

		whenMod.Select(x => x.UUID).ToUIProperty(this, x => x.UUID);
		whenMod.Select(x => x.DisplayName).ToUIProperty(this, x => x.DisplayName);
		whenMod.Select(x => x.Version.Version).ToUIProperty(this, x => x.Version);
		whenMod.Select(x => x.Author).ToUIProperty(this, x => x.Author);
		whenMod.Select(x => x.LastModified?.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture)).ToUIProperty(this, x => x.LastUpdated);

		whenMod.Select(x => x.CanDelete).ToUIProperty(this, x => x.CanDelete);

		this.WhenAnyValue(x => x.IsHidden).Subscribe(b =>
		{
			if (!b) IsSelected = false;
		});
	}

	public ModEntry(DivinityModData modData) : this()
	{
		Data = modData;
	}
}