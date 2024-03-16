using DivinityModManager.Models;
using DivinityModManager.Models.GitHub;
using DivinityModManager.Models.GitHub.Json;
using DivinityModManager.Util;

using Octokit;

using System.IO;

namespace DivinityModManager
{
	public interface IGitHubService
	{
		Task<GitHubLatestReleaseData> GetLatestReleaseAsync(string owner, string repo);
		Task<Dictionary<string, GitHubLatestReleaseData>> GetLatestDownloadsForModsAsync(IEnumerable<DivinityModData> mods, CancellationToken token);
	}
}

namespace DivinityModManager.AppServices
{
	public class GitHubService : IGitHubService
	{
		private readonly GitHubClient _client;

		private static readonly List<string> _archiveFormats = new() { ".7z", ".7zip", ".gzip", ".rar", ".tar", ".tar.gz", ".zip" };
		private static readonly List<string> _compressedFormats = new() { ".bz2", ".xz", ".zst", ".pak" };

		private static bool IsSupportedFile(string name)
		{
			var ext = Path.GetExtension(name).ToLower();
			return _archiveFormats.Contains(ext) || _compressedFormats.Contains(ext);
		}

		private static string GetDownloadFileUrl(IReadOnlyList<ReleaseAsset> assets)
		{
			var primary = assets.FirstOrDefault(x => IsSupportedFile(x.Name));
			if (primary != null) return primary.BrowserDownloadUrl;
			return assets.FirstOrDefault()?.BrowserDownloadUrl;
		}

		public async Task<GitHubLatestReleaseData> GetLatestReleaseAsync(string owner, string repo)
		{
			var result = await _client.Repository.Release.GetLatest(owner, repo);
			if (result != null)
			{
				return new GitHubLatestReleaseData()
				{
					Date = result.PublishedAt ?? DateTimeOffset.Now,
					Version = result.TagName,
					Description = result.Body,
					BrowserDownloadLink = GetDownloadFileUrl(result.Assets)
				};
			}
			return null;
		}

		public async Task<Dictionary<string, GitHubLatestReleaseData>> GetLatestDownloadsForModsAsync(IEnumerable<DivinityModData> mods, CancellationToken token)
		{
			var results = new Dictionary<string, GitHubLatestReleaseData>();

			try
			{
				foreach (var mod in mods)
				{
					if (mod.GitHubData.IsEnabled)
					{
						//For repositories set up with a "Repository.json" file.
						if(mod.GitHubData.Url.EndsWith(".json", StringComparison.InvariantCultureIgnoreCase))
						{
							var contents = await WebHelper.DownloadUrlAsStringAsync(mod.GitHubData.Url, token);
							if (!String.IsNullOrEmpty(contents) && DivinityJsonUtils.TrySafeDeserialize<GitHubRepositoryJsonData>(contents, out var data))
							{
								var latest = data.GetLatest(mod.UUID);
								if(latest != null && mod.Version < latest.Version)
								{
									results.Add(mod.UUID, new GitHubLatestReleaseData() { 
										BrowserDownloadLink = latest.DownloadUrl,
										Date = DateTimeOffset.Now,
										Version = latest.Version,
										Description = ""
									});
								}
							}
						}
						else
						{
							var latest = await GetLatestReleaseAsync(mod.GitHubData.Author, mod.GitHubData.Repository);
							if (latest != null && mod.Version < latest.Version)
							{
								results.Add(mod.UUID, latest);
							}
						}
					}

					if (token.IsCancellationRequested) break;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error fetching GitHub data:\n{ex}");
			}

			return results;
		}

		public GitHubService(string appName, string appVersion)
		{
			_client = new GitHubClient(new ProductHeaderValue(appName, appVersion));
		}
	}
}
