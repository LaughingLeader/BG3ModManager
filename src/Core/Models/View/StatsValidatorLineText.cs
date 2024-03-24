namespace ModManager.Models.View;
public class StatsValidatorLineText : TreeViewEntry
{
	public override object ViewModel => this;

	[Reactive] public string Text { get; set; }
	[Reactive] public int Start { get; set; }
	[Reactive] public int End { get; set; }
	[Reactive] public bool IsError { get; set; }

	[ObservableAsProperty] public string HighlightedText { get; }

	private static string GetHighlightedText(ValueTuple<string, int, int> x)
	{
		var text = x.Item1;
		var start = x.Item2;
		var end = x.Item3;

		var length = Math.Min(text.Length, end - start);

		if (length > 0)
		{
			try
			{
				var result = text.Substring(start, length);
				DivinityApp.Log($"{result}");
				return result;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"{ex}");
			}
		}
		return String.Empty;
	}

	public StatsValidatorLineText()
	{
		IsExpanded = true;

		this.WhenAnyValue(x => x.Text, x => x.Start, x => x.End).Select(GetHighlightedText).ToUIProperty(this, x => x.HighlightedText);
	}
}
