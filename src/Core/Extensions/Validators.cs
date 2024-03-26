namespace ModManager;
public static class Validators
{
	/// <summary>
	/// AbsolutePath is not null or empty.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static bool IsValid(this Uri? uri) => !string.IsNullOrEmpty(uri?.AbsolutePath);

	/// <summary>
	/// Not null or empty.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static bool IsValid(this string? str) => !string.IsNullOrEmpty(str);
}
