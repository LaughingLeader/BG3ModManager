using ModManager.Converters;
using ModManager.ViewModels;

using ReactiveUI;

namespace ModManager.Views;

public class ExportOrderToArchiveViewViewBase : ReactiveUserControl<ExportOrderToArchiveViewModel> { }

/// <summary>
/// Interaction logic for ExportOrderToArchiveView.xaml
/// </summary>
public partial class ExportOrderToArchiveView : ExportOrderToArchiveViewViewBase
{
	public ExportOrderToArchiveView()
	{
		InitializeComponent();

		ViewModel = ViewModelLocator.ExportOrderToArchive;

		this.WhenActivated(d =>
		{
			d(this.OneWayBind(ViewModel, vm => vm.IsVisible, view => view.Visibility, BoolToVisibilityConverter.FromBool));

			d(this.OneWayBind(ViewModel, vm => vm.Entries, v => v.FilesListView.ItemsSource));

			d(this.OneWayBind(ViewModel, vm => vm.IsRunning, v => v.ProgressIndicator.IsBusy));
			d(this.OneWayBind(ViewModel, vm => vm.ProgressTitle, v => v.TaskProgressTitleText.Text));
			d(this.OneWayBind(ViewModel, vm => vm.ProgressWorkText, v => v.TaskProgressWorkText.Text));
			d(this.OneWayBind(ViewModel, vm => vm.ProgressValue, v => v.TaskProgressBar.Value));
			d(this.OneWayBind(ViewModel, vm => vm.IsProgressActive, view => view.TaskProgressBar.Visibility, BoolToVisibilityConverter.FromBool));

			d(this.Bind(ViewModel, vm => vm.IncludeOverrides, view => view.IncludeOverridesCheckbox.IsChecked));
			d(this.Bind(ViewModel, vm => vm.SelectedOrderType, view => view.OrderTypeComboBox.SelectedItem));
			d(this.Bind(ViewModel, vm => vm.OrderTypes, view => view.OrderTypeComboBox.ItemsSource));

			//d(this.BindCommand(ViewModel, vm => vm.SelectAllCommand, v => v.ConfirmButton));
			d(this.BindCommand(ViewModel, vm => vm.RunCommand, v => v.ConfirmButton));
			d(this.BindCommand(ViewModel, vm => vm.CancelRunCommand, v => v.CancelProgressButton));
			d(this.BindCommand(ViewModel, vm => vm.CloseCommand, v => v.CancelButton));
		});
	}
}
