using AdonisUI;

using DivinityModManager.Models.Updates;
using DivinityModManager.ViewModels;

using Microsoft.Windows.Themes;

using ReactiveUI;

using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DivinityModManager.Views;

public class ModUpdatesLayoutBase : ReactiveUserControl<ModUpdatesViewData> { }

public partial class ModUpdatesLayout : ModUpdatesLayoutBase
{
	public ModUpdatesLayout()
	{
		InitializeComponent();

		Loaded += (o,e) =>
		{
			UpdateBackgroundColors();
		};

		this.OneWayBind(ViewModel, vm => vm.Unlocked, view => view.IsManipulationEnabled);
		this.OneWayBind(ViewModel, vm => vm.Unlocked, view => view.IsEnabled);

		this.OneWayBind(ViewModel, vm => vm.Updates, view => view.UpdatesModListView.ItemsSource);

		this.BindCommand(ViewModel, vm => vm.UpdateModsCommand, view => view.UpdateButton);

		this.OneWayBind(ViewModel, vm => vm.AllSelected, view => view.ModUpdatesCheckboxHeader.IsChecked);
		this.BindCommand(ViewModel, vm => vm.ToggleSelectCommand, view => view.ModUpdatesCheckboxHeader);

		this.WhenActivated(d =>
		{

		});
	}

	public void UpdateBackgroundColors()
	{
		//Fix for IsEnabled False ListView having a system color border background we can't change.
		foreach (var border in this.FindVisualChildren<ClassicBorderDecorator>())
		{
			border.SetResourceReference(BackgroundProperty, Brushes.Layer4BackgroundBrush);
		}
	}

	private GridViewColumnHeader _lastHeaderClicked = null;
	private ListSortDirection _lastDirection = ListSortDirection.Ascending;

	public static string GetSortProperty(string sortBy) => sortBy switch
	{
		"Current"		=> nameof(DivinityModUpdateData.CurrentVersion),
		"Name"			=> nameof(DivinityModUpdateData.UpdateVersion),
		"Update Date"	=> nameof(DivinityModUpdateData.UpdateDateText),
		"Source"		=> nameof(DivinityModUpdateData.SourceText),
		_ => sortBy
	};

	private static void Sort(string sortBy, ListSortDirection direction, object sender)
	{
		sortBy = GetSortProperty(sortBy);

		if (sortBy != "")
		{
			try
			{
				ListView lv = sender as ListView;
				ICollectionView dataView =
					CollectionViewSource.GetDefaultView(lv.ItemsSource);

				dataView.SortDescriptions.Clear();
				SortDescription sd = new(sortBy, direction);
				dataView.SortDescriptions.Add(sd);
				dataView.Refresh();
			}
			catch (Exception ex)
			{
				DivinityApp.Log("Error sorting grid: " + ex.ToString());
			}
		}
	}

	private void SortGrid(object sender, RoutedEventArgs e)
	{
		ListSortDirection direction;

		if (e.OriginalSource is GridViewColumnHeader headerClicked)
		{
			if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
			{
				if (headerClicked != _lastHeaderClicked)
				{
					direction = ListSortDirection.Ascending;
				}
				else
				{
					if (_lastDirection == ListSortDirection.Ascending)
					{
						direction = ListSortDirection.Descending;
					}
					else
					{
						direction = ListSortDirection.Ascending;
					}
				}

				string header = "";

				if (headerClicked.Column.Header is TextBlock textBlock)
				{
					header = textBlock.Text;
				}
				else if (headerClicked.Column.Header is string gridHeader)
				{
					header = gridHeader;
				}
				else if (headerClicked.Column.Header is CheckBox)
				{
					header = nameof(DivinityModUpdateData.IsSelected);
				}
				else if (headerClicked.Column.Header is Control c && c.ToolTip is string toolTip)
				{
					header = toolTip;
				}

				Sort(header, direction, sender);

				_lastHeaderClicked = headerClicked;
				_lastDirection = direction;
			}
		}
	}

	private void SortModUpdatesGridView(object sender, RoutedEventArgs e)
	{
		SortGrid(sender, e);
	}
}
