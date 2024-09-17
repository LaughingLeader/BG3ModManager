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
				d(this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible));


			}
		});
	}
}