using ModManager.ViewModels;
using ModManager.ViewModels.Settings;
using ModManager.Views.Generated;

namespace ModManager.Windows;
public partial class StatsValidatorWindow : ReactiveWindow<StatsValidatorWindowViewModel>
{
	public StatsValidatorWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			ViewModel = AppServices.Get<StatsValidatorWindowViewModel>();

			if (ViewModel != null)
			{
				d(this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible));
				d(this.OneWayBind(ViewModel, vm => vm.ModName, view => view.TitleTextBlock.Text, name => $"{name} Results"));
			}
		});
	}
}