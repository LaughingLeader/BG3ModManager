using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
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
			Dispatcher.UIThread.InvokeAsync(() => ViewModel.OnViewActivated(this));
			this.OneWayBind(ViewModel, x => x.Router, x => x.ViewHost.Router);
		});
    }
}