using Avalonia.Media;

using ModManager.ViewModels.Window;

namespace ModManager.Windows;

public partial class PakFileExplorerWindow : ReactiveWindow<PakFileExplorerWindowViewModel>
{
	public PakFileExplorerWindow()
	{
		InitializeComponent();

		if (Design.IsDesignMode)
		{
			Background = Brushes.Black;
		}
		else
		{
			ViewModel = AppServices.Get<PakFileExplorerWindowViewModel>();
		}

		this.WhenActivated(d =>
		{
			if (ViewModel != null)
			{
				var commands = AppServices.Commands;

				this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);

				this.BindCommand(ViewModel, x => x.ExtractPakFilesCommand, view => view.CopyToButton);
				this.BindCommand(ViewModel, x => x.CopyToClipboardCommand, view => view.CopyPathButton);
			}
		});
	}
}