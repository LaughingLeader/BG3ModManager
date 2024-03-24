using System.Runtime.Serialization;

namespace ModManager.Models.Cache;

public interface IModCacheData
{
	long LastUpdated { get; set; }
	string LastVersion { get; set; }
	bool CacheUpdated { get; set; }
}

[DataContract]
public class BaseModCacheData<T> : IModCacheData
{
	[DataMember] public long LastUpdated { get; set; }
	[DataMember] public string LastVersion { get; set; }
	[DataMember] public Dictionary<string, T> Mods { get; set; } = new Dictionary<string, T>();

	public bool CacheUpdated { get; set; }

	public BaseModCacheData()
	{
		LastUpdated = -1;
		LastVersion = "";

		Mods = new Dictionary<string, T>();
	}
}
