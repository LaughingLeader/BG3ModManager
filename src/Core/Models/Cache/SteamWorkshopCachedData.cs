using ModManager.Models.Steam;

using Newtonsoft.Json;

using System.Runtime.Serialization;
using System.Text;

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

	public string Serialize()
	{
		StringBuilder sb = new();
		StringWriter sw = new(sb);

		using (JsonWriter writer = new JsonTextWriter(sw))
		{
			try
			{
				writer.WriteStartObject();

				writer.WritePropertyName("LastUpdated");
				writer.WriteValue(LastUpdated);

				writer.WritePropertyName("LastVersion");
				writer.WriteValue(LastVersion);

				writer.WritePropertyName("Mods");
				writer.WriteStartArray();

				foreach (var data in Mods.Values)
				{
					writer.WriteStartObject();

					writer.WritePropertyName("UUID");
					writer.WriteValue(data.UUID);

					writer.WritePropertyName("ModId");
					writer.WriteValue(data.ModId);

					//writer.WritePropertyName("Created");
					//writer.WriteValue(data.Created);

					writer.WritePropertyName("LastUpdated");
					writer.WriteValue(data.LastUpdated);

					writer.WritePropertyName("Tags");
					writer.WriteStartArray();

					if (data.Tags != null && data.Tags.Count > 0)
					{
						foreach (var tag in data.Tags)
						{
							writer.WriteValue(tag);
						}
					}

					writer.WriteEndArray();

					writer.WriteEndObject();
				}

				writer.WriteEndArray();

				writer.WritePropertyName("NonWorkshopMods");
				writer.WriteStartArray();
				foreach (var uuid in NonWorkshopMods)
				{
					writer.WriteValue(uuid);
				}
				writer.WriteEndArray();

				writer.WriteEndObject();
			}
			catch (Exception ex)
			{
				DivinityApp.Log("Error serializing cache:");
				DivinityApp.Log(ex.ToString());
			}
		}

		return sb.ToString();
	}

	public SteamWorkshopCachedData() : base()
	{
		NonWorkshopMods = [];
	}
}
