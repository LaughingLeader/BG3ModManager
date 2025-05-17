using System.Diagnostics.CodeAnalysis;

namespace ModManager;

public static class StringExtensions
{
	/// <summary>
	/// Expands environment variables and makes the path relative to the app directory if not rooted.
	/// </summary>
	public static string ToRealPath(this string path)
	{
		if (string.IsNullOrEmpty(path)) return path;

		var finalPath = Environment.ExpandEnvironmentVariables(path);
		if (!Path.IsPathRooted(finalPath))
		{
			finalPath = DivinityApp.GetAppDirectory(finalPath);
		}
		return finalPath;
	}

	public static string ThisOrFallback(this string? str, string fallback) => str ?? fallback;
}
