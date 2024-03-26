using ModManager.ViewModels;

using ReactiveUI;

namespace ModManager.Windows;

public class NxmDownloadWindowBase : HideWindowBase<NxmDownloadWindowViewModel> { }
/// <summary>
/// Interaction logic for NxmDownloadWindow.xaml
/// </summary>
public partial class NxmDownloadWindow : NxmDownloadWindowBase
{
	public NxmDownloadWindow()
	{
		InitializeComponent();

		ViewModel = new();

		this.WhenActivated(d =>
		{
			this.Bind(ViewModel, vm => vm.Url, view => view.UrlTextBox.Text);
			this.BindCommand(ViewModel, vm => vm.DownloadCommand, view => view.DownloadButton);
		});
	}
}
