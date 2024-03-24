using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace ModManager.Util;

public static class ElementHelper
{
	/// <summary>
	/// Finds a Child of a given item in the visual tree. 
	/// </summary>
	/// <param name="parent">A direct parent of the queried item.</param>
	/// <typeparam name="T">The type of the queried item.</typeparam>
	/// <param name="childName">x:Name or Name of child. </param>
	/// <returns>The first parent item that matches the submitted type parameter. 
	/// If not matching item can be found, 
	/// a null parent is being returned.</returns>
	public static T FindChild<T>(DependencyObject parent, string childName)
	   where T : DependencyObject
	{
		// Confirm parent and childName are valid. 
		if (parent == null) return null;

		T foundChild = null;

		int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
		for (int i = 0; i < childrenCount; i++)
		{
			var child = VisualTreeHelper.GetChild(parent, i);
			// If the child is not of the request child type child
			T childType = child as T;
			if (childType == null)
			{
				// recursively drill down the tree
				foundChild = FindChild<T>(child, childName);

				// If the child is found, break so we do not overwrite the found child. 
				if (foundChild != null) break;
			}
			else if (!String.IsNullOrEmpty(childName))
			{
				var frameworkElement = child as FrameworkElement;
				// If the child's name is set for search
				if (frameworkElement != null && frameworkElement.Name == childName)
				{
					// if the child's name is of the request name
					foundChild = (T)child;
					break;
				}
			}
			else
			{
				// child element found.
				foundChild = (T)child;
				break;
			}
		}

		return foundChild;
	}

	public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
	{
		if (depObj != null)
		{
			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
			{
				DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
				if (child != null && child is T)
				{
					yield return (T)child;
				}

				foreach (T childOfChild in FindVisualChildren<T>(child))
				{
					yield return childOfChild;
				}
			}
		}
	}

	// Source: https://stackoverflow.com/a/22420728
	private static Size MeasureTextSize(Visual target, string text, FontFamily fontFamily, FontStyle fontStyle,
		FontWeight fontWeight, FontStretch fontStretch, double fontSize)
	{
		var typeFace = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
		var ft = new FormattedText(text, CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, typeFace, fontSize, Brushes.Black, VisualTreeHelper.GetDpi(target).PixelsPerDip);
		return new Size(ft.Width, ft.Height);
	}

	public static Size MeasureText(Visual target, string text,
		FontFamily fontFamily,
		FontStyle fontStyle,
		FontWeight fontWeight,
		FontStretch fontStretch, double fontSize)
	{
		Typeface typeface = new(fontFamily, fontStyle, fontWeight, fontStretch);
		GlyphTypeface glyphTypeface;

		if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
		{
			return MeasureTextSize(target, text, fontFamily, fontStyle, fontWeight, fontStretch, fontSize);
		}

		double totalWidth = 0;
		double height = 0;

		for (int n = 0; n < text.Length; n++)
		{
			try
			{
				ushort glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];

				double width = glyphTypeface.AdvanceWidths[glyphIndex] * fontSize;

				double glyphHeight = glyphTypeface.AdvanceHeights[glyphIndex] * fontSize;

				if (glyphHeight > height)
				{
					height = glyphHeight;
				}

				totalWidth += width;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error measuring text:\n{ex}");
			}
		}

		return new Size(totalWidth, height);
	}
}
