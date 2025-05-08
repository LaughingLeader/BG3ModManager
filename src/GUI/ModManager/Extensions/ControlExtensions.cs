using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager;
public static class ControlExtensions
{
	public static void DesignSetup(this UserControl	control)
	{
		//Workaround for not being able to set the designer background to a darker color
		if (Design.IsDesignMode && (control.Background == null || control.Background == Brushes.Transparent))
		{
			control.Background = Brushes.Black;
		}
	}

	/// <summary>
	/// Finds first descendant of given type.
	/// </summary>
	/// <typeparam name="T">Descendant type.</typeparam>
	/// <param name="visual">The logical.</param>
	/// <param name="includeSelf">If given logical should be included in search.</param>
	/// <returns>First descendant of given type.</returns>
	public static List<T>? FindVisualDescendantsOfType<T>(this Visual? visual, bool includeSelf = false) where T : class
	{
		if (visual is null)
		{
			return null;
		}

		if (includeSelf && visual is T result)
		{
			return [result];
		}

		var results = new List<T>();

		FindDescendantsOfTypeImpl<T>(visual, ref results);
		return results;
	}

	private static void FindDescendantsOfTypeImpl<T>(Visual visual, ref List<T> targetList) where T : class
	{
		foreach (var child in visual.GetVisualChildren())
		{
			if (child is T result)
			{
				targetList.Add(result);
			}

			FindDescendantsOfTypeImpl<T>(child, ref targetList);
		}
	}

	/// <summary>
	/// Finds first descendant of given type.
	/// </summary>
	/// <typeparam name="T">Descendant type.</typeparam>
	/// <param name="visual">The logical.</param>
	/// <param name="name">The control's Name.</param>
	/// <param name="includeSelf">If given logical should be included in search.</param>
	/// <returns>First descendant of given type.</returns>
	public static T? FindVisualDescendantWithName<T>(this Visual? visual, string name, bool includeSelf = false) where T : StyledElement
	{
		if (visual is null)
		{
			return null;
		}

		if (includeSelf && visual is T result && result.Name == name)
		{
			return result;
		}

		return FindDescendantWithNameImpl<T>(visual, name);
	}

	private static T? FindDescendantWithNameImpl<T>(Visual visual, string name) where T : StyledElement
	{
		foreach(var child in visual.GetVisualChildren())
		{
			if (child is T result && result.Name == name)
			{
				return result;
			}

			var childResult = FindVisualDescendantWithName<T>(child, name);

			if (childResult is not null)
			{
				return childResult;
			}
		}

		return null;
	}
}
