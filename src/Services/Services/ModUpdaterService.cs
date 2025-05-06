using ModManager.Models.GitHub;
using ModManager.Models.Mod;
using ModManager.Models.NexusMods;
using ModManager.Models.Settings;
using ModManager.ModUpdater;
using ModManager.ModUpdater.Cache;
using ModManager.Util;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModManager.Services;
public class ModUpdaterService : ReactiveObject, IModUpdaterService
{
	private readonly NexusModsCacheHandler _nexus;
	public NexusModsCacheHandler NexusMods => _nexus;

	private readonly ModioCacheHandler _modio;
	public ModioCacheHandler Modio => _modio;

	private readonly GitHubModsCacheHandler _github;
	public GitHubModsCacheHandler GitHub => _github;

	[Reactive] public bool IsRefreshing { get; set; }

	private static readonly JsonSerializerOptions DefaultSerializerSettings = new()
	{
		AllowTrailingCommas = true,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	public async Task<bool> UpdateInfoAsync(IEnumerable<ModData> mods, CancellationToken token)
	{
		IsRefreshing = true;
		if (Modio.IsEnabled) await Modio.Update(mods, token);
		if (NexusMods.IsEnabled) await NexusMods.Update(mods, token);
		if (GitHub.IsEnabled) await GitHub.Update(mods, token);
		IsRefreshing = false;
		return false;
	}

	public async Task<bool> LoadCacheAsync(IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token)
	{
		await Modio.LoadCacheAsync(currentAppVersion, token);
		await NexusMods.LoadCacheAsync(currentAppVersion, token);
		await GitHub.LoadCacheAsync(currentAppVersion, token);

		await Observable.Start(() =>
		{
			foreach (var mod in mods)
			{
				if(!string.IsNullOrEmpty(mod.UUID))
				{
					if (Modio.CacheData.Mods.TryGetValue(mod.UUID, out var modioData))
					{
						mod.ModioData.Update(modioData);
					}
					if (NexusMods.CacheData.Mods.TryGetValue(mod.UUID, out var nexusData))
					{
						mod.NexusModsData.Update(nexusData);
					}
					if (GitHub.CacheData.Mods.TryGetValue(mod.UUID, out var githubData))
					{
						mod.GitHubData.Update(githubData);
					}
				}
			}
			return Unit.Default;
		}, RxApp.MainThreadScheduler);

		return false;
	}

	public async Task<bool> SaveCacheAsync(IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token)
	{
		if (Modio.IsEnabled)
		{
			await Modio.SaveCacheAsync(true, currentAppVersion, token);
		}
		if (NexusMods.IsEnabled)
		{
			foreach (var mod in mods.Where(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START).Select(x => x.NexusModsData))
			{
				NexusMods.CacheData.Mods[mod.UUID] = mod;
			}
			await NexusMods.SaveCacheAsync(true, currentAppVersion, token);
		}
		if (GitHub.IsEnabled)
		{
			await GitHub.SaveCacheAsync(true, currentAppVersion, token);
		}
		return false;
	}

	public bool DeleteCache()
	{
		var b1 = NexusMods.DeleteCache();
		var b2 = Modio.DeleteCache();
		var b3 = GitHub.DeleteCache();
		return b1 || b2 || b3;
	}

	public async Task<ModUpdaterResults> FetchUpdatesAsync(ModManagerSettings settings, IEnumerable<ModData> mods, CancellationToken token)
	{
		var appVersion = Locator.Current.GetService<IEnvironmentService>()?.AppVersion.ToString() ?? "Debug";

		//TODO
		IsRefreshing = true;
		var githubResults = await GetGitHubUpdatesAsync(mods, appVersion, token);
		var nexusResults = await GetNexusModsUpdatesAsync(mods, appVersion, token);
		var modioResults = await GetModioUpdatesAsync(settings, mods, appVersion, token);
		IsRefreshing = false;
		return new ModUpdaterResults(githubResults, nexusResults, modioResults);
	}

	public async Task<Dictionary<string, GitHubLatestReleaseData>> GetGitHubUpdatesAsync(IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token)
	{
		var results = new Dictionary<string, GitHubLatestReleaseData>();
		try
		{
			if (!GitHub.CacheData.CacheUpdated)
			{
				await GitHub.LoadCacheAsync(currentAppVersion, token);
				if (GitHub.IsEnabled)
				{
					await GitHub.Update(mods, token);
					await GitHub.SaveCacheAsync(true, currentAppVersion, token);
				}

				await Observable.Start(() =>
				{
					foreach (var mod in mods)
					{
						if (GitHub.CacheData.Mods.TryGetValue(mod.UUID, out var githubData))
						{
							results.Add(mod.UUID, githubData.LatestRelease);
							mod.GitHubData.Update(githubData);
						}
					}
					return Unit.Default;
				}, RxApp.MainThreadScheduler);
			}
			if (!GitHub.IsEnabled) return results;
			return await Locator.Current.GetService<IGitHubService>().GetLatestDownloadsForModsAsync(mods, token);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error fetching GitHub updates:\n{ex}");
		}
		return results;
	}

	public async Task<Dictionary<string, NexusModsModDownloadLink>> GetNexusModsUpdatesAsync(IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token)
	{
		var results = new Dictionary<string, NexusModsModDownloadLink>();
		try
		{
			if (!NexusMods.CacheData.CacheUpdated)
			{
				await NexusMods.LoadCacheAsync(currentAppVersion, token);
				if (NexusMods.IsEnabled)
				{
					await NexusMods.Update(mods, token);
					await NexusMods.SaveCacheAsync(true, currentAppVersion, token);
				}
				await Observable.Start(() =>
				{
					foreach (var mod in mods)
					{
						if (NexusMods.CacheData.Mods.TryGetValue(mod.UUID, out var nexusData))
						{
							mod.NexusModsData.Update(nexusData);
						}
					}
					return Unit.Default;
				}, RxApp.MainThreadScheduler);
			}
			if (!NexusMods.IsEnabled) return results;
			return await Locator.Current.GetService<INexusModsService>().GetLatestDownloadsForModsAsync(mods, token);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error fetching NexusMods updates:\n{ex}");
		}
		return results;
	}

	//TODO
	public async Task<Dictionary<string, Modio.Models.Download>> GetModioUpdatesAsync(ModManagerSettings settings, IEnumerable<ModData> mods, string currentAppVersion, CancellationToken token)
	{
		var results = new Dictionary<string, Modio.Models.Download>();
		try
		{
			if (!Modio.CacheData.CacheUpdated)
			{
				await Modio.LoadCacheAsync(currentAppVersion, token);
				if (Modio.IsEnabled)
				{
					await Modio.Update(mods, token);
					await Modio.SaveCacheAsync(true, currentAppVersion, token);
				}
				await Observable.Start(() =>
				{
					foreach (var mod in mods)
					{
						if (Modio.CacheData.Mods.TryGetValue(mod.UUID, out var modioData))
						{
							mod.ModioData.Update(modioData);
						}
					}
					return Unit.Default;
				}, RxApp.MainThreadScheduler);
			}
			if (!Modio.IsEnabled) return results;
			return await Locator.Current.GetService<IModioService>()!.GetLatestDownloadsForModsAsync(mods, token);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error fetching Modio updates:\n{ex}");
		}
		return results;
	}

	public ModUpdaterService()
	{
		_nexus = new NexusModsCacheHandler(DefaultSerializerSettings);
		_modio = new ModioCacheHandler(DefaultSerializerSettings);
		_github = new GitHubModsCacheHandler();
	}
}