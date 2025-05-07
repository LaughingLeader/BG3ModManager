using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ModManager.Util;

#nullable enable

public static class WebHelper
{
	private static HttpClient Client => Locator.Current.GetService<HttpClient>()!;

	public static Task<HttpResponseMessage> GetAsync([StringSyntax("Uri")] string? requestUri) => Client.GetAsync(requestUri);
	public static Task<HttpResponseMessage> GetAsync([StringSyntax("Uri")] string? requestUri, CancellationToken cancellationToken) => Client.GetAsync(requestUri, cancellationToken);
	public static Task<HttpResponseMessage> GetAsync([StringSyntax("Uri")] string? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken) => Client.GetAsync(requestUri, completionOption, cancellationToken);

	public static Task<HttpResponseMessage> PostAsync([StringSyntax("Uri")] string? requestUri, HttpContent? content) => Client.PostAsync(requestUri, content);
	public static Task<HttpResponseMessage> PostAsync([StringSyntax("Uri")] string? requestUri, HttpContent? content, CancellationToken token) => Client.PostAsync(requestUri, content, token);

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
		return string.Empty;
	}
}