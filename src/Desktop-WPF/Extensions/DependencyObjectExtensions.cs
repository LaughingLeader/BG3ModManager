using System.Windows;
using System.Windows.Media;

namespace ModManager;

public static class DependencyObjectExtensions
{
	public static IEnumerable<T> FindVisualChildren<T>(this DependencyObject depObj) where T : DependencyObject
	{
		if (depObj != null)
		{
			for (var i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				var child = VisualTreeHelper.GetChild(depObj, i);
				if (child != null && child is T)
				{
					yield return (T)child;
				}

				foreach (var childOfChild in FindVisualChildren<T>(child))
				{
					yield return childOfChild;
				}
			}
		}
	}
	public static T FindVisualParent<T>(this DependencyObject depObj) where T : DependencyObject
	{
		if (depObj != null)
		{
			//get parent item
			var parentObject = VisualTreeHelper.GetParent(depObj);

			//we've reached the end of the tree
			if (parentObject == null) return null;

			//check if the parent matches the type we're looking for
			var parent = parentObject as T;
			if (parent != null)
				return parent;
			else
				return FindVisualParent<T>(parentObject);
		}
		return null;
	}
}
