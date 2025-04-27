using ModManager.Models.Cache;
using ModManager.Models.Mod;

namespace ModManager.ModUpdater.Cache;

public interface IExternalModCacheHandler<T> where T : IModCacheData
{
	ModSourceType SourceType { get; }
	string FileName { get; }
	JsonSerializerOptions SerializerSettings { get; }

	bool IsEnabled { get; set; }
	T CacheData { get; set; }
	void OnCacheUpdated(T cachedData);
	Task<bool> Update(IEnumerable<ModData> mods, CancellationToken token);
}