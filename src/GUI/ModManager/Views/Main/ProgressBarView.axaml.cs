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

		ProgressBarControl.Value = 0d;

		this.WhenActivated(d =>
		{
			this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);

			/*var arc = ProgressBarControl.FindControl<Arc>("PART_ArcFill");
			if(arc != null)
			{
				var transition = arc.Transitions.OfType<DoubleTransition>().FirstOrDefault();
			}*/

			ViewModel.WhenAnyValue(x => x.Title).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.TitleTextControl.Text);
			ViewModel.WhenAnyValue(x => x.WorkText).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.WorkTextControl.Text);
			var whenValue = ViewModel.WhenAnyValue(x => x.Value).ObserveOn(RxApp.MainThreadScheduler);
			whenValue.BindTo(this, x => x.ProgressBarControl.Value);
			whenValue.Select(x => $"{x:0}%").BindTo(this, x => x.ProgressValueTextControl.Text);
		});
	}
}
