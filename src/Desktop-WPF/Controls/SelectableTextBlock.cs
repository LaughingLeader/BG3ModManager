using ModManager.Controls.Util;

using System.Windows;
using System.Windows.Controls;

namespace ModManager.Controls;

/// <summary>
/// Source: https://stackoverflow.com/a/45627524
/// </summary>
public class SelectableTextBlock : TextBlock
{
	static SelectableTextBlock()
	{
		FocusableProperty.OverrideMetadata(typeof(SelectableTextBlock), new FrameworkPropertyMetadata(true));
		TextEditorWrapper.RegisterCommandHandlers(typeof(SelectableTextBlock), true, true, true);

		// remove the focus rectangle around the control
		FocusVisualStyleProperty.OverrideMetadata(typeof(SelectableTextBlock), new FrameworkPropertyMetadata((object)null));
	}

	public SelectableTextBlock()
	{
		TextEditorWrapper.CreateFor(this);
	}
}
