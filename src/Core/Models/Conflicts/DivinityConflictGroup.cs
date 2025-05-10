namespace ModManager.Models.Conflicts;

public class DivinityConflictGroup : ReactiveObject
{
	[Reactive] public string? Header { get; set; }

	[Reactive] public int TotalConflicts { get; set; }

	public List<DivinityConflictEntryData> Conflicts { get; set; } = [];

	[Reactive] public int SelectedConflictIndex { get; set; }

	public void OnActivated(CompositeDisposable disposables)
	{
		this.WhenAnyValue(x => x.Conflicts.Count).Subscribe(c => TotalConflicts = c).DisposeWith(disposables);
	}
}
