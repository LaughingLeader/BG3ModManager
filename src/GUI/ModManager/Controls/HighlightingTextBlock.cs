using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace ModManager.Controls;

/// <summary>
/// A TextBlock that highlights text
/// </summary>
[TemplatePart(Name = HighlightTextBlockName, Type = typeof(TextBlock))]
public class HighlightingTextBlock : TemplatedControl
{
	private const string HighlightTextBlockName = "PART_HighlightTextblock";

	private static readonly StyledProperty<int> HighlightStartProperty = AvaloniaProperty.Register<HighlightingTextBlock, int>(nameof(HighlightStart));
	private static readonly StyledProperty<int> HighlightEndProperty = AvaloniaProperty.Register<HighlightingTextBlock, int>(nameof(HighlightEnd));

	public static readonly StyledProperty<string?> TextProperty = TextBlock.TextProperty.AddOwner<HighlightingTextBlock>();

	public static readonly StyledProperty<TextWrapping> TextWrappingProperty = TextBlock.TextWrappingProperty.AddOwner<HighlightingTextBlock>();

	public static readonly StyledProperty<TextTrimming> TextTrimmingProperty = TextBlock.TextTrimmingProperty.AddOwner<HighlightingTextBlock>();

	public static readonly StyledProperty<IBrush> HighlightForegroundProperty = AvaloniaProperty.Register<HighlightingTextBlock, IBrush>(nameof(HighlightForeground), Brushes.White);

	public static readonly StyledProperty<IBrush> HighlightBackgroundProperty = AvaloniaProperty.Register<HighlightingTextBlock, IBrush>(nameof(HighlightBackground), Brushes.Blue);

	private TextBlock? highlightTextBlock;

	public IBrush HighlightBackground
	{
		get => GetValue(HighlightBackgroundProperty);
		set => SetValue(HighlightBackgroundProperty, value);
	}

	public IBrush HighlightForeground
	{
		get => GetValue(HighlightForegroundProperty);
		set => SetValue(HighlightForegroundProperty, value);
	}

	public int HighlightStart
	{
		get => GetValue(HighlightStartProperty);
		set => SetValue(HighlightStartProperty, value);
	}

	public int HighlightEnd
	{
		get => GetValue(HighlightEndProperty);
		set => SetValue(HighlightEndProperty, value);
	}

	public string? Text
	{
		get => GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	public TextWrapping TextWrapping
	{
		get => GetValue(TextWrappingProperty);
		set => SetValue(TextWrappingProperty, value);
	}

	public TextTrimming TextTrimming
	{
		get => GetValue(TextTrimmingProperty);
		set => SetValue(TextTrimmingProperty, value);
	}

	private void ProcessTextChanged(string? mainText, int highlightStart, int highlightEnd)
	{
		if (highlightTextBlock == null || mainText == null || string.IsNullOrWhiteSpace(mainText))
			return;
		if (highlightTextBlock.Inlines == null) highlightTextBlock.Inlines = [];
		highlightTextBlock.Inlines.Clear();

		var start = Math.Max(0, highlightStart);
		var end = Math.Min(mainText.Length, highlightEnd);
		var highlightLength = Math.Min(mainText.Length, end - start);

		if (start == end || highlightLength <= 0)
		{
			var completeRun = new Run(mainText);
			highlightTextBlock.Inlines.Add(completeRun);
			return;
		}

		var highlightedText = mainText.Substring(start, highlightLength);

		if (start > 0) highlightTextBlock.Inlines.Add(GetRunForText(mainText[..start], false));
		highlightTextBlock.Inlines.Add(GetRunForText(highlightedText, true));
		if (end < mainText.Length)
		{
			highlightTextBlock.Inlines.Add(GetRunForText(mainText[end..], false));
		}
	}

	private Run GetRunForText(string text, bool isHighlighted)
	{
		var textRun = new Run(text);
		if (isHighlighted)
		{
			textRun.Foreground = HighlightForeground;
			textRun.Background = HighlightBackground;

			var disposables = new CompositeDisposable(
			this.ObservableForProperty(x => x.HighlightForeground).BindTo(textRun, x => x.Foreground),
			this.ObservableForProperty(x => x.HighlightBackground).BindTo(textRun, x => x.Background));

			textRun.DetachedFromLogicalTree += (o, e) =>
			{
				disposables?.Dispose();
			};
		}
		return textRun;
	}

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		highlightTextBlock = e.NameScope.Find<TextBlock>(HighlightTextBlockName);
		if (highlightTextBlock != null)
		{
			ProcessTextChanged(Text, HighlightStart, HighlightEnd);
		}
	}

	private void UpdateRunColors(IBrush foreground, IBrush background)
	{
		if (highlightTextBlock?.Inlines != null)
		{
			foreach (var run in highlightTextBlock.Inlines)
			{
				if (run != null)
				{
					run.Foreground = foreground;
					run.Background = background;
				}
			}
		}
	}

	public HighlightingTextBlock()
	{
		this.ObservableForProperty(x => x.HighlightForeground)
		.CombineLatest(this.ObservableForProperty(x => x.HighlightBackground)).Subscribe(x => UpdateRunColors(x.First.Value, x.Second.Value));


		this.ObservableForProperty(x => x.Text)
		.CombineLatest(this.ObservableForProperty(x => x.HighlightStart), this.ObservableForProperty(x => x.HighlightEnd))
		.Subscribe(x => ProcessTextChanged(x.First.Value, x.Second.Value, x.Third.Value));
	}
}
