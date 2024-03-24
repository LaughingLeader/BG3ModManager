namespace ModManager.Models.GitHub;

public class GitHubLatestReleaseData : ReactiveObject
{
	[Reactive] public string Version { get; set; }
	[Reactive] public string Description { get; set; }
	[Reactive] public DateTimeOffset Date { get; set; }
	[Reactive] public string BrowserDownloadLink { get; set; }
}
