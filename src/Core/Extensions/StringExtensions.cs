using System.Diagnostics.CodeAnalysis;

namespace ModManager.Extensions;

public static class StringExtensions
{
	public static bool IsExistingDirectory([NotNullWhen(true)] this string? path)
	{
		return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
	}

	public static bool IsExistingFile([NotNullWhen(true)] this string? path)
	{
		return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
	}

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
}
