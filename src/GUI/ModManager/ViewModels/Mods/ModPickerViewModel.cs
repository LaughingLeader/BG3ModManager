using DynamicData;

using ModManager.Models.View;

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

	private readonly Subject<ModPickerResult> Result = new();

	public IObservable<ModPickerResult> WaitForResult() => Result.Take(1);

	public RxCommandUnit ConfirmCommand { get; }
	public RxCommandUnit CancelCommand { get; }

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

	public ModPickerViewModel()
	{
		_source.Connect().ObserveOn(RxApp.MainThreadScheduler).SortAndBind(out _mods, Sorters.INamedIgnoreCase).DisposeMany().Subscribe();

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
	}
}
