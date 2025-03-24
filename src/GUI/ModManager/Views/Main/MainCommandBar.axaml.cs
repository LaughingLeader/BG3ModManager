using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

using DynamicData.Binding;

using ModManager.Models.Menu;
using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class MainCommandBar : ReactiveUserControl<MainCommandBarViewModel>
{
	private void AddMenuItem(IMenuEntry entry, ItemCollection target)
	{
		if(entry is MenuEntry menuEntry)
		{
			var menuItem = new MenuItem()
			{
				Command = menuEntry.Command,
			};
			if(menuEntry.DisplayName.StartsWith("_"))
			{
				menuItem.Header = new AccessText() { Text = menuEntry.DisplayName };
			}
			else
			{
				menuItem.Header = new TextBlock() { Text = menuEntry.DisplayName };
			}
			ToolTip.SetTip(menuItem, menuEntry.ToolTip);
			target.Add(menuItem);
			if(menuEntry.Children != null)
			{
				foreach(var child in menuEntry.Children)
				{
					AddMenuItem(child, menuItem.Items);
				}
			}
		}
		else if(entry is MenuSeparator)
		{
			target.Add(new Separator());
		}
	}

	public MainCommandBar()
	{
		InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif

		//TODO See if there's a way to fix FAComboBox staying "pressed" when opening a context menu

		Observable.FromEventPattern<PointerPressedEventArgs>(OrdersComboBox, nameof(OrdersComboBox.PointerPressed))
		.Subscribe(x =>
		{
			if (x.Sender is Visual v && x.EventArgs.GetCurrentPoint(v).Properties.IsRightButtonPressed)
			{
				x.EventArgs.Handled = true;
			}
		});

		/*Observable.FromEventPattern<ContextRequestedEventArgs>(OrdersComboBox, nameof(OrdersComboBox.ContextRequested))
		.Subscribe(x =>
		{
			if (x.Sender is Control target && Resources.TryGetValue("RenamingCommandBarFlyout", out var obj) && obj is CommandBarFlyout flyout)
			{
				flyout.ShowAt(target, false);
				x.EventArgs.Handled = true;
				OrdersComboBox.IsDropDownOpen = false;
			}
		});*/

		this.WhenActivated(d =>
		{
			if (ViewModel != null)
			{
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					foreach (var entry in ViewModel.MenuEntries)
					{
						AddMenuItem(entry, TopMenu.Items);
					}
				});

				//Temp fix for ComboBox selected items not updating from bindings

				var whenModOrder = ViewModel.WhenAnyValue(x => x.ModOrder).WhereNotNull();

				whenModOrder.Select(x => x.SelectedProfile).Subscribe(x =>
				{
					if (x != null && ProfileComboBox.SelectedItem == null)
					{
						ProfileComboBox.SelectedItem = x;
						ProfileComboBox.SelectedIndex = ViewModel.ModOrder.SelectedProfileIndex;
					}
				});

				whenModOrder.Select(x => x.SelectedModOrder).Subscribe(x =>
				{
					if (x != null && OrdersComboBox.SelectedItem == null)
					{
						OrdersComboBox.SelectedItem = x;
						OrdersComboBox.SelectedIndex = ViewModel.ModOrder.SelectedModOrderIndex;
					}
				});

				whenModOrder.Select(x => x.SelectedAdventureMod).Subscribe(x =>
				{
					if (x != null && CampaignComboBox.SelectedItem == null)
					{
						CampaignComboBox.SelectedItem = x;
						CampaignComboBox.SelectedIndex = ViewModel.ModOrder.SelectedAdventureModIndex;
					}
				});
			}
		});
	}
}