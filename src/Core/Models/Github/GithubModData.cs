using ModManager.Models.Mod;

namespace ModManager.Models.GitHub;

public class GitHubModData : ReactiveObject
{
	[Reactive] public string? Url { get; set; }
	[Reactive] public GitHubLatestReleaseData LatestRelease { get; set; }

	/// <summary>
	/// True if Url is set.
	/// </summary>
	[Reactive] public bool IsEnabled { get; private set; }

	[ObservableAsProperty] public string? Author { get; }
	[ObservableAsProperty] public string? Repository { get; }

	public void Update(GitHubModData data)
	{
		Url = data.Url;
		if (data.LatestRelease != null)
		{
			LatestRelease.Version = data.LatestRelease.Version;
			LatestRelease.Description = data.LatestRelease.Description;
			LatestRelease.Date = data.LatestRelease.Date;
			LatestRelease.BrowserDownloadLink = data.LatestRelease.BrowserDownloadLink;
		}
	}

	public GitHubModData()
	{
		LatestRelease = new GitHubLatestReleaseData();

		var parseGitHubUrl = this.WhenAnyValue(x => x.Url).Select(ModConfig.GitHubUrlToParts);
		parseGitHubUrl.Select(x => x.Item1).ToUIPropertyImmediate(this, x => x.Author);
		parseGitHubUrl.Select(x => x.Item2).ToUIPropertyImmediate(this, x => x.Repository);

		this.WhenAnyValue(x => x.Url, url => !string.IsNullOrEmpty(url)).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.IsEnabled);
	}
}
