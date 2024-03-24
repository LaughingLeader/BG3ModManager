using System.IO;

using System.Text.RegularExpressions;

namespace ModManager.Models.NexusMods;

public struct NexusModFileVersionData
{
	public long ModId { get; set; }
	public long FileId { get; set; }
	public bool Success { get; set; }

	static readonly Regex _filePattern = new(@"^.*?-(\d+)-(.*?)(\d+)");

	public static NexusModFileVersionData FromFilePath(string path)
	{
		var name = Path.GetFileNameWithoutExtension(path);
		var match = _filePattern.Match(name);

		long modId = -1;
		long fileId = -1;

		if (match.Success)
		{
			if (long.TryParse(match.Groups[1]?.Value, out var mid))
			{
				modId = mid;
			}
			if (long.TryParse(match.Groups[3]?.Value, out var fid))
			{
				fileId = fid;
			}
		}

		return new NexusModFileVersionData()
		{
			ModId = modId,
			FileId = fileId,
			Success = match.Success
		};
	}
}
