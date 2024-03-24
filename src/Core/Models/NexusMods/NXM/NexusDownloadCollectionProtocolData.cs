using System.Text.RegularExpressions;

namespace ModManager.Models.NexusMods.NXM;

public class NexusDownloadCollectionProtocolData : INexusModsProtocol
{
	public NexusModsProtocolType ProtocolType => NexusModsProtocolType.Collection;

	//nxm://{game}/collections/{slug}/revisions/{revisionID}
	private static readonly Regex _pattern = new("nxm://(?<game>[^/]+)/collections/(?<slug>[^/]+)/revisions/(?<revision>[^?]+)", RegexOptions.IgnoreCase);

	public string GameDomain { get; set; }
	public string Slug { get; set; }
	public int Revision { get; set; }

	public bool IsValid => !string.IsNullOrEmpty(GameDomain) && !string.IsNullOrEmpty(Slug) && Revision > -1;

	public string AsUrl => $"nxm://{GameDomain}/collections/{Slug}/revisions/{Revision}";

	public override string ToString() => $"[NexusModsProtocolData] IsValid({IsValid}) GameDomain({GameDomain}) Slug({Slug}) Revision({Revision})";

	private static int TryParseGroup(Group group)
	{
		if (int.TryParse(group?.Value, out var value))
		{
			return value;
		}
		return -1;
	}

	public static NexusDownloadCollectionProtocolData FromUrl(string url)
	{
		var data = new NexusDownloadCollectionProtocolData();
		if (!string.IsNullOrEmpty(url))
		{
			var urlData = _pattern.Match(url);
			if (urlData.Success)
			{
				data.GameDomain = urlData.Groups["game"]?.Value;
				data.Slug = urlData.Groups["slug"]?.Value;
				data.Revision = TryParseGroup(urlData.Groups["revision"]);
			}
		}
		return data;
	}
}
