using Avalonia.Controls.Documents;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

using ModManager.Util;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Controls;

/// <summary>
/// A TextBlock with a clickable hyperlink
/// </summary>
[TemplatePart(ElementTextBlock, typeof(TextBlock))]
public class Hyperlink : ContentControl
{
	private const string ElementTextBlock = "PART_HyperlinkTextBlock";

	public static readonly StyledProperty<Uri?> UrlProperty = AvaloniaProperty.Register<HighlightingTextBlock, Uri?>(nameof(Url));
	public static readonly StyledProperty<string?> DisplayTextProperty = AvaloniaProperty.Register<HighlightingTextBlock, string?>(nameof(DisplayText));

	public Uri? Url
	{
		get => GetValue(UrlProperty);
		set => SetValue(UrlProperty, value);
	}

	public string? DisplayText
	{
		get => GetValue(DisplayTextProperty);
		set => SetValue(DisplayTextProperty, value);
	}

	private TextBlock? TextElement { get; set; }

	protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
	{
		base.OnApplyTemplate(e);

		var textBlock = e.NameScope.Find<TextBlock>(ElementTextBlock);
		if (textBlock != null)
		{
			TextElement = textBlock;
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		base.OnPointerPressed(e);

		if (Url != null && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
		{
			ProcessHelper.TryOpenUrl(Url.ToString());
		}
	}

	private static string TransformText(Uri? url, string? displayText)
	{
		if (displayText.IsValid()) return displayText;
		if (url != null) return url.ToString();
		return string.Empty;
	}

	private void ApplyText(string text)
	{
		if(TextElement != null)
		{
			TextElement.Text = text;
		}
	}

	public Hyperlink()
	{
		var urlChanged = this.GetObservable(UrlProperty);
		var displayTextChanged = this.GetObservable(DisplayTextProperty);
		Observable.CombineLatest(urlChanged, displayTextChanged, TransformText)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(ApplyText);
	}
}
