using ModManager.Models.Mod;

using NexusModsNET.DataModels;

namespace ModManager.Models.NexusMods;

public struct NexusModsModDownloadLink
{
	public DivinityModData Mod { get; set; }
	public NexusModFileDownloadLink DownloadLink { get; set; }
	public NexusModFile File { get; set; }

	public NexusModsModDownloadLink(DivinityModData mod, NexusModFileDownloadLink link, NexusModFile file)
	{
		Mod = mod;
		DownloadLink = link;
		File = file;
	}
}
