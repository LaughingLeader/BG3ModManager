using System.Diagnostics.CodeAnalysis;

namespace ModManager;
public static class Validators
{
	/// <summary>
	/// AbsolutePath is not null or empty.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static bool IsValid([NotNullWhen(true)] this Uri? uri) => !string.IsNullOrEmpty(uri?.AbsolutePath);

	/// <summary>
	/// Not null or empty.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static bool IsValid([NotNullWhen(true)] this string? str) => !string.IsNullOrEmpty(str);
}
