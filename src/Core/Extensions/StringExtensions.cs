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
}
