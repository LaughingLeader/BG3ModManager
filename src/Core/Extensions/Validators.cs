using ModManager.Services;

using System.Diagnostics.CodeAnalysis;

namespace ModManager;
public static class Validators
{
	private static readonly IFileSystemService _fs;
	static Validators()
	{
		_fs = Locator.Current.GetService<IFileSystemService>()!;
	}
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

	/// <summary>
	/// True if the path is not empty and the directory exists.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static bool IsExistingDirectory([NotNullWhen(true)] this string? path)
	{
		return !string.IsNullOrWhiteSpace(path) && _fs.Directory.Exists(path);
	}

	/// <summary>
	/// True if the path is not empty and the file exists.
	/// </summary>
	/// <param name="path"></param>
	/// <returns></returns>
	public static bool IsExistingFile([NotNullWhen(true)] this string? path)
	{
		return !string.IsNullOrWhiteSpace(path) && _fs.File.Exists(path);
	}
}
