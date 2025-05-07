namespace ModManager.Extensions;

public static class StringExtensions
{
	public static bool IsExistingDirectory(this string path)
	{
		return !string.IsNullOrWhiteSpace(path) && Directory.Exists(path);
	}

	public static bool IsExistingFile(this string path)
	{
		return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
	}
}
