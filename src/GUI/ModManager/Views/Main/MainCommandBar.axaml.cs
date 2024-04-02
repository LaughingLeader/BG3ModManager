using Avalonia.Input;

using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class MainCommandBar : ReactiveUserControl<MainCommandBarViewModel>
{
	public MainCommandBar()
	{
		InitializeComponent();

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
	}
}