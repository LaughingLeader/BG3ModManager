using Avalonia.Controls.Templates;
using Avalonia.Threading;
using Avalonia.VisualTree;

using ModManager.ViewModels.Main;

using ReactiveUI;

using SukiUI.Controls;
using SukiUI.Dialogs;
using SukiUI.Toasts;

namespace ModManager.Windows;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
	private readonly ISukiToastManager _toastManager = new SukiToastManager();
	private readonly ISukiDialogManager _dialogManager = new SukiDialogManager();

	public void ShowSukiDialog(object? content, bool showCardBehind = true, bool allowBackgroundClose = false)
	{
		if (content is string message)
		{
			var dialogContent = new TextBlock { Text = message };
			var dialog = _dialogManager.CreateDialog().WithContent(dialogContent);
			dialog.SetCanDismissWithBackgroundClick(allowBackgroundClose);
			dialog.TryShow();
		}
		else if (content is ReactiveObject viewModel)
		{
			var dialog = _dialogManager.CreateDialog().WithViewModel(x =>
			{
				x.CanDismissWithBackgroundClick = allowBackgroundClose;
				return viewModel;
			});
			dialog.TryShow();
		}

		var borderDialog = DialogHost.GetTemplateChildren().FirstOrDefault(x => x.GetType() == typeof(Border));
		if (borderDialog != null)
		{
			borderDialog.Opacity = (showCardBehind ? 1 : 0);
		}
	}

	public MainWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			DialogHost.Manager = _dialogManager;
			DialogHost.GetObservable(SukiDialogHost.IsDialogOpenProperty).BindTo(ViewModelLocator.MessageBox, x => x.IsVisible);
			ToastHost.Manager = _toastManager;

			this.OneWayBind(ViewModel, x => x.Router, x => x.ViewHost.Router);

			Dispatcher.UIThread.InvokeAsync(() => ViewModel.OnViewActivated(this), DispatcherPriority.Background);

			var interactions = AppServices.Interactions;

			interactions.ShowMessageBox.RegisterHandler(context =>
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

			interactions.ShowAlert.RegisterHandler(async context =>
			{
				var data = context.Input;
				var title = data.Title;
				var duration = data.Timeout <= 0 ? TimeSpan.FromSeconds(10) : TimeSpan.FromSeconds(data.Timeout);
				if (!title.IsValid())
				{
					title = data.AlertType switch
					{
						AlertType.Danger => "Error",
						AlertType.Warning => "Warning",
						AlertType.Success => "Success",
						AlertType.Info => "Info",
						_ => "Info",
					};
				}
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					var toastBuilder = _toastManager.CreateToast().WithTitle(title).WithContent(data.Message);
					toastBuilder.SetCanDismissByClicking(true);
					toastBuilder.Delay(duration, _toastManager.Dismiss);
					toastBuilder.SetType(data.AlertType.ToNotificationType());
					toastBuilder.Queue();
				});
				context.SetOutput(true);
			});
		});
	}
}