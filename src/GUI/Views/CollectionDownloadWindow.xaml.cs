﻿using AdonisUI;

using DivinityModManager.Controls;
using DivinityModManager.Models.NexusMods;
using DivinityModManager.ViewModels;

using Microsoft.Windows.Themes;

using NexusModsNET.DataModels.GraphQL.Types;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DivinityModManager.Views
{
	public class CollectionDownloadWindowBase : HideWindowBase<CollectionDownloadWindowViewModel> { }

	public partial class CollectionDownloadWindow : CollectionDownloadWindowBase
	{
		private readonly Subject<bool> _taskResult;
		private IObservable<bool> TaskResult => _taskResult;

		private async Task OpenWindow(IInteractionContext<NexusGraphCollectionRevision, bool> context)
		{
			ViewModel.Load(context.Input);
			var result = await TaskResult;
			context.SetOutput(result);
		}

		private void Confirm() => _taskResult.OnNext(true);
		private void Cancel() => _taskResult.OnNext(false);

		private static ViewBase ModsGridView;
		private static ViewBase ModsCardView;

		private ViewBase GetView(bool isCardView) => isCardView ? ModsCardView : ModsGridView;

		public CollectionDownloadWindow()
		{
			InitializeComponent();

			ModsGridView = (ViewBase)FindResource("ModsListViewGridView");
			ModsCardView = (ViewBase)FindResource("ModsListViewCardView");
			var cardViewBaseStyle = (Style)FindResource("CardViewBase");
			var modListViewDefaultStyle = (Style)FindResource("ModListViewDefaultStyle");

			_taskResult = new();

			ViewModel = new CollectionDownloadWindowDesignViewModel();

			DivinityInteractions.OpenDownloadCollectionView.RegisterHandler(OpenWindow);

			ViewModel.ConfirmCommand = ReactiveCommand.Create(Confirm);
			ViewModel.CancelCommand = ReactiveCommand.Create(Cancel);

			Closed += (o, e) => Cancel();

			this.WhenActivated(d =>
			{
				this.OneWayBind(ViewModel, vm => vm.Mods, view => view.ModsGridListView.ItemsSource);
				this.OneWayBind(ViewModel, vm => vm.Mods, view => view.ModsCardListView.ItemsSource);
				this.OneWayBind(ViewModel, vm => vm.GridViewVisibility, view => view.ModsGridListView.Visibility);
				this.OneWayBind(ViewModel, vm => vm.CardViewVisibility, view => view.ModsCardListView.Visibility);

				this.OneWayBind(ViewModel, vm => vm.Title, view => view.CollectionTitleTextBlock.Text);
				this.OneWayBind(ViewModel, vm => vm.AuthorAvatar, view => view.AuthorImage.Source);
				this.OneWayBind(ViewModel, vm => vm.AuthorAvatarVisibility, view => view.AuthorImage.Visibility);

				//this.BindCommand(ViewModel, vm => vm.SelectAllCommand, view => view.CheckboxHeader);

				this.BindCommand(ViewModel, vm => vm.ConfirmCommand, view => view.ConfirmButton);
				this.BindCommand(ViewModel, vm => vm.CancelCommand, view => view.CancelButton);

				this.BindCommand(ViewModel, vm => vm.SetGridViewCommand, view => view.SetGridViewButton);
				this.BindCommand(ViewModel, vm => vm.SetCardViewCommand, view => view.SetCardViewButton);

				this.OneWayBind(ViewModel, vm => vm.IsCardView, view => view.SetGridViewButton.IsChecked, b => !b);
				this.OneWayBind(ViewModel, vm => vm.IsCardView, view => view.SetCardViewButton.IsChecked);
			});
		}

		GridViewColumnHeader _lastHeaderClicked = null;
		ListSortDirection _lastDirection = ListSortDirection.Ascending;

		private void Sort(string sortBy, ListSortDirection direction, object sender)
		{
			if (sortBy == "#") sortBy = "Index";

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

		private void SortGrid(object sender, GridViewColumnHeader headerClicked, RoutedEventArgs e)
		{
			ListSortDirection direction;

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

			var header = "";

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
				header = "IsSelected";
			}
			else if (headerClicked.Column.Header is Control c && c.ToolTip is string toolTip)
			{
				header = toolTip;
			}

			Sort(header, direction, sender);

			_lastHeaderClicked = headerClicked;
			_lastDirection = direction;
		}

		private void SortView(object sender, RoutedEventArgs e)
		{
			if(e.OriginalSource is GridViewColumnHeader headerClicked && headerClicked.Role != GridViewColumnHeaderRole.Padding)
			{
				SortGrid(sender, headerClicked, e);
			}
		}

		private void ModsListViewCardView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if(sender is ListView lv)
			{
				DivinityApp.Log($"ItemSource: {lv.ItemsSource} | DataContext: {lv.DataContext}");
			}
		}

		private void ListView_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is ListView lv)
			{
				DivinityApp.Log($"ItemSource: {lv.ItemsSource} | DataContext: {lv.DataContext}");
			}
		}
	}
}
