using System.Globalization;
#if WPF
using System.Windows;
#endif

namespace ModManager.Util;

public static class PropertyConverters
{
	[Obsolete]
	public static bool BoolToVisibility(bool b) => b;

	[Obsolete]
	public static bool BoolToVisibilityReversed(bool b) => !b;
	public static bool BoolTupleToVisibility(ValueTuple<bool, bool, bool, bool> b) => b.Item1 || b.Item2 || b.Item3 || b.Item4;
	public static bool BoolTupleToVisibility(ValueTuple<bool, bool, bool, bool, bool> b) => b.Item1 || b.Item2 || b.Item3 || b.Item4 || b.Item5;

	/// <summary>
	/// Visible if not null or empty, otherwise collapsed.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static bool StringToVisibility(string? str) => str.IsValid();
	public static bool StringToVisibilityReversed(string? str) => !str.IsValid();
	public static bool UriToVisibility(Uri uri) => uri.IsValid();
	public static bool IntToVisibility(int i) => i > 0;

	public static bool StringToBool(string? str) => str.IsValid();
	public static bool AnyBool(ValueTuple<bool, bool, bool> x) => x.Item1 || x.Item2 || x.Item3;
	public static string DateToString(DateTimeOffset date) => date.ToString(DivinityApp.DateTimeColumnFormat, CultureInfo.InstalledUICulture);
}
