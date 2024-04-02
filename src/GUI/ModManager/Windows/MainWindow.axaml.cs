using Avalonia.Threading;

using ModManager.ViewModels.Main;

using SukiUI.Controls;

namespace ModManager.Windows;

public partial class MainWindow : SukiWindow, IViewFor<MainWindowViewModel>
{
	public static readonly StyledProperty<MainWindowViewModel?> ViewModelProperty = AvaloniaProperty
			.Register<MainWindow, MainWindowViewModel?>(nameof(ViewModel));

	public MainWindow()
	{
		InitializeComponent();

		// This WhenActivated block calls ViewModel's WhenActivated
		// block if the ViewModel implements IActivatableViewModel.
		this.WhenActivated(disposables => { });
		this.GetObservable(DataContextProperty).Subscribe(OnDataContextChanged);
		this.GetObservable(ViewModelProperty).Subscribe(OnViewModelChanged);

		this.WhenActivated(d =>
		{
			this.OneWayBind(ViewModel, x => x.Router, x => x.ViewHost.Router);

			Dispatcher.UIThread.InvokeAsync(() => ViewModel.OnViewActivated(this), DispatcherPriority.Background);
		});
	}

	/// <summary>
	/// The ViewModel.
	/// </summary>
	public MainWindowViewModel? ViewModel
	{
		get => GetValue(ViewModelProperty);
		set => SetValue(ViewModelProperty, value);
	}

	object? IViewFor.ViewModel
	{
		get => ViewModel;
		set => ViewModel = (MainWindowViewModel?)value;
	}

	private void OnDataContextChanged(object? value)
	{
		if (value is MainWindowViewModel viewModel)
		{
			ViewModel = viewModel;
		}
		else
		{
			ViewModel = null;
		}
	}

	private void OnViewModelChanged(object? value)
	{
		if (value == null)
		{
			ClearValue(DataContextProperty);
		}
		else if (DataContext != value)
		{
			DataContext = value;
		}
	}
}