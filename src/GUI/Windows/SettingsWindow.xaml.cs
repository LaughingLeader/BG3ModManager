using ModManager.Controls;
using ModManager.Models.Settings;
using ModManager.Models.View;
using ModManager.Util;
using ModManager.ViewModels;

using DynamicData;

using ReactiveUI;

using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

using Xceed.Wpf.Toolkit;

namespace ModManager.Windows;

public class SettingsWindowBase : HideWindowBase<SettingsWindowViewModel> { }

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : SettingsWindowBase
{
	public SettingsWindow()
	{
		InitializeComponent();
	}

	private void CreateSettingsElements(ReactiveObject source, Type settingsModelType, AutoGrid targetGrid)
	{
		var props = settingsModelType.GetProperties()
			.Select(x => SettingsAttributeProperty.FromProperty(x))
			.Where(x => x.Attribute != null && !x.Attribute.HideFromUI).ToList();

		int count = props.Count + targetGrid.Children.Count + 1;
		int row = targetGrid.Children.Count;

		targetGrid.RowCount = count;
		targetGrid.Rows = String.Join(",", Enumerable.Repeat("auto", count));

		var debugModeBinding = new Binding(nameof(SettingsWindowViewModel.DeveloperModeVisibility))
		{
			Source = ViewModel,
			FallbackValue = Visibility.Collapsed
		};

		foreach (var prop in props)
		{
			var isBlankTooltip = String.IsNullOrEmpty(prop.Attribute.Tooltip);
			var targetRow = row;
			row++;
			var tb = new TextBlock
			{
				Text = prop.Attribute.DisplayName,
				ToolTip = !isBlankTooltip ? prop.Attribute.Tooltip : null,
			};
			targetGrid.Children.Add(tb);
			Grid.SetRow(tb, targetRow);

			if (prop.Attribute.IsDebug)
			{
				tb.SetBinding(TextBlock.VisibilityProperty, debugModeBinding);
			}

			if (prop.Property.PropertyType.IsEnum)
			{
				var combo = new ComboBox()
				{
					ToolTip = !isBlankTooltip ? prop.Attribute.Tooltip : null,
					DisplayMemberPath = "Description",
					SelectedValuePath = "Value",
					ItemsSource = prop.Property.PropertyType.GetEnumValues().Cast<Enum>().Select(x => new EnumEntry(x.GetDescription(), x))
				};
				combo.SetBinding(ComboBox.SelectedValueProperty, new Binding(prop.Property.Name)
				{
					Source = source,
					Mode = BindingMode.TwoWay,
					UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
				});
				targetGrid.Children.Add(combo);
				Grid.SetRow(combo, targetRow);
				Grid.SetColumn(combo, 1);
				continue;
			}

			if (prop.Property.PropertyType == typeof(TimeSpan))
			{
				if (TryFindResource("TimePeriodPicker") is DataTemplate template)
				{
					var element = template.LoadContent() as FrameworkElement;
					element.SetBinding(DataContextProperty, new Binding(prop.Property.Name)
					{
						Source = source,
						Mode = BindingMode.TwoWay,
						UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
					});

					targetGrid.Children.Add(element);
					Grid.SetRow(element, targetRow);
					Grid.SetColumn(element, 1);
				}
			}

			var propType = Type.GetTypeCode(prop.Property.PropertyType);

			switch (propType)
			{
				case TypeCode.Boolean:
					var cb = new CheckBox
					{
						ToolTip = !isBlankTooltip ? prop.Attribute.Tooltip : null,
						VerticalAlignment = VerticalAlignment.Center
					};
					//cb.HorizontalAlignment = HorizontalAlignment.Right;
					cb.SetBinding(CheckBox.IsCheckedProperty, new Binding(prop.Property.Name)
					{
						Source = source,
						Mode = BindingMode.TwoWay,
						UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
					});
					if (prop.Attribute.IsDebug)
					{
						cb.SetBinding(CheckBox.VisibilityProperty, debugModeBinding);
					}
					targetGrid.Children.Add(cb);
					Grid.SetRow(cb, targetRow);
					Grid.SetColumn(cb, 1);
					break;

				case TypeCode.String:
					var utb = new UnfocusableTextBox
					{
						ToolTip = !isBlankTooltip ? prop.Attribute.Tooltip : null,
						VerticalAlignment = VerticalAlignment.Center,
						//utb.HorizontalAlignment = HorizontalAlignment.Stretch;
						TextAlignment = TextAlignment.Left
					};
					utb.SetBinding(UnfocusableTextBox.TextProperty, new Binding(prop.Property.Name)
					{
						Source = source,
						Mode = BindingMode.TwoWay,
						UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
					});
					if (prop.Attribute.IsDebug)
					{
						utb.SetBinding(CheckBox.VisibilityProperty, debugModeBinding);
					}
					targetGrid.Children.Add(utb);
					Grid.SetRow(utb, targetRow);
					Grid.SetColumn(utb, 1);
					break;
				case TypeCode.Int32:
				case TypeCode.Int64:
					var ud = new Xceed.Wpf.Toolkit.IntegerUpDown
					{
						ToolTip = !isBlankTooltip ? prop.Attribute.Tooltip : null,
						VerticalAlignment = VerticalAlignment.Center,
						HorizontalAlignment = HorizontalAlignment.Left,
						Padding = new Thickness(4, 2, 4, 2),
						AllowTextInput = true
					};
					ud.SetBinding(IntegerUpDown.ValueProperty, new Binding(prop.Property.Name)
					{
						Source = ViewModel.ExtenderSettings,
						Mode = BindingMode.TwoWay,
						UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
					});
					if (prop.Attribute.IsDebug)
					{
						ud.SetBinding(VisibilityProperty, debugModeBinding);
					}
					targetGrid.Children.Add(ud);
					Grid.SetRow(ud, targetRow);
					Grid.SetColumn(ud, 1);
					break;
			}
		}
	}

	private SettingsWindowTab IndexToTab(int index)
	{
		return (SettingsWindowTab)index;
	}

	private int TabToIndex(SettingsWindowTab tab)
	{
		return (int)tab;
	}

	public void Init(MainWindowViewModel main)
	{
		ViewModel = new SettingsWindowViewModel(this, main);
		//main.WhenAnyValue(x => x.Settings).BindTo(ViewModel, vm => vm.Settings);

		var settingsService = AppServices.Get<ISettingsService>();

		var settingsFilePath = DivinityApp.GetAppDirectory("Data", settingsService.ManagerSettings.FileName);
		var keybindingsFilePath = AppKeys.SettingsFilePath();

		GeneralSettingsTabHeader.Tag = settingsFilePath;
		UpdateSettingsTabHeader.Tag = settingsFilePath;
		AdvancedSettingsTabHeader.Tag = settingsFilePath;
		KeybindingsTabHeader.Tag = keybindingsFilePath;
		this.OneWayBind(ViewModel, vm => vm.ExtenderSettingsFilePath, view => view.ScriptExtenderTabHeader.Tag);
		this.OneWayBind(ViewModel, vm => vm.ExtenderUpdaterSettingsFilePath, view => view.UpdaterTabHeader.Tag);

		this.KeyDown += SettingsWindow_KeyDown;
		KeybindingsListView.Loaded += (o, e) =>
		{
			if (KeybindingsListView.SelectedIndex < 0)
			{
				KeybindingsListView.SelectedIndex = 0;
			}
			ListViewItem row = (ListViewItem)KeybindingsListView.ItemContainerGenerator.ContainerFromIndex(KeybindingsListView.SelectedIndex);
			if (row != null && !FocusHelper.HasKeyboardFocus(row))
			{
				Keyboard.Focus(row);
			}
		};
		KeybindingsListView.KeyUp += KeybindingsListView_KeyUp;

		CreateSettingsElements(ViewModel.Settings, typeof(ModManagerSettings), SettingsAutoGrid);
		CreateSettingsElements(ViewModel.UpdateSettings, typeof(ModManagerUpdateSettings), UpdateSettingsAutoGrid);
		CreateSettingsElements(ViewModel.ExtenderSettings, typeof(ScriptExtenderSettings), ExtenderSettingsAutoGrid);
		CreateSettingsElements(ViewModel.ExtenderUpdaterSettings, typeof(ScriptExtenderUpdateConfig), ExtenderUpdaterSettingsAutoGrid);

		this.OneWayBind(ViewModel, vm => vm.Main.Keys.All, view => view.KeybindingsListView.ItemsSource);
		this.Bind(ViewModel, vm => vm.SelectedHotkey, view => view.KeybindingsListView.SelectedItem);

		this.Bind(ViewModel, vm => vm.Settings.DebugModeEnabled, view => view.DebugModeCheckBox.IsChecked);
		this.Bind(ViewModel, vm => vm.Settings.LogEnabled, view => view.LogEnabledCheckBox.IsChecked);

		this.OneWayBind(ViewModel, vm => vm.LaunchParams, view => view.GameLaunchParamsMainMenu.ItemsSource);
		GameLaunchParamsMainButton.Events().Click.Subscribe(e =>
		{
			this.GameLaunchParamsMainButton.ContextMenu.IsOpen = true;
		});

		this.Bind(ViewModel, vm => vm.Settings.GameLaunchParams, view => view.GameLaunchParamsTextBox.Text);

		this.Bind(ViewModel, vm => vm.ExtenderUpdaterSettings.UpdateChannel, view => view.UpdateChannelComboBox.SelectedValue);
		this.OneWayBind(ViewModel, vm => vm.ScriptExtenderUpdates, view => view.UpdaterTargetVersionComboBox.ItemsSource);
		this.OneWayBind(ViewModel, vm => vm.TargetVersion, view => view.UpdaterTargetVersionComboBox.Tag);
		this.Bind(ViewModel, vm => vm.TargetVersion, view => view.UpdaterTargetVersionComboBox.SelectedItem);

		//this.WhenAnyValue(x => x.UpdaterTargetVersionComboBox.SelectedItem).Subscribe(ViewModel.OnTargetVersionSelected);

		this.Bind(ViewModel, vm => vm.SelectedTabIndex, view => view.PreferencesTabControl.SelectedIndex, tab => TabToIndex(tab), index => IndexToTab(index));
		this.OneWayBind(ViewModel, vm => vm.ExtenderTabVisibility, view => view.ScriptExtenderTab.Visibility);
		this.OneWayBind(ViewModel, vm => vm.ExtenderUpdaterVisibility, view => view.ScriptExtenderUpdaterTab.Visibility);
		this.OneWayBind(ViewModel, vm => vm.ResetSettingsCommandToolTip, view => view.ResetSettingsButton.ToolTip);

		this.OneWayBind(ViewModel, vm => vm.TargetVersion.URL, view => view.TargetVersionContextCopyMenuItem.CommandParameter);
		this.OneWayBind(ViewModel, vm => vm.TargetVersion.URL, view => view.TargetVersionContextOpenMenuItem.CommandParameter);

		this.BindCommand(ViewModel, vm => vm.SaveSettingsCommand, view => view.SaveSettingsButton);
		this.BindCommand(ViewModel, vm => vm.OpenSettingsFolderCommand, view => view.OpenSettingsFolderButton);
		this.BindCommand(ViewModel, vm => vm.ResetSettingsCommand, view => view.ResetSettingsButton);
		this.BindCommand(ViewModel, vm => vm.ClearLaunchParamsCommand, view => view.ClearLaunchParamsMenuItem);
		this.BindCommand(ViewModel, vm => vm.ClearCacheCommand, view => view.ClearCacheButton);

		this.BindCommand(ViewModel, vm => vm.AssociateWithNXMCommand, view => view.AssociateWithNXMButton);

		this.Events().IsVisibleChanged.InvokeCommand(ViewModel.OnWindowShownCommand);

		DataContext = ViewModel;
	}

	private bool isSettingKeybinding = false;

	private void ClearFocus()
	{
		foreach (var item in KeybindingsListView.Items)
		{
			if (item is HotkeyEditorControl hotkey && hotkey.IsEditing)
			{
				hotkey.SetEditing(false);
			}
		}
	}

	private void FocusSelectedHotkey()
	{
		ListViewItem row = (ListViewItem)KeybindingsListView.ItemContainerGenerator.ContainerFromIndex(KeybindingsListView.SelectedIndex);
		var hotkeyControls = row.FindVisualChildren<HotkeyEditorControl>();
		foreach (var c in hotkeyControls)
		{
			c.SetEditing(true);
			isSettingKeybinding = true;
		}
	}

	private void KeybindingsListView_KeyUp(object sender, KeyEventArgs e)
	{
		if (KeybindingsListView.SelectedIndex >= 0 && e.Key == Key.Enter)
		{
			FocusSelectedHotkey();
		}
	}

	private void SettingsWindow_KeyDown(object sender, KeyEventArgs e)
	{
		if (isSettingKeybinding)
		{
			return;
		}
		else if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
		{
			ViewModel.SaveSettingsCommand.Execute(null);
			e.Handled = true;
		}
		else if (e.Key == Key.Left && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
		{
			int current = PreferencesTabControl.SelectedIndex;
			int nextIndex = current - 1;
			if (nextIndex < 0)
			{
				nextIndex = PreferencesTabControl.Items.Count - 1;
			}
			PreferencesTabControl.SelectedIndex = nextIndex;
			Keyboard.Focus((FrameworkElement)PreferencesTabControl.SelectedContent);
			MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		}
		else if (e.Key == Key.Right && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
		{
			int current = PreferencesTabControl.SelectedIndex;
			int nextIndex = current + 1;
			if (nextIndex >= PreferencesTabControl.Items.Count)
			{
				nextIndex = 0;
			}
			PreferencesTabControl.SelectedIndex = nextIndex;
			//Keyboard.Focus((FrameworkElement)PreferencesTabControl.SelectedContent);
			//MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
		}
	}

	private string lastWorkshopPath = "";

	public EventHandler OnWorkshopPathChanged { get; set; }

	private void WorkshopPathTextbox_GotFocus(object sender, RoutedEventArgs e)
	{
		lastWorkshopPath = ViewModel.Settings.WorkshopPath;
	}

	private void WorkshopPathTextbox_LostFocus(object sender, RoutedEventArgs e)
	{
		if (sender is TextBox tb && tb.Text != lastWorkshopPath)
		{
			OnWorkshopPathChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	private void HotkeyEditorControl_GotFocus(object sender, RoutedEventArgs e)
	{
		isSettingKeybinding = true;
	}

	private void HotkeyEditorControl_LostFocus(object sender, RoutedEventArgs e)
	{
		isSettingKeybinding = false;
	}

	private void HotkeyListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
	{
		FocusSelectedHotkey();
	}
}
