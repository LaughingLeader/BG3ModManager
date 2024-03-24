using System.Net.Http;

namespace ModManager.Services;
public class AppHttpClient : HttpClient
{
	public AppHttpClient(IEnvironmentService environmentService) : base()
	{
		// Required for GitHub permissions
		DefaultRequestHeaders.Add("User-Agent", environmentService.AppFriendlyName);
	}
}
