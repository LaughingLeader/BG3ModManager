using ModManager.Models.Steam;

using System.Runtime.Serialization;

namespace ModManager.Models.Cache;

[DataContract]
public class SteamWorkshopCachedData : BaseModCacheData<DivinityModWorkshopCachedData>
{
	[DataMember] public List<string> NonWorkshopMods { get; set; }

	public void AddOrUpdate(string uuid, IWorkshopPublishFileDetails d, List<string> tags)
	{
		// Mods may have the same UUID, so use the WorkshopID instead.
		var cachedData = Mods.Values.FirstOrDefault(x => x.ModId == d.PublishedFileId);
		if (cachedData != null)
		{
			cachedData.LastUpdated = d.TimeUpdated;
			cachedData.Created = d.TimeCreated;
			cachedData.Tags = tags;
		}
		else
		{
			Mods.Add(uuid, new DivinityModWorkshopCachedData()
			{
				Created = d.TimeCreated,
				LastUpdated = d.TimeUpdated,
				UUID = uuid,
				ModId = d.PublishedFileId,
				Tags = tags
			});
		}
		NonWorkshopMods.Remove(uuid);
		CacheUpdated = true;
	}

	public void AddNonWorkshopMod(string uuid)
	{
		if (!NonWorkshopMods.Any(x => x == uuid))
		{
			NonWorkshopMods.Add(uuid);
		}
		CacheUpdated = true;
	}

	public SteamWorkshopCachedData() : base()
	{
		NonWorkshopMods = [];
	}
}
