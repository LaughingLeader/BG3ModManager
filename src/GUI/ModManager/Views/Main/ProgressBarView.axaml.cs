using Avalonia.Controls;

using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;
public partial class ProgressBarView : ReactiveUserControl<ProgressBarViewModel>
{
	public ProgressBarView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);
			ViewModel.WhenAnyValue(x => x.Title).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.TitleTextControl.Text);
			ViewModel.WhenAnyValue(x => x.WorkText).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.WorkTextControl.Text);
			ViewModel.WhenAnyValue(x => x.Value).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.ProgressBarControl.Value);
		});
	}
}
