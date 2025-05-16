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

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		base.OnPropertyChanged(change);

		if (change.Property == TextProperty && change.NewValue is string text)
		{
			CanClear = text.IsValid();
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (!IsFocused) return;

		if (e.Key == Key.Escape || ((e.Key == Key.Enter || e.Key == Key.Return) && !AcceptsReturn))
		{
			if (this.GetLogicalParent<Window>() is Window window)
			{
				window.Focus(NavigationMethod.Tab);
			}
		}
	}

	private static void OnContextRequested(object? sender, ContextRequestedEventArgs e)
	{
		if(sender is EnhancedTextBox tb && !tb.IsFocused && tb.ContextFlyout is not null)
		{
			tb.Focus();
		}
	}

	public EnhancedTextBox() : base()
	{
		ContextRequested += OnContextRequested;
	}
}
