namespace ModManager.Models.Conflicts;

public class DivinityConflictGroup : ReactiveObject
{
	private string header;

	public string? Header
	{
		get => header;
		set { this.RaiseAndSetIfChanged(ref header, value); }
	}

	private int totalConflicts = 0;

	public int TotalConflicts
	{
		get => totalConflicts;
		set { this.RaiseAndSetIfChanged(ref totalConflicts, value); }
	}

	public List<DivinityConflictEntryData> Conflicts { get; set; } = [];

	private int selectedConflictIndex = 0;

	public int SelectedConflictIndex
	{
		get => selectedConflictIndex;
		set { this.RaiseAndSetIfChanged(ref selectedConflictIndex, value); }
	}

	public void OnActivated(CompositeDisposable disposables)
	{
		this.WhenAnyValue(x => x.Conflicts.Count).Subscribe(c => TotalConflicts = c).DisposeWith(disposables);
	}
}
