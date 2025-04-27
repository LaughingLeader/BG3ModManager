using ModManager.Models.Mod;

using System.Globalization;

namespace ModManager.Util;

public static class StringUtils
{
	public static string BytesToString(long value)
	{
		string suffix;
		double readable;
		switch (Math.Abs(value))
		{
			case >= 0x1000000000000000:
				suffix = "EB";
				readable = value >> 50;
				break;
			case >= 0x4000000000000:
				suffix = "PB";
				readable = value >> 40;
				break;
			case >= 0x10000000000:
				suffix = "TB";
				readable = value >> 30;
				break;
			case >= 0x40000000:
				suffix = "GB";
				readable = value >> 20;
				break;
			case >= 0x100000:
				suffix = "MB";
				readable = value >> 10;
				break;
			case >= 0x400:
				suffix = "KB";
				readable = value;
				break;
			default:
				return value.ToString("0B");
		}

		return (readable / 1024).ToString("0.## ", CultureInfo.InvariantCulture) + suffix;
	}

	private static readonly Dictionary<string, string> replacePaths = [];

	private static void MaybeAddReplacement(string key, string path)
	{
		if (!String.IsNullOrEmpty(path))
		{
			replacePaths.Add(key, path);
		}
	}

	static StringUtils()
	{
		MaybeAddReplacement("%LOCALAPPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
		MaybeAddReplacement("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
		MaybeAddReplacement("%USERPROFILE%", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
	}

	public static string ReplaceSpecialPathways(string input)
	{
		if (!String.IsNullOrEmpty(input))
		{
			foreach (var kvp in replacePaths)
			{
				input = input.Replace(kvp.Value, kvp.Key);
			}
		}
		return input;
	}

	public static Uri StringToUri(string value)
	{
		if (!String.IsNullOrEmpty(value))
		{
			return new Uri(value);
		}
		return null;
	}

	public static string ModToTSVLine(ModData mod)
	{
		var index = mod.Index.ToString();
		if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
		{
			index = "Override";
		}
		var urls = String.Join(";", mod.GetAllURLs());
		return $"{index}\t{mod.Name}\t{mod.AuthorDisplayName}\t{mod.OutputPakName}\t{String.Join(", ", mod.Tags)}\t{String.Join(", ", mod.Dependencies.Items.Select(y => y.Name))}\t{urls}";
	}

	public static string ModToTextLine(ModData mod)
	{
		var index = mod.Index.ToString() + ".";
		if (mod.IsForceLoaded && !mod.IsForceLoadedMergedMod)
		{
			index = "Override";
		}
		var urls = String.Join(";", mod.GetAllURLs());
		return $"{index} {mod.Name} ({mod.OutputPakName}) {urls}";
	}
}
