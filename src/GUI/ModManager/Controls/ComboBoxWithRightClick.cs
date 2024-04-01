using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace ModManager.Controls;

/// <summary>
/// Allows right clicking a ComboBox to open a Flyout (as a custom context menu);
/// The default behavior of ComboBox doesn't distinguish between click types - it just opens or closes on pointer pressed/released events.
/// </summary>
public class ComboBoxWithRightClick : ComboBox
{
	protected override Type StyleKeyOverride => typeof(ComboBox);

	protected override void OnPointerPressed(PointerPressedEventArgs e)
	{
		var props = e.GetCurrentPoint(this).Properties;
		if (props.PointerUpdateKind == PointerUpdateKind.RightButtonPressed)
		{
			e.Handled = true;
			return;
		}
		base.OnPointerPressed(e);
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e)
	{
		var props = e.GetCurrentPoint(this).Properties;
		if (props.PointerUpdateKind == PointerUpdateKind.RightButtonReleased)
		{
			e.Handled = true;
			FlyoutBase.ShowAttachedFlyout(this);
			return;
		}
		base.OnPointerReleased(e);
	}
}
