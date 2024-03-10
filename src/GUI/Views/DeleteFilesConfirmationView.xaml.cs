using DivinityModManager.Converters;
using DivinityModManager.Util;
using DivinityModManager.ViewModels.Main;

using ReactiveUI;

using System.Reactive.Linq;
using System.Windows;

namespace DivinityModManager.Views;

public class DeleteFilesConfirmationViewBase : ReactiveUserControl<DeleteFilesViewData> { }

/// <summary>
/// Interaction logic for DeleteFilesConfirmationView.xaml
/// </summary>
public partial class DeleteFilesConfirmationView : DeleteFilesConfirmationViewBase
{
	private double GetLongestNameWidth()
	{
		var longestName = ViewModel.Files.OrderByDescending(x => x.DisplayName.Length).FirstOrDefault()?.DisplayName ?? "";
		if (!String.IsNullOrEmpty(longestName))
		{
			//DivinityApp.LogMessage($"Autosizing active mods grid for name {longestName}");
			var targetWidth = ElementHelper.MeasureText(FilesListView, longestName,
				FilesListView.FontFamily,
				FilesListView.FontStyle,
				FilesListView.FontWeight,
				FilesListView.FontStretch,
				FilesListView.FontSize).Width + 48;
			return targetWidth;
		}
		return 0d;
	}


	private void ResizeColumns()
	{
		var nameWidth = GetLongestNameWidth();
		var width = FilesListView.ActualWidth - SystemParameters.VerticalScrollBarWidth - FileListGridView.Columns[0].ActualWidth - nameWidth;
		FileListGridView.Columns[1].Width = nameWidth;

		if (FileListGridView.Columns.Count > 3)
		{
			FileListGridView.Columns[2].Width = width * 0.40;
			FileListGridView.Columns[3].Width = width * 0.60;
		}
		else
		{
			FileListGridView.Columns[2].Width = width;
		}
	}

	public DeleteFilesConfirmationView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			if (this.ViewModel != null)
			{
				d(this.OneWayBind(ViewModel, vm => vm.IsVisible, view => view.Visibility, BoolToVisibilityConverter.FromBool));
				d(this.OneWayBind(ViewModel, vm => vm.IsRunning, v => v.ProgressIndicator.IsBusy));
				d(this.OneWayBind(ViewModel, vm => vm.Files, v => v.FilesListView.ItemsSource));

				d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.TitleTextBlock.Text));

				d(this.OneWayBind(ViewModel, vm => vm.ProgressTitle, v => v.TaskProgressTitleText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.ProgressWorkText, v => v.TaskProgressWorkText.Text));
				d(this.OneWayBind(ViewModel, vm => vm.ProgressValue, v => v.TaskProgressBar.Value));
				d(this.OneWayBind(ViewModel, vm => vm.IsProgressActive, view => view.TaskProgressBar.Visibility, BoolToVisibilityConverter.FromBool));
				d(this.Bind(ViewModel, vm => vm.PermanentlyDelete, view => view.DeletionOptionCheckbox.IsChecked));

				d(this.Bind(ViewModel, vm => vm.RemoveFromLoadOrder, view => view.RemoveFromLoadOrderCheckbox.IsChecked));
				d(this.Bind(ViewModel, vm => vm.RemoveFromLoadOrderVisibility, view => view.RemoveFromLoadOrderCheckbox.Visibility));

				d(this.BindCommand(ViewModel, vm => vm.RunCommand, v => v.ConfirmButton));
				d(this.BindCommand(ViewModel, vm => vm.CancelRunCommand, v => v.CancelProgressButton));
				d(this.BindCommand(ViewModel, vm => vm.CloseCommand, v => v.CancelButton));

				//d(ViewModel.WhenAnyValue(x => x.IsDeletingDuplicates).Select(GetLastColumnWidth).BindTo(ViewModel, vm => vm.DuplicateColumnWidth));
				d(ViewModel.WhenAnyValue(x => x.IsDeletingDuplicates).Subscribe(b =>
				{
					if (!b)
					{
						FileListGridView.Columns.Remove(DuplicatesColumn);
					}
					else if (!FileListGridView.Columns.Contains(DuplicatesColumn))
					{
						FileListGridView.Columns.Add(DuplicatesColumn);
					}
					ResizeColumns();
				}));

				FilesListView.SizeChanged += (o, e) => ResizeColumns();
			}
		});
	}
}
