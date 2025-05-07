using System.Globalization;
#if WPF
using System.Windows;
#endif

namespace ModManager.Util;

public static class PropertyConverters
{
#if WPF
	public static Visibility BoolToVisibility(bool b) => b ? Visibility.Visible : Visibility.Collapsed;
	public static Visibility BoolToVisibilityReversed(bool b) => !b ? Visibility.Visible : Visibility.Collapsed;
	public static Visibility BoolTupleToVisibility(ValueTuple<bool, bool, bool, bool, bool> b) => b.Item1 || b.Item2 || b.Item3 || b.Item4 || b.Item5 ? Visibility.Visible : Visibility.Collapsed;
	/// <summary>
	/// Visible if not null or empty, otherwise collapsed.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static Visibility StringToVisibility(string str, Visibility fallback = Visibility.Collapsed) => !string.IsNullOrEmpty(str) ? Visibility.Visible : fallback;
	public static Visibility StringToVisibility(string str) => StringToVisibility(str, Visibility.Collapsed);
	public static Visibility StringToVisibilityReversed(string str, Visibility fallback = Visibility.Collapsed) => string.IsNullOrEmpty(str) ? Visibility.Visible : fallback;
	public static Visibility StringToVisibilityReversed(string str) => StringToVisibilityReversed(str, Visibility.Collapsed);
	public static Visibility UriToVisibility(Uri uri) => !string.IsNullOrEmpty(uri?.ToString()) ? Visibility.Visible : Visibility.Collapsed;
	public static Visibility IntToVisibility(int i) => i > 0 ? Visibility.Visible : Visibility.Collapsed;
#else
	[Obsolete]
	public static bool BoolToVisibility(bool b) => b;

	[Obsolete]
	public static bool BoolToVisibilityReversed(bool b) => !b;
	public static bool BoolTupleToVisibility(ValueTuple<bool, bool, bool, bool, bool> b) => b.Item1 || b.Item2 || b.Item3 || b.Item4 || b.Item5;

	/// <summary>
	/// Visible if not null or empty, otherwise collapsed.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static bool StringToVisibility(string str) => !string.IsNullOrEmpty(str);
	public static bool StringToVisibilityReversed(string str) => string.IsNullOrEmpty(str);
	public static bool UriToVisibility(Uri uri) => !string.IsNullOrEmpty(uri?.ToString());
	public static bool IntToVisibility(int i) => i > 0;
#endif

	public static bool StringToBool(string str) => !string.IsNullOrEmpty(str);
	public static bool AnyBool(ValueTuple<bool, bool, bool> x) => x.Item1 || x.Item2 || x.Item3;
	public static string DateToString(DateTimeOffset date) => date.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture);
}
