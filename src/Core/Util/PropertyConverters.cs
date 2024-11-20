﻿using System.Windows;

namespace DivinityModManager.Util;

public static class PropertyConverters
{
	public static Visibility BoolToVisibility(bool b) => b ? Visibility.Visible : Visibility.Collapsed;
	public static Visibility BoolToVisibilityReversed(bool b) => !b ? Visibility.Visible : Visibility.Collapsed;
	public static Visibility BoolTupleToVisibility(ValueTuple<bool, bool, bool, bool, bool> b) => b.Item1 || b.Item2 || b.Item3 || b.Item4 || b.Item5 ? Visibility.Visible : Visibility.Collapsed;
	/// <summary>
	/// Visible if not null or empty, otherwise collapsed.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static Visibility StringToVisibility(string str, Visibility fallback = Visibility.Collapsed) => !String.IsNullOrEmpty(str) ? Visibility.Visible : fallback;
	public static Visibility StringToVisibility(string str) => StringToVisibility(str, Visibility.Collapsed);
	public static Visibility StringToVisibilityReversed(string str, Visibility fallback = Visibility.Collapsed) => String.IsNullOrEmpty(str) ? Visibility.Visible : fallback;
	public static Visibility StringToVisibilityReversed(string str) => StringToVisibilityReversed(str, Visibility.Collapsed);
	public static Visibility UriToVisibility(Uri uri) => !String.IsNullOrEmpty(uri?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
	public static Visibility IntToVisibility(int i) => i > 0 ? Visibility.Visible : Visibility.Collapsed;
}
