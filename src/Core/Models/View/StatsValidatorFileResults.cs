using DynamicData;
using DynamicData.Binding;

namespace ModManager.Models.View;
public class StatsValidatorFileResults : TreeViewEntry
{
	public override object ViewModel => this;

	[Reactive] public string FilePath { get; set; }

	[ObservableAsProperty] public string Name { get; }
	[ObservableAsProperty] public int Total { get; }
	[ObservableAsProperty] public string DisplayName { get; }
	[ObservableAsProperty] public string ToolTip { get; }
	[ObservableAsProperty] public bool HasErrors { get; }

	private readonly ReadOnlyObservableCollection<StatsValidatorErrorEntry> _errors;
	public ReadOnlyObservableCollection<StatsValidatorErrorEntry> Errors => _errors;

	private string ToToolTip(ValueTuple<string, int> x)
	{
		var errors = Children.Cast<StatsValidatorErrorEntry>().Count(x => x.IsError);
		return $"{x.Item1}\nErrors: {errors}\nWarnings: {x.Item2 - errors}";
	}

	public StatsValidatorFileResults()
	{
		var childrenChanged = this.Children.ToObservableChangeSet().Transform(x => (StatsValidatorErrorEntry)x);
		childrenChanged.CountChanged().Select(_ => Children.Count).ToUIProperty(this, x => x.Total);
		childrenChanged.Bind(out _errors).Subscribe();

		this.WhenAnyValue(x => x.FilePath).Select(Path.GetFileName).ToUIProperty(this, x => x.Name);
		this.WhenAnyValue(x => x.Name, x => x.Total).Select(x => $"{x.Item1} ({x.Item2})").ToUIProperty(this, x => x.DisplayName);
		this.WhenAnyValue(x => x.FilePath, x => x.Total).Select(ToToolTip).ToUIProperty(this, x => x.ToolTip);
		Errors.ToObservableChangeSet().AutoRefresh(x => x.IsError).ToCollection().Select(_ => Errors.Any(x => x.IsError)).ToUIProperty(this, x => x.HasErrors);
	}
}
