using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using ModManager.Controls;
using ModManager.ViewModels;

using SukiUI.Toasts;

using System.Globalization;

namespace ModManager;

public partial class VersionGeneratorWindow : HideWindowBase<VersionGeneratorViewModel>
{
	private readonly ISukiToastManager _toastManager = new SukiToastManager();

	public VersionGeneratorWindow()
    {
		InitializeComponent();

		ToastHost.Manager = _toastManager;

		this.WhenActivated(d =>
		{
			ViewModel = AppServices.Get<VersionGeneratorViewModel>();

			if (ViewModel != null)
			{
				this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);

				Observable.FromEventPattern(VersionNumberTextBox, nameof(LostFocus))
				.Select(x => Unit.Default).InvokeCommand(ViewModel.UpdateVersionFromTextCommand);

				ViewModel.ShowAlertCommand.Subscribe(data =>
				{
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
				});
			}
		});
	}
}