using DynamicData;

using ModManager.Models.Mod;
using ModManager.Models.View;
using ModManager.Util;

using SukiUI.Dialogs;

using System.Collections.ObjectModel;
using System.Reactive.Subjects;

namespace ModManager.ViewModels.Mods;

public class ModPickerViewModel : ReactiveObject, IDialogViewModel
{
	private readonly SourceCache<ModPickerEntry, string> _source = new(x => x.UUID);

	private readonly ReadOnlyObservableCollection<ModPickerEntry> _mods;
	public ReadOnlyObservableCollection<ModPickerEntry> Mods => _mods;

	[Reactive] public string? Title { get; set; }
	[Reactive] public bool IsVisible { get; set; }
	[Reactive] public ISukiDialog? Dialog { get; set; }
	[Reactive] public string? FilterInputText { get; set; }
	[Reactive] public int TotalMods { get; private set; }
	[Reactive] public int TotalModsHidden { get; private set; }
	[Reactive] public int TotalModsSelected { get; private set; }


	private readonly Subject<ModPickerResult> Result = new();
	public IObservable<ModPickerResult> WaitForResult() => Result.Take(1);

	[ObservableAsProperty] public string? FilterResultText { get; }

	public RxCommandUnit ConfirmCommand { get; }
	public RxCommandUnit CancelCommand { get; }

	public void AddMod(ModData mod)
	{
		_source.AddOrUpdate(new ModPickerEntry(mod));
	}

	public void AddMod(IModEntry entry)
	{
		if(entry.EntryType == ModEntryType.Mod && entry is ModEntry modEntry && modEntry.Data != null)
		{
			_source.AddOrUpdate(new ModPickerEntry(modEntry.Data));
		}
	}

	public void AddMod(IEnumerable<IModEntry> mods)
	{
		foreach (var mod in mods)
		{
			AddMod(mod);
		}
	}

	public void AddMod(IEnumerable<ModData> mods)
	{
		foreach(var mod in mods)
		{
			AddMod(mod);
		}
	}

	public void Open(string title)
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			_source.Clear();
			foreach (var mod in AppServices.Mods.AllMods)
			{
				if (mod.IsUserMod || mod.IsToolkitProject)
				{
					_source.AddOrUpdate(new ModPickerEntry(mod));
				}
			}
			Title = title;
		});
	}

	public void Open(ShowModPickerRequest request) => Open(request.Title);

	private void Close(bool result)
	{
		var selectedMods = !result ? [] : Mods.Where(x => x.IsSelected).Select(x => x.Mod).ToList();
		Result.OnNext(new(selectedMods, result));
		Dialog?.Dismiss();
	}

	private static string ToFilterResultText(int totalMods, int totalHidden, int totalSelected, string? filterText)
	{
		if (totalMods <= 0 || !filterText.IsValid()) return string.Empty;

		List<string> texts = [];
		if (!string.IsNullOrWhiteSpace(filterText))
		{
			var matched = Math.Max(0, totalMods - totalHidden);
			texts.Add($"{matched} Matched");
			if (totalHidden > 0) texts.Add($"{totalHidden} Hidden");
		}
		if (totalSelected > 0) texts.Add($"{totalSelected} Selected");

		return string.Join(", ", texts);
	}

	public ModPickerViewModel()
	{
		_source.Connect()
			.AutoRefresh(x => x.IsHidden)
			.Filter(x => !x.IsHidden)
			.ObserveOn(RxApp.MainThreadScheduler)
			.SortAndBind(out _mods, Sorters.INamedIgnoreCase)
			.DisposeMany()
			.Subscribe();

		var canRunCommands = this.WhenAnyValue(x => x.IsVisible);

		ConfirmCommand = ReactiveCommand.Create(() => Close(true), canRunCommands);
		CancelCommand = ReactiveCommand.Create(() => Close(false), canRunCommands);

		this.WhenAnyValue(x => x.IsVisible).ObserveOn(RxApp.TaskpoolScheduler).Subscribe(b =>
		{
			if (!b)
			{
				Result.OnNext(new([], false));
			}
		});

		this.WhenAnyValue(x => x.TotalMods, x => x.TotalModsHidden, x => x.TotalModsSelected,
			x => x.FilterInputText, ToFilterResultText)
			.ToUIProperty(this, x => x.FilterResultText);

		this.WhenAnyValue(x => x.FilterInputText)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(filterText =>
			{
				var mods = _source.Items.Select(x => x.Mod);
				ModUtils.FilterMods(filterText, mods);
			});
	}
}

public class DesignModPickerViewModel : ModPickerViewModel
{
	public DesignModPickerViewModel() : base()
	{
		Title = "Pick Mods";
		AddMod(ModelGlobals.TestMods);
	}
}