using Avalonia.Threading;

using ModManager.ViewModels.Main;

namespace ModManager.Windows;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
	public MainWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			this.OneWayBind(ViewModel, x => x.Router, x => x.ViewHost.Router);

			Dispatcher.UIThread.InvokeAsync(() => ViewModel.OnViewActivated(this), DispatcherPriority.Background);
		});
	}
}