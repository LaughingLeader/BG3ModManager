using ModManager.Services;

using System.Diagnostics.CodeAnalysis;

namespace ModManager;

public static class StringExtensions
{
	private static readonly IFileSystemService _fs;
	static StringExtensions()
	{
		_fs = Locator.Current.GetService<IFileSystemService>()!;
	}

	/// <summary>
	/// Expands environment variables and makes the path relative to the app directory if not rooted.
	/// </summary>
	public static string? ToRealPath(this string? path)
	{
		if (string.IsNullOrEmpty(path)) return path;

		var finalPath = Environment.ExpandEnvironmentVariables(path);
		if (!_fs.Path.IsPathRooted(finalPath))
		{
			finalPath = DivinityApp.GetAppDirectory(finalPath);
		}
		return finalPath;
	}

	/// <summary>
	/// Expands environment variables and makes the path relative to the app directory if not rooted.
	/// </summary>
	public static string? NormalizeDirectorySep(this string? path)
	{
		if (string.IsNullOrEmpty(path)) return path;

		return _fs.Path.TrimEndingDirectorySeparator(path.Replace(_fs.Path.AltDirectorySeparatorChar, _fs.Path.DirectorySeparatorChar));
	}

	public static string ThisOrFallback(this string? str, string fallback) => str ?? fallback;
}
