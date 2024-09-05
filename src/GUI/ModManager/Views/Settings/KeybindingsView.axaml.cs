using Avalonia.Animation;
using Avalonia.Controls;

using ModManager.ViewModels.Settings;

namespace ModManager.Views.Settings;
public partial class KeybindingsView : ReactiveUserControl<KeybindingsViewModel>
{
	public KeybindingsView()
	{
		InitializeComponent();

		ViewModel = AppServices.Get<KeybindingsViewModel>();

		this.WhenActivated(d =>
		{
			var fadeInAnimation = (Animation)this.Resources["FadeInAnimation"]!;
			var fadeOutAnimation = (Animation)this.Resources["FadeOutAnimation"]!;

			if (ViewModel != null)
			{
				Observable.FromEvent<EventHandler<KeyEventArgs>, KeyEventArgs>(
					h => (sender, e) => h(e),
					h => KeyDown += h,
					h => KeyDown -= h
				).InvokeCommand(ViewModel.OnBindingKeyPressedCommand);

				ViewModel.WhenAnyValue(x => x.IsBindingKey).Subscribe(b =>
				{
					if(b)
					{
						KeyConfirmationGrid.IsHitTestVisible = true;
						if (KeyConfirmationGrid.Opacity <= 0)
						{
							RxApp.MainThreadScheduler.ScheduleAsync(async (sch, t) =>
							{
								await fadeInAnimation.RunAsync(KeyConfirmationGrid, t);
							});
						}
					}
					else
					{
						KeyConfirmationGrid.IsHitTestVisible = false;
						if (KeyConfirmationGrid.Opacity > 0)
						{
							RxApp.MainThreadScheduler.ScheduleAsync(async (sch, t) =>
							{
								await fadeOutAnimation.RunAsync(KeyConfirmationGrid, t);
							});
						}
					}
				});
			}
		});
	}
}
