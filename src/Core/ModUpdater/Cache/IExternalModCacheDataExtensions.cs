using ModManager.Models.Cache;
using ModManager.Util;

using System.Text;

namespace ModManager.ModUpdater.Cache;
public static class IExternalModCacheDataExtensions
{
	public static async Task<T> LoadCacheAsync<T>(this IExternalModCacheHandler<T> handler, string currentAppVersion, CancellationToken token) where T : IModCacheData
	{
		var filePath = DivinityApp.GetAppDirectory("Data", handler.FileName);

		if (File.Exists(filePath))
		{
			var cachedData = await JsonUtils.DeserializeFromPathAsync<T>(filePath, token);
			if (cachedData != null)
			{
				if (string.IsNullOrEmpty(cachedData.LastVersion) || cachedData.LastVersion != currentAppVersion)
				{
					cachedData.LastUpdated = -1;
				}
				cachedData.CacheUpdated = true;
				handler.OnCacheUpdated(cachedData);
				return cachedData;
			}
		}
		return default;
	}

	public static async Task<bool> SaveCacheAsync<T>(this IExternalModCacheHandler<T> handler, bool updateLastTimestamp, string currentAppVersion, CancellationToken token) where T : IModCacheData
	{
		try
		{
			var parentDir = DivinityApp.GetAppDirectory("Data");
			var filePath = Path.Join(parentDir, handler.FileName);
			if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

			if (updateLastTimestamp)
			{
				handler.CacheData.LastUpdated = DateTimeOffset.Now.ToUnixTimeSeconds();
			}
			handler.CacheData.LastVersion = currentAppVersion;

			var contents = JsonSerializer.Serialize(handler.CacheData, handler.SerializerSettings);

			var buffer = Encoding.UTF8.GetBytes(contents);
			using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length, true);
			await fs.WriteAsync(buffer, token);

			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error saving cache:\n{ex}");
		}
		return false;
	}

	public static bool DeleteCache<T>(this IExternalModCacheHandler<T> handler, bool permanent = false) where T : IModCacheData
	{
		try
		{
			var parentDir = DivinityApp.GetAppDirectory("Data");
			var filePath = Path.Join(parentDir, handler.FileName);
			if (File.Exists(filePath))
			{
				RecycleBinHelper.DeleteFile(filePath, false, permanent);
				return true;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error saving cache:\n{ex}");
		}
		return false;
	}
}