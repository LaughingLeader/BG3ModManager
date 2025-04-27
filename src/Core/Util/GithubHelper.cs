namespace ModManager.Util;

public static class GitHubHelper
{
	private static readonly string GIT_URL_REPO_LATEST = "https://api.github.com/repos/{0}/releases/latest";
	private static readonly string GIT_URL_REPO_RELEASES = "https://api.github.com/repos/{0}/releases";

	private static readonly HttpCompletionOption _completionOption = HttpCompletionOption.ResponseContentRead;

	public static async Task<string> GetLatestReleaseJsonStringAsync(string repo, CancellationToken token)
	{
		var response = await WebHelper.GetAsync(String.Format(GIT_URL_REPO_LATEST, repo), _completionOption, token);
		return await response.Content.ReadAsStringAsync();
	}

	public static async Task<string> GetAllReleaseJsonStringAsync(string repo, CancellationToken token)
	{
		var response = await WebHelper.GetAsync(String.Format(GIT_URL_REPO_RELEASES, repo), _completionOption, token);
		return await response.Content.ReadAsStringAsync();
	}

	private static string? GetBrowserDownloadUrl(string dataString)
	{
		var jsonData = JsonUtils.SafeDeserialize<Dictionary<string, object>>(dataString);
		if (jsonData != null)
		{
			if (jsonData.TryGetValue("assets", out var assetsArray))
			{
				if (assetsArray is IEnumerable<object> assets)
				{
					foreach (var obj in assets)
					{
						if (obj is Dictionary<string, object> objDict
							&& objDict.TryGetValue("browser_download_url", out var browserUrl))
						{
							return browserUrl.ToString();
						}
					}
				}
			}
#if DEBUG
			var lines = jsonData.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());
			DivinityApp.Log($"Can't find 'browser_download_url' in:\n{String.Join(Environment.NewLine, lines)}");
#endif
		}
		return null;
	}

	public static async Task<string?> GetLatestReleaseLinkAsync(string repo, CancellationToken token)
	{
		var response = await WebHelper.GetAsync(String.Format(GIT_URL_REPO_LATEST, repo), _completionOption, token);
		if (response != null)
		{
			var data = await response.Content.ReadAsStringAsync(token);
			if (data != null) return GetBrowserDownloadUrl(data);
		}
		return null;
	}
}
