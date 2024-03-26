using System.Windows;
using System.Windows.Controls;

namespace ModManager.Controls;

public class CardView : ViewBase
{
	private static readonly Type t = typeof(CardView);

	public static readonly DependencyProperty ItemContainerStyleProperty = ItemsControl.ItemContainerStyleProperty.AddOwner(t);
	public static readonly DependencyProperty ItemTemplateProperty = ItemsControl.ItemTemplateProperty.AddOwner(t);
	public static readonly DependencyProperty ItemWidthProperty = WrapPanel.ItemWidthProperty.AddOwner(t);
	public static readonly DependencyProperty ItemHeightProperty = WrapPanel.ItemHeightProperty.AddOwner(t);

	public Style ItemContainerStyle
	{
		get => (Style)GetValue(ItemContainerStyleProperty);
		set => SetValue(ItemContainerStyleProperty, value);
	}


	public DataTemplate ItemTemplate
	{
		get => (DataTemplate)GetValue(ItemTemplateProperty);
		set => SetValue(ItemTemplateProperty, value);
	}

	public double ItemWidth
	{
		get => (double)GetValue(ItemWidthProperty);
		set => SetValue(ItemWidthProperty, value);
	}

	public double ItemHeight
	{
		get => (double)GetValue(ItemHeightProperty);
		set => SetValue(ItemHeightProperty, value);
	}

	protected override object DefaultStyleKey => new ComponentResourceKey(t, "CardView");
	protected override object ItemContainerDefaultStyleKey => new ComponentResourceKey(t, "CardViewItem");

	public static ComponentResourceKey DefaultStyle => new(t, "CardView");

	public CardView() : base()
	{

	}
}
