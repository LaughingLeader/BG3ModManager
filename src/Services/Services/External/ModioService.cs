using Modio;
using Modio.Filters;
using Modio.Models;

using ModManager.Models.Mod;
using ModManager.Models.Updates;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reactive.Linq;
using System.Text.Json;

namespace ModManager.Services;
public class ModioService : ReactiveObject, IModioService
{
	private readonly IFileSystemService _fs;

	private static readonly uint GAME_ID = 6715;
	private Client? _client;

	[Reactive] public string ApiKey { get; set; }
	[Reactive] public bool LimitExceeded { get; set; }
	[ObservableAsProperty] public bool IsInitialized { get; }
	[ObservableAsProperty] public bool CanFetchData { get; }

	[MemberNotNullWhen(true, nameof(_client))]
	private bool TryInitClient()
	{
		if (_client != null) return true;
		if(ApiKey.IsValid())
		{
			try
			{
				_client = new Client(new Uri("https://g-6715.modapi.io/v1/"), new Credentials(ApiKey));
				this.RaisePropertyChanged(nameof(_client));
				return _client != null;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error initializing mod.io client:\n{ex}");
			}
		}
		return false;
	}

	private UpdateResult LoadTestData(IEnumerable<ModData> mods, CancellationToken token)
	{
		var updateResult = new UpdateResult();

		var modDataText = _fs.File.ReadAllText(DivinityApp.GetAppDirectory("TEST\\BG3_modio_mods.json"));
		var cachedModData = JsonSerializer.Deserialize<Result<Mod>>(modDataText);
		if (cachedModData?.Data != null)
		{
			var modDict = mods.ToDictionary(x => x.PublishHandle);
			updateResult.Success = true;
			foreach (var result in cachedModData.Data)
			{
				if (modDict.TryGetValue(result.Id, out var existingMod))
				{
					existingMod.ModioData.Update(result);
					updateResult.UpdatedMods.Add(existingMod);
				}
			}
		}
		return updateResult;
	}

	public async Task<UpdateResult> FetchModInfoAsync(IEnumerable<ModData> mods, CancellationToken token)
	{
		var updateResult = new UpdateResult();
		if (!TryInitClient())
		{
			updateResult.Success = false;
			updateResult.FailureMessage = "Failed to initialize client with given ApiKey";
			return updateResult;
		}

		var modIds = mods.Select(x => (uint)x.PublishHandle).ToArray();
		var filter = ModFilter.Id.In(modIds);

		try
		{
			var modsResults = await _client.Games[GAME_ID].Mods.Search(filter).ToList(token);

			if (modsResults != null)
			{
				updateResult.Success = true;
				var modDict = mods.ToDictionary(x => x.PublishHandle);
				foreach (var modResult in modsResults)
				{
					if (modDict.TryGetValue(modResult.Id, out var existingMod))
					{
						existingMod.ModioData.Update(modResult);
						updateResult.UpdatedMods.Add(existingMod);
					}
				}
				LimitExceeded = false;
			}
		}
		catch(RateLimitExceededException rateEx)
		{
			DivinityApp.Log($"mod.io rate limit exceeded:\n{rateEx}");
			LimitExceeded = true;
		}
		catch(Exception ex)
		{
			DivinityApp.Log($"Error fetching mod.io data:\n{ex}");
		}

		return updateResult;
	}

	public async Task<Dictionary<string, Download>> GetLatestDownloadsForModsAsync(IEnumerable<ModData> mods, CancellationToken token)
	{
		var downloads = new Dictionary<string, Download>();
		var result = await FetchModInfoAsync(mods, token);
		if(result.Success)
		{
			foreach(var mod in result.UpdatedMods)
			{
				var download = mod.ModioData.Data?.Modfile?.Download;
				if(download != null)
				{
					downloads.Add(mod.UUID!, download);
				}
			}
		}
		return downloads;
	}

	public async Task<List<string>> DownloadFilesForModsAsync(IEnumerable<ModData> mods, CancellationToken token)
	{
		var downloadedFiles = new List<string>();

		if (!TryInitClient())
		{
			return downloadedFiles;
		}

		var tempDir = DivinityApp.GetAppDirectory("Temp");
		_fs.Directory.CreateDirectory(tempDir);

		var tasks = new List<Task>();
		
		foreach(var mod in mods)
		{
			var modFile = mod.ModioData.Data?.Modfile;
			if (modFile != null)
			{
				var destFile = new FileInfo(_fs.Path.Join(tempDir, modFile.Filename));
				downloadedFiles.Add(destFile.FullName);
				tasks.Add(_client.Download(modFile, destFile, token));
			}
		}

		await Task.WhenAll(tasks).WaitAsync(token);

		return downloadedFiles;
	}

	public ModioService(IFileSystemService fileSystemService)
	{
		_fs = fileSystemService;

		this.WhenAnyValue(x => x._client).Select(x => x != null).ToPropertyEx(this, x => x.IsInitialized, deferSubscription: false);
		this.WhenAnyValue(x => x.IsInitialized, x => x.LimitExceeded).Select(x => x.Item1 && !x.Item2).ToPropertyEx(this, x => x.CanFetchData, deferSubscription: false);
	}
}
