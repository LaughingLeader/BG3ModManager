using Avalonia.LogicalTree;

namespace ModManager.Controls;
public class EnhancedTextBox : TextBox
{
	protected override Type StyleKeyOverride => typeof(TextBox);

	public static KeyGesture ClearGesture { get; } = new KeyGesture(Key.Delete, KeyModifiers.Control);

	public static readonly DirectProperty<EnhancedTextBox, bool> CanClearProperty =
					AvaloniaProperty.RegisterDirect<EnhancedTextBox, bool>(
						nameof(CanClear),
						o => o.CanClear);

	private bool _canClear;

	/// <summary>
	/// Property for determining if the Clear command can be executed.
	/// </summary>
	public bool CanClear
	{
		get { return _canClear; }
		private set { SetAndRaise(CanPasteProperty, ref _canClear, value); }
	}

	public EnhancedTextBox() : base()
	{
		PropertyChanged += EnhancedTextBox_PropertyChanged;

		KeyDown += EnhancedTextBox_KeyDown;
	}

	private void EnhancedTextBox_KeyDown(object? sender, KeyEventArgs e)
	{
		if (!IsFocused) return;

		if(e.Key == Key.Escape || ((e.Key == Key.Enter || e.Key == Key.Return) && !AcceptsReturn))
		{
			if(this.GetLogicalParent<Window>() is Window window)
			{
				window.Focus(NavigationMethod.Tab);
			}
		}
	}

	private void EnhancedTextBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
	{
		if(e.Property == TextProperty && e.NewValue is string text)
		{
			CanClear = !String.IsNullOrEmpty(text);
		}
	}
}
