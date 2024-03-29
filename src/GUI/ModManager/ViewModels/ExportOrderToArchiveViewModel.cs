using DynamicData;
using DynamicData.Binding;

using ModManager.Models.Mod;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace ModManager.ViewModels;

public enum ExportOrderFileType
{
	[SettingsEntry("Default JSON", "The default .json load order that the mod manager uses")]
	DefaultJson,
	[SettingsEntry("Detailed JSON", "An order .json that contains more information, such as the author, description, tags, and more")]
	DetailedJson,
	[SettingsEntry("Tab-Separated Spreadsheet", "A .tsv spreadsheet contained detailed information about each mod")]
	TSV
}

public class ExportOrderFileEntry : ReactiveObject
{
	[Reactive] public bool IsSelected { get; set; }
	[Reactive] public bool IsVisible { get; set; }
	[Reactive] public DivinityModData? Mod { get; set; }

	public ExportOrderFileEntry()
	{
		IsVisible = true;
		IsSelected = true;
	}
}

public class ExportOrderToArchiveViewModel : BaseProgressViewModel
{
	[Reactive] public string? OutputPath { get; set; }
	[Reactive] public bool IncludeOverrides { get; set; }
	[Reactive] public ExportOrderFileType SelectedOrderType { get; set; }

	private readonly ObservableCollectionExtended<ExportOrderFileType> _orderTypes;

	public ObservableCollectionExtended<ExportOrderFileType> OrderTypes => _orderTypes;

	private readonly ObservableCollectionExtended<ExportOrderFileEntry> _entries;

	protected ReadOnlyObservableCollection<ExportOrderFileEntry> _visibleEntries;
	public ReadOnlyObservableCollection<ExportOrderFileEntry> Entries => _visibleEntries;

	[ObservableAsProperty] public bool AnySelected { get; }
	[ObservableAsProperty] public bool AllSelected { get; }
	[ObservableAsProperty] public string? SelectAllTooltip { get; }

	public RxCommandUnit SelectAllCommand { get; private set; }

	public override async Task<bool> Run(CancellationToken cts)
	{
		//Only visible + selected entries
		var exportedMods = Entries.Where(x => x.IsSelected);

		return true;
	}

	public override void Close()
	{
		base.Close();
		_entries.Clear();
	}

	public void ToggleSelectAll()
	{
		var b = !AllSelected;
		foreach (var f in Entries)
		{
			f.IsSelected = b;
		}
	}

	public ExportOrderToArchiveViewModel() : base()
	{
		IncludeOverrides = true;
		CanRun = true;

		_orderTypes = new ObservableCollectionExtended<ExportOrderFileType>(Enum.GetValues(typeof(ExportOrderFileType)).Cast<ExportOrderFileType>());

		_entries = [];

		var changeSet = _entries.ToObservableChangeSet();
		changeSet.Filter(x => x.IsVisible).ObserveOn(RxApp.MainThreadScheduler).Bind(out _visibleEntries).Subscribe();

		var filesChanged = changeSet.AutoRefresh(x => x.IsSelected).ToCollection().Throttle(TimeSpan.FromMilliseconds(50)).ObserveOn(RxApp.MainThreadScheduler);
		filesChanged.Select(x => x.Any(y => y.IsSelected)).ToUIProperty(this, x => x.AnySelected);
		filesChanged.Select(x => x.All(y => y.IsSelected)).ToUIProperty(this, x => x.AllSelected);
		this.WhenAnyValue(x => x.AllSelected).Select(b => $"{(b ? "Deselect" : "Select")} All").ToUIProperty(this, x => x.SelectAllTooltip);

		SelectAllCommand = ReactiveCommand.Create(ToggleSelectAll, RunCommand.IsExecuting.Select(b => !b), RxApp.MainThreadScheduler);

		this.WhenAnyValue(x => x.IncludeOverrides).Subscribe(b =>
		{
			foreach (var entry in _entries)
			{
				if (entry.Mod?.IsForceLoaded == true && !entry.Mod.IsForceLoadedMergedMod)
				{
					entry.IsVisible = b;
				}
			}
		});
	}
}
