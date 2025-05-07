using ModManager.Json;

using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace ModManager.Models.Mod;

[DataContract]
public partial class ModConfig : ReactiveObject, IObjectWithId
{
	public static string FileName => "ModManagerConfig.json";
	/// <summary>
	/// The mod UUID or FileName (override paks) associated with this config.
	/// </summary>
	public bool IsLoaded { get; set; }
	public string? Id { get; set; }

	[Reactive, DataMember] public string? Notes { get; set; }

	[Reactive, DataMember] public string? GitHub { get; set; }
	[Reactive, DataMember] public long NexusModsId { get; set; }
	[Reactive, DataMember] public string? ModioId { get; set; }

	[ObservableAsProperty] public string? GitHubAuthor { get; }
	[ObservableAsProperty] public string? GitHubRepository { get; }


	[GeneratedRegex("^.*/([^/]+)/([^/]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant)]
	private static partial Regex GitHubUrlPattern();

	private static readonly Regex _githubUrlPattern = GitHubUrlPattern();

	public static ValueTuple<string, string> GitHubUrlToParts(string? url)
	{
		if (url.IsValid() && !url.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
		{
			var match = _githubUrlPattern.Match(url);
			if (match.Success)
			{
				var author = match.Groups[1]?.Value ?? String.Empty;
				var repo = match.Groups[2]?.Value ?? String.Empty;
				return (author, repo);
			}
		}
		return (String.Empty, String.Empty);
	}

	public ModConfig()
	{
		var parseGitHubUrl = this.WhenAnyValue(x => x.GitHub).Select(GitHubUrlToParts);
		parseGitHubUrl.Select(x => x.Item1).ToPropertyEx(this, x => x.GitHubAuthor, String.Empty, false, RxApp.MainThreadScheduler);
		parseGitHubUrl.Select(x => x.Item2).ToPropertyEx(this, x => x.GitHubRepository, String.Empty, false, RxApp.MainThreadScheduler);
	}
}
