using AdonisUI.Extensions;

using ModManager.ViewModels;

using System.Windows.Data;

namespace ModManager.Controls;

public class DownloadActivityBarBase : ReactiveUserControl<DownloadActivityBarViewModel> { }

public partial class DownloadActivityBar : DownloadActivityBarBase
{
	public DownloadActivityBar()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			DataContext = ViewModel;
			if (ViewModel != null)
			{
				this.OneWayBind(ViewModel, vm => vm.CurrentValue, view => view.TaskProgressBar.Value);
				this.OneWayBind(ViewModel, vm => vm.CurrentText, view => view.TaskProgressWorkText.Text);
				this.OneWayBind(ViewModel, vm => vm.IsVisible, view => view.Visibility);
				this.BindCommand(ViewModel, vm => vm.CancelCommand, view => view.CancelButton);

				SetBinding(ProgressBarExtension.IsProgressAnimationEnabledProperty,
					new Binding(nameof(DownloadActivityBarViewModel.IsAnimating)) { Source = ViewModel });
			}
		});
	}
}
