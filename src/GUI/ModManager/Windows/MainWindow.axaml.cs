using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Avalonia.VisualTree;

using ModManager.ViewModels.Main;

using ReactiveUI;

using SukiUI.Controls;

namespace ModManager.Windows;

public partial class MainWindow : SukiWindow, IViewFor<MainWindowViewModel>
{
	public static readonly StyledProperty<MainWindowViewModel?> ViewModelProperty = AvaloniaProperty
			.Register<MainWindow, MainWindowViewModel?>(nameof(ViewModel));

	public void ShowSukiDialog(object? content, bool showCardBehind = true, bool allowBackgroundClose = false)
	{
		var host = this.FindDescendantOfType<SukiHost>() ?? throw new InvalidOperationException("No SukiHost present in this window");
		var viewLocator = AppServices.Get<IViewLocator>();
		if (viewLocator != null)
		{
			Control? dialogContent = null;
			if (content is string message)
			{
				dialogContent = new TextBlock { Text = message };
			}
			else if(content is ReactiveObject viewModel)
			{
				if(viewLocator.ResolveView(viewModel) is Control view)
				{
					dialogContent = view;
				}
			}
			if(dialogContent != null)
			{
				host.IsDialogOpen = true;
				host.DialogContent = dialogContent;
				host.AllowBackgroundClose = allowBackgroundClose;
				host.GetTemplateChildren().First((Control n) => n.Name == "BorderDialog1").Opacity = (showCardBehind ? 1 : 0);
			}
		}
		else
		{
			throw new InvalidOperationException("Failed to get ViewLocator");
		}
	}

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
			var sukiHost = this.FindDescendantOfType<SukiHost>();
			if (sukiHost != null)
			{
				sukiHost.GetObservable(SukiHost.IsDialogOpenProperty).BindTo(ViewModelLocator.MessageBox, x => x.IsVisible);
			}

			this.OneWayBind(ViewModel, x => x.Router, x => x.ViewHost.Router);

			Dispatcher.UIThread.InvokeAsync(() => ViewModel.OnViewActivated(this), DispatcherPriority.Background);

			AppServices.Interactions.ShowMessageBox.RegisterHandler(context =>
			{
				return Observable.StartAsync(async () =>
				{
					var data = context.Input;
					var dialogVM = ViewModelLocator.MessageBox;
					dialogVM.Open(data);

					var isConfirmation = data.MessageBoxType.IsConfirmation();

					ShowSukiDialog(dialogVM, true, !isConfirmation);
					if (!isConfirmation)
					{
						context.SetOutput(true);
					}
					else
					{
						var result = await dialogVM.WaitForResult();
						context.SetOutput(result);
					}
					//SukiHost.ShowDialog(dialogVM, true, !data.MessageBoxType.HasFlag(InteractionMessageBoxType.YesNo));
				}, RxApp.TaskpoolScheduler);
			});
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