using System.Windows;

namespace ModManager.Themes;

public static class DivinityColors
{
	public static ComponentResourceKey TagBackgroundColor => new(typeof(DivinityColors), "TagBackgroundColor");
	public static ComponentResourceKey CustomTagBackgroundColor => new(typeof(DivinityColors), "CustomTagBackgroundColor");
	public static ComponentResourceKey ModeTagBackgroundColor => new(typeof(DivinityColors), "ModeTagBackgroundColor");
	public static ComponentResourceKey ListInactiveColor => new(typeof(DivinityColors), "ListInactiveColor");

	public static ComponentResourceKey TagBackgroundBrush => new(typeof(DivinityColors), "TagBackgroundBrush");
	public static ComponentResourceKey CustomTagBackgroundBrush => new(typeof(DivinityColors), "CustomTagBackgroundBrush");
	public static ComponentResourceKey ModeTagBackgroundBrush => new(typeof(DivinityColors), "ModeTagBackgroundBrush");
	public static ComponentResourceKey ListInactiveBrush => new(typeof(DivinityColors), "ListInactiveBrush");

	public static ComponentResourceKey ListInactiveRectangle => new(typeof(DivinityColors), "ListInactiveRectangle");
}
