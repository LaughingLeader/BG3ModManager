﻿using AdonisUI;

using DivinityModManager.Converters;
using DivinityModManager.Models.App;
using DivinityModManager.Util;
using DivinityModManager.Util.ScreenReader;
using DivinityModManager.ViewModels;
using DivinityModManager.Views.Main;
using DivinityModManager.Windows;

using ReactiveUI;

using System.Data;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DivinityModManager.Views;

public class MainViewControlViewBase : ReactiveUserControl<MainWindowViewModel> { }

public partial class MainViewControl : MainViewControlViewBase
{
	private readonly MainWindow main;

	private readonly Dictionary<string, MenuItem> menuItems = [];
	public Dictionary<string, MenuItem> MenuItems => menuItems;

	private void RegisterKeyBindings()
	{
		foreach (var key in ViewModel.Keys.All)
		{
			var keyBinding = new KeyBinding(key.Command, key.Key, key.Modifiers);
			BindingOperations.SetBinding(keyBinding, InputBinding.CommandProperty, new Binding { Path = new PropertyPath("Command"), Source = key });
			BindingOperations.SetBinding(keyBinding, KeyBinding.KeyProperty, new Binding { Path = new PropertyPath("Key"), Source = key });
			BindingOperations.SetBinding(keyBinding, KeyBinding.ModifiersProperty, new Binding { Path = new PropertyPath("Modifiers"), Source = key });
			main.InputBindings.Add(keyBinding);
		}

		//Initial keyboard focus by hitting up or down
		var setInitialFocusCommand = ReactiveCommand.Create(() =>
		{
			var modManager = Services.Mods;
			if (!DivinityApp.IsKeyboardNavigating && modManager.ActiveSelected == 0 && modManager.InactiveSelected == 0)
			{
				Services.Get<ModOrderView>()?.ModLayout?.FocusInitialActiveSelected();
			}
		});
		main.InputBindings.Add(new KeyBinding(setInitialFocusCommand, Key.Up, ModifierKeys.None));
		main.InputBindings.Add(new KeyBinding(setInitialFocusCommand, Key.Down, ModifierKeys.None));

		foreach (var item in TopMenuBar.Items)
		{
			if (item is MenuItem entry)
			{
				if (entry.Header is string label)
				{
					menuItems.Add(label, entry);
				}
				else if (!String.IsNullOrWhiteSpace(entry.Name))
				{
					menuItems.Add(entry.Name, entry);
				}
			}
		}

		//Generating menu items
		var menuKeyProperties = typeof(AppKeys)
		.GetRuntimeProperties()
		.Where(prop => Attribute.IsDefined(prop, typeof(MenuSettingsAttribute)))
		.Select(prop => typeof(AppKeys).GetProperty(prop.Name));
		foreach (var prop in menuKeyProperties)
		{
			Hotkey key = (Hotkey)prop.GetValue(ViewModel.Keys);
			MenuSettingsAttribute menuSettings = prop.GetCustomAttribute<MenuSettingsAttribute>();
			if (String.IsNullOrEmpty(key.DisplayName))
				key.DisplayName = menuSettings.DisplayName;

			if (!menuItems.TryGetValue(menuSettings.Parent, out MenuItem parentMenuItem))
			{
				parentMenuItem = new MenuItem
				{
					Header = menuSettings.Parent
				};
				TopMenuBar.Items.Add(parentMenuItem);
				menuItems.Add(menuSettings.Parent, parentMenuItem);
			}

			MenuItem newEntry = new()
			{
				Header = menuSettings.DisplayName,
				InputGestureText = key.ToString(),
				Command = key.Command
			};
			BindingOperations.SetBinding(newEntry, MenuItem.CommandProperty, new Binding { Path = new PropertyPath("Command"), Source = key });
			parentMenuItem.Items.Add(newEntry);
			if (!String.IsNullOrWhiteSpace(menuSettings.ToolTip))
			{
				newEntry.ToolTip = menuSettings.ToolTip;
			}
			if (!String.IsNullOrWhiteSpace(menuSettings.Style))
			{
				Style style = (Style)TryFindResource(menuSettings.Style);
				if (style != null)
				{
					newEntry.Style = style;
				}
			}

			if (menuSettings.AddSeparator)
			{
				parentMenuItem.Items.Add(new Separator());
			}

			menuItems.Add(prop.Name, newEntry);
		}
	}

	protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
	{
		return new CachedAutomationPeer(this);
	}

	public void UpdateColorTheme(bool darkMode)
	{
		ResourceLocator.SetColorScheme(this.Resources, !darkMode ? MainWindow.LightTheme : MainWindow.DarkTheme);
		main.UpdateColorTheme(darkMode);
	}

	public void OnActivated()
	{
		this.OneWayBind(ViewModel, vm => vm.Router, view => view.RoutedViewHost.Router);

		this.WhenAnyValue(x => x.ViewModel.MainProgressIsActive).Take(1).Delay(TimeSpan.FromMilliseconds(25)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(b =>
		{
			this.MainBusyIndicator.Visibility = Visibility.Visible;
		});

		this.OneWayBind(ViewModel, vm => vm.MainProgressIsActive, view => view.MainBusyIndicator.IsBusy);

		this.OneWayBind(ViewModel, vm => vm.StatusBarRightText, view => view.StatusBarLoadingOperationTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.NexusModsLimitsText, view => view.StatusBarNexusLimitsTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.NexusModsProfileAvatarVisibility, view => view.NexusModsProfileImage.Visibility);
		this.OneWayBind(ViewModel, vm => vm.NexusModsProfileBitmapImage, view => view.NexusModsProfileImage.Source);

		this.OneWayBind(ViewModel, vm => vm.ModUpdatesAvailable, view => view.UpdatesButtonPanel.IsEnabled);

		this.OneWayBind(ViewModel, vm => vm.UpdatingBusyIndicatorVisibility, view => view.UpdatesToggleButtonBusyIndicator.Visibility);
		this.OneWayBind(ViewModel, vm => vm.UpdatesViewVisibility, view => view.UpdatesToggleButtonExpandImage.Visibility);
		this.OneWayBind(ViewModel, vm => vm.UpdateCountVisibility, view => view.UpdateCountTextBlock.Visibility);
		this.OneWayBind(ViewModel, vm => vm.ModUpdatesViewData.TotalUpdates, view => view.UpdateCountTextBlock.Text);

		this.BindCommand(ViewModel, vm => vm.ToggleUpdatesViewCommand, view => view.UpdateViewToggleButton);

		this.BindCommand(ViewModel, vm => vm.RefreshModUpdatesCommand, view => view.UpdateAllSourcesMenuItem);
		this.BindCommand(ViewModel, vm => vm.CheckForGitHubModUpdatesCommand, view => view.UpdateGitHubMenuItem);
		this.BindCommand(ViewModel, vm => vm.CheckForNexusModsUpdatesCommand, view => view.UpdateNexusModsMenuItem);
		this.BindCommand(ViewModel, vm => vm.CheckForSteamWorkshopUpdatesCommand, view => view.UpdateSteamWorkshopMenuItem);

		/*this.OneWayBind(ViewModel, vm => vm.UpdatesViewVisibility, view => view.ModUpdaterPanel.Visibility);
		var whenUpdatesViewData = ViewModel.WhenAnyValue(x => x.ModUpdatesViewData);
		whenUpdatesViewData.BindTo(this, x => x.ModUpdaterPanel.ViewModel);
		whenUpdatesViewData.BindTo(this, x => x.ModUpdaterPanel.DataContext);*/

		RegisterKeyBindings();

		/*this.DeleteFilesView.ViewModel.FileDeletionComplete += (o, e) =>
		{
			DivinityApp.Log($"Deleted {e.TotalFilesDeleted} file(s).");
			if (e.TotalFilesDeleted > 0)
			{
				if (!e.IsDeletingDuplicates)
				{
					var deletedUUIDs = e.DeletedFiles.Where(x => !x.IsWorkshop).Select(x => x.UUID).ToHashSet();
					//var deletedWorkshopUUIDs = e.DeletedFiles.Where(x => x.IsWorkshop).Select(x => x.UUID).ToHashSet();
					ViewModel.Views.ModOrder.RemoveDeletedMods(deletedUUIDs, e.RemoveFromLoadOrder);
				}
				main.Activate();
			}
		};*/
	}

	public MainViewControl(MainWindow window, MainWindowViewModel vm)
	{
		InitializeComponent();

		main = window;
		ViewModel = vm;

		DownloadBar.ViewModel = vm.DownloadBar;
	}
}
