using System.Text.RegularExpressions;
using System.Web;

namespace ModManager.Models.NexusMods.NXM;

public class NexusDownloadModProtocolData : INexusModsProtocol
{
	public NexusModsProtocolType ProtocolType => NexusModsProtocolType.ModFile;

	private static readonly Regex _pattern = new("nxm://(?<game>[^/]+)/mods/(?<mod>[^/]+)/files/(?<file>[^?]+)", RegexOptions.IgnoreCase);

	public string? GameDomain { get; set; }
	public int ModId { get; set; }
	public int FileId { get; set; }
	public int UserId { get; set; }
	public string? Key { get; set; }
	public int Expires { get; set; }

	public bool IsValid => GameDomain.IsValid() && ModId > 0 && FileId > 0 && UserId > 0 && Key.IsValid() && Expires > 0;

	public string AsUrl => $"nxm://{GameDomain}/mods/{ModId}/files/{FileId}?key={Key}&expires={Expires}&user_id={UserId}";

	public override string ToString() => $"[NexusModsProtocolData] IsValid({IsValid}) GameDomain({GameDomain}) ModId({ModId}) FileId({FileId}) UserId({UserId}) Key({Key}) Expires({Expires})";

	private static int TryParseGroup(Group group)
	{
		if (int.TryParse(group?.Value, out var value))
		{
			return value;
		}
		return -1;
	}

	public static NexusDownloadModProtocolData FromUrl(string url)
	{
		var data = new NexusDownloadModProtocolData();
		if (url.IsValid())
		{
			var urlData = _pattern.Match(url);
			if (urlData.Success)
			{
				data.GameDomain = urlData.Groups["game"]?.Value;
				data.ModId = TryParseGroup(urlData.Groups["mod"]);
				data.FileId = TryParseGroup(urlData.Groups["file"]);
				//Replace ? with &, otherwise the ?key part will be included as the first key, aint with the url
				var queryData = HttpUtility.ParseQueryString(url.Replace("?", "&"));
				if (queryData != null)
				{
					data.Key = queryData.Get("key");
					if (int.TryParse(queryData.Get("expires"), out var expires))
					{
						data.Expires = expires;
					}
					if (int.TryParse(queryData.Get("user_id"), out var userId))
					{
						data.UserId = userId;
					}
				}
			}
		}
		return data;
	}
}
