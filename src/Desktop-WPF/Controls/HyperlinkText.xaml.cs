using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace ModManager.Controls;

/// <summary>
/// Interaction logic for HyperlinkText.xaml
/// </summary>
public partial class HyperlinkText : TextBlock
{
	private static readonly PropertyChangedCallback _updateText = new(UpdateHyperlinkText);
	public static readonly DependencyProperty URLProperty = DependencyProperty.Register("URL", typeof(string), typeof(HyperlinkText), new PropertyMetadata("", new PropertyChangedCallback(OnURLChanged)));
	public static readonly DependencyProperty DisplayTextProperty = DependencyProperty.Register("DisplayText", typeof(string), typeof(HyperlinkText), new PropertyMetadata("", _updateText));
	public static readonly DependencyProperty UseUrlForDisplayTextProperty = DependencyProperty.Register("UseUrlForDisplayText", typeof(bool), typeof(HyperlinkText), new PropertyMetadata(false, _updateText));

	public string URL
	{
		get => (string)GetValue(URLProperty);
		set => SetValue(URLProperty, value);
	}

	public string DisplayText
	{
		get => (string)GetValue(DisplayTextProperty);
		set => SetValue(DisplayTextProperty, value);
	}

	public bool UseUrlForDisplayText
	{
		get => (bool)GetValue(UseUrlForDisplayTextProperty);
		set => SetValue(UseUrlForDisplayTextProperty, value);
	}

	private static void OnURLChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is HyperlinkText hyperLinkText && e.NewValue is string url && Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
		{
			hyperLinkText.Hyperlink.NavigateUri = uri;
			hyperLinkText.UpdateDisplayText();
		}
	}

	private static void UpdateHyperlinkText(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is HyperlinkText hyperLinkText)
		{
			hyperLinkText.UpdateDisplayText();
		}
	}

	private void UpdateDisplayText()
	{
		var outputText = DisplayText;
		if (String.IsNullOrEmpty(outputText) || UseUrlForDisplayText)
		{
			outputText = URL;
		}
		DisplayTextTextBlock.Text = outputText;
		ToolTip = URL;
	}

	public HyperlinkText()
	{
		InitializeComponent();

		Hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
	}

	private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
	{
		Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
		e.Handled = true;
	}
}
