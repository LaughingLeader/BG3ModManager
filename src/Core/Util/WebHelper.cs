using System.IO;
using System.Net;
using System.Net.Http;

namespace ModManager.Util;

public static class WebHelper
{
	public static readonly HttpClient Client = new();

	public static void SetupClient()
	{
		// Required for GitHub permissions
		Client.DefaultRequestHeaders.Add("User-Agent", "BG3ModManager");
	}

	public static async Task<Stream> DownloadFileAsStreamAsync(string downloadUrl, CancellationToken token)
	{
		try
		{
			var fileStream = await Client.GetStreamAsync(downloadUrl, token);
			return fileStream;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error downloading url ({downloadUrl}):\n{ex}");
			return Stream.Null;
		}
	}

	public static async Task<string> DownloadUrlAsStringAsync(string downloadUrl, CancellationToken token)
	{
		try
		{
			var result = await Client.GetStringAsync(downloadUrl, token);
			return result;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error downloading url ({downloadUrl}):\n{ex}");
		}
		return String.Empty;
	}
}
