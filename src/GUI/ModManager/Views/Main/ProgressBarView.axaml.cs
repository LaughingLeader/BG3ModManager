using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;

using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;
public partial class ProgressBarView : ReactiveUserControl<ProgressBarViewModel>
{
	public ProgressBarView()
	{
		InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif

		ProgressBarControl.Value = 0d;

		this.WhenActivated(d =>
		{
			this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);

			ViewModel.WhenAnyValue(x => x.Title).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.TitleTextControl.Text);
			ViewModel.WhenAnyValue(x => x.WorkText).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.WorkTextControl.Text);

			var whenValue = ViewModel.WhenAnyValue(x => x.Value)
			.Select(x => Math.Clamp(x, 0d, 100d))
			.ObserveOn(RxApp.MainThreadScheduler);

			whenValue.BindTo(this, x => x.ProgressBarControl.Value);
			whenValue.Select(x => $"{x:0}%").BindTo(this, x => x.ProgressValueTextControl.Text);
		});
	}
}
