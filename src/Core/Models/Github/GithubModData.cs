using DivinityModManager.Models.Mod;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Linq;

namespace DivinityModManager.Models.GitHub;

public class GitHubModData : ReactiveObject
{
	[Reactive] public string Url { get; set; }
	[Reactive] public GitHubLatestReleaseData LatestRelease { get; set; }

	/// <summary>
	/// True if Url is set.
	/// </summary>
	[Reactive] public bool IsEnabled { get; private set; }

	[ObservableAsProperty] public string Author { get; }
	[ObservableAsProperty] public string Repository { get; }

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
		parseGitHubUrl.Select(x => x.Item1).ToPropertyEx(this, x => x.Author, String.Empty, false, RxApp.MainThreadScheduler);
		parseGitHubUrl.Select(x => x.Item2).ToPropertyEx(this, x => x.Repository, String.Empty, false, RxApp.MainThreadScheduler);

		this.WhenAnyValue(x => x.Url, url => !String.IsNullOrEmpty(url)).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.IsEnabled);
	}
}
