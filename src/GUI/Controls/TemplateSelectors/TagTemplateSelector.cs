using System.Windows;
using System.Windows.Controls;

namespace ModManager.Controls.TemplateSelectors;

public class TagTemplateSelector : DataTemplateSelector
{
	private readonly string[] modeValues = { "Story", "GM", "Arena" };
	private readonly string[] defaultWorkshopTags = { "Armors", "Balancing/Stats", "Classes", "Companions", "Consumables", "Maps", "Origins", "Overhauls", "Quality of Life", "Quests", "Races", "Runes/Boosts", "Skills", "Utility", "Visual Overrides", "Weapons" };

	public override DataTemplate SelectTemplate(object item, DependencyObject container)
	{
		FrameworkElement element = container as FrameworkElement;

		if (element != null && item != null && item is string tag)
		{
			if (modeValues.Any(x => x.Equals(tag, StringComparison.OrdinalIgnoreCase)))
			{
				return element.FindResource("ModeTagTemplate") as DataTemplate;
			}
			else if (defaultWorkshopTags.Any(x => x.Equals(tag, StringComparison.OrdinalIgnoreCase)))
			{
				return element.FindResource("TagTemplate") as DataTemplate;
			}
			else
			{
				return element.FindResource("CustomTagTemplate") as DataTemplate;
			}
		}

		return null;
	}
}
