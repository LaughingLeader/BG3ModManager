using ModManager.Models.Mod;

using NexusModsNET.DataModels;

namespace ModManager.Models.NexusMods;

public struct NexusModsModDownloadLink
{
	public ModData Mod { get; set; }
	public NexusModFileDownloadLink DownloadLink { get; set; }
	public NexusModFile File { get; set; }

	public NexusModsModDownloadLink(ModData mod, NexusModFileDownloadLink link, NexusModFile file)
	{
		Mod = mod;
		DownloadLink = link;
		File = file;
	}
}
