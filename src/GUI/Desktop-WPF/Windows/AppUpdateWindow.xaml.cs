using ModManager.Controls;
using ModManager.ViewModels;

using System.Windows.Documents;

namespace ModManager.Windows;

public class AppUpdateWindowBase : HideWindowBase<AppUpdateWindowViewModel> { }

public partial class AppUpdateWindow : AppUpdateWindowBase
{
	private readonly Lazy<Markdown> _fallbackMarkdown = new(() => new Markdown());
	private readonly Markdown _defaultMarkdown;

	private FlowDocument StringToMarkdown(string text)
	{
		var markdown = _defaultMarkdown ?? _fallbackMarkdown.Value;
		var doc = markdown.Transform(text);
		return doc;
	}

	public AppUpdateWindow()
	{
		InitializeComponent();

		ViewModel = ViewModelLocator.AppUpdate;

		var obj = TryFindResource("DefaultMarkdown");
		if (obj != null && obj is Markdown markdown)
		{
			_defaultMarkdown = markdown;
		}

		this.WhenActivated(d =>
		{
			d(this.BindCommand(ViewModel, vm => vm.ConfirmCommand, v => v.ConfirmButton));
			d(this.BindCommand(ViewModel, vm => vm.SkipCommand, v => v.SkipButton));
			d(this.OneWayBind(ViewModel, vm => vm.SkipButtonText, v => v.SkipButton.Content));
			d(this.OneWayBind(ViewModel, vm => vm.UpdateDescription, v => v.UpdateDescription.Text));
			d(this.OneWayBind(ViewModel, vm => vm.UpdateChangelogView, v => v.UpdateChangelogView.Document, StringToMarkdown));
		});
	}
}
