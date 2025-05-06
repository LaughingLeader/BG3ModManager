using Modio;
using Modio.Filters;
using Modio.Models;

using ModManager.Models.Mod;
using ModManager.Models.Updates;

using ReactiveUI;

using System.Text.Json;

namespace ModManager.Services;
public class ModioService : ReactiveObject, IModioService
{
	private readonly IFileSystemService _fs;

	private readonly Client _client;

	private const int GAME_ID = 6715;

	public string ApiKey { get; set; }
	public bool IsInitialized { get; }
	public bool LimitExceeded { get; }
	public bool CanFetchData { get; }
	public Uri ProfileAvatarUrl { get; }

	public async Task<UpdateResult> FetchModInfoAsync(IEnumerable<ModData> mods, CancellationToken token)
	{
		//var filter = Filter.WithLimit(10).Offset(10);
		//var modsResult = await _client.Games[GAME_ID].Mods.Search(filter).ToList();
		//TODO - Use actual data fetching
		var modDataText = _fs.File.ReadAllText(DivinityApp.GetAppDirectory("TEST\\BG3_modio_mods.json"));
		var cachedModData = JsonSerializer.Deserialize<Result<Mod>>(modDataText);
		var updateResult = new UpdateResult();
		if (cachedModData?.Data != null)
		{
			var modDict = mods.ToDictionary(x => x.PublishHandle);
			updateResult.Success = true;
			foreach (var result in cachedModData.Data)
			{
				if(modDict.TryGetValue(result.Id, out var existingMod))
				{
					existingMod.ModioData.Update(result);
					updateResult.UpdatedMods.Add(existingMod);
				}
			}
		}

		return updateResult;
	}

	public async Task<Dictionary<string, Download>> GetLatestDownloadsForModsAsync(IEnumerable<ModData> mods, CancellationToken token)
	{
		await FetchModInfoAsync(mods, token);
		throw new NotImplementedException();
	}

	public ModioService(IFileSystemService fileSystemService)
	{
		_fs = fileSystemService;

		//_client = new Client(new Credentials(ApiKey));
	}
}
