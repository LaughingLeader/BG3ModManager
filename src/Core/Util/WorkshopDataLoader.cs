﻿using ModManager.Models.Cache;
using ModManager.Models.Mod;
using ModManager.Models.Steam;

namespace ModManager.Util;

public static class WorkshopDataLoader
{
	private static readonly string STEAM_API_GET_WORKSHOP_DATA_URL = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/?";
	private static readonly string STEAM_API_GET_WORKSHOP_MODS_URL = "https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?";

#if !DOS2
	private static readonly List<string> ignoredTags = ["Add-on", "Adventure"];
#else
	private static readonly List<string> ignoredTags = ["Add-on", "Adventure", "GM", "Arena", "Story", "Definitive Edition"];
#endif

	private static List<string> GetWorkshopTags(IWorkshopPublishFileDetails data)
	{
		var tags = data.Tags.Where(t => !ignoredTags.Contains(t.Tag)).Select(x => x.Tag).ToList();
		if (tags != null)
		{
			return tags;
		}
		return [];
	}

	public static async Task<int> LoadAllWorkshopDataAsync(List<DivinityModData> workshopMods, SteamWorkshopCachedData cachedData, CancellationToken token)
	{
		if (workshopMods == null || workshopMods.Count == 0)
		{
			return 0;
		}
		//var workshopMods = mods.Where(x => !String.IsNullOrEmpty(x.WorkshopData.ID)).ToList();
		var values = new Dictionary<string, string>
		{
		{ "itemcount", workshopMods.Count.ToString()}
		};
		var i = 0;
		foreach (var mod in workshopMods)
		{
			values.Add($"publishedfileids[{i}]", mod.WorkshopData.ModId.ToString());
			i++;
		}

		DivinityApp.Log($"Updating workshop data for mods.");

		var responseData = "";
		try
		{
			var content = new FormUrlEncodedContent(values);
			var response = await WebHelper.PostAsync(STEAM_API_GET_WORKSHOP_DATA_URL, content, token);
			responseData = await response.Content.ReadAsStringAsync();
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error requesting Steam API to get workshop mod data:\n{ex}");
		}

		var totalLoaded = 0;

		if (!String.IsNullOrEmpty(responseData))
		{
			var pResponse = DivinityJsonUtils.SafeDeserialize<PublishedFileDetailsResponse>(responseData);
			if (pResponse != null && pResponse.Response != null && pResponse.Response.PublishedFileDetails != null && pResponse.Response.PublishedFileDetails.Count > 0)
			{
				var details = pResponse.Response.PublishedFileDetails;
				foreach (var d in details)
				{
					try
					{
						var mod = workshopMods.FirstOrDefault(x => x.WorkshopData.ModId == d.PublishedFileId);
						if (mod != null)
						{
							mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(d.TimeCreated);
							mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(d.TimeUpdated);
							if (d.Tags != null && d.Tags.Count > 0)
							{
								mod.WorkshopData.Tags = GetWorkshopTags(d);
								mod.AddTags(mod.WorkshopData.Tags);
							}
							//DivinityApp.LogMessage($"Loaded workshop details for mod {mod.Name}:");
							totalLoaded++;
						}
						cachedData.AddOrUpdate(mod.UUID, d, mod.WorkshopData.Tags);
					}
					catch (Exception ex)
					{
						DivinityApp.Log($"Error parsing mod data for {d.Title}({d.PublishedFileId})\n{ex}");
					}
				}

				DivinityApp.Log($"Successfully loaded workshop mod data.");
			}
			else
			{
				DivinityApp.Log("Failed to load workshop data for mods.");
				DivinityApp.Log($"{responseData}");
			}
		}
		else
		{
			DivinityApp.Log("Failed to load workshop data for mods - no response data.");
		}
		return totalLoaded;
	}

	public static async Task<bool> GetAllWorkshopDataAsync(SteamWorkshopCachedData cachedData, string appid, string apiKey, CancellationToken token)
	{
		if (String.IsNullOrEmpty(apiKey))
		{
			DivinityApp.Log($"Steam Web API key not set. Skipping.");
			return false;
		}
		DivinityApp.Log($"Attempting to get workshop data for mods missing workshop folders.");
		var totalFound = 0;

		var total = 1482;
		var page = 0;
		var maxPage = (total / 99) + 1;

		while (page < maxPage)
		{
			if (token.IsCancellationRequested) break;
			var url = $"https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?key={apiKey}&appid={appid}&return_short_description=true&numperpage=99&return_tags=true&return_metadata=true&excludedtags[0]=GM+Campaign&page={page}";
			var responseData = "";
			try
			{
				var response = await WebHelper.GetAsync(url, token);
				responseData = await response.Content.ReadAsStringAsync();
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error requesting Steam API to get workshop mod data:\n{ex}");
			}

			if (!String.IsNullOrEmpty(responseData)
				&& DivinityJsonUtils.TrySafeDeserialize<QueryFilesResponse>(responseData, out var pResponse))
			{
				if (pResponse.Response != null && pResponse.Response.PublishedFileDetails != null && pResponse.Response.PublishedFileDetails.Count > 0)
				{
					if (pResponse.Response.Total > total)
					{
						total = pResponse.Response.Total;
						maxPage = (total / 99) + 1;
					}
					var details = pResponse.Response.PublishedFileDetails;

					foreach (var d in details)
					{
						try
						{
							var uuid = d.GetGuid();
							if (!String.IsNullOrEmpty(uuid))
							{
								cachedData.AddOrUpdate(uuid, d, GetWorkshopTags(d));
								totalFound++;
							}
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error parsing mod data for {d.Title}({d.PublishedFileId})\n{ex}");
						}
					}
				}
				else
				{
					DivinityApp.Log($"Failed to get workshop data from {url}");
				}
			}
			else
			{
				DivinityApp.Log("Failed to load workshop data for mods - no response data.");
			}

			page++;
		}

		if (totalFound > 0)
		{
			DivinityApp.Log($"Cached workshop data for {totalFound} mods.");
			return true;
		}
		else
		{
			return false;
		}
	}

	public static async Task<int> FindWorkshopDataAsync(List<DivinityModData> mods, SteamWorkshopCachedData cachedData, string appid, string apiKey, CancellationToken token)
	{
		if (mods == null || mods.Count == 0)
		{
			DivinityApp.Log($"Skipping FindWorkshopDataAsync");
			return 0;
		}
		DivinityApp.Log($"Attempting to get workshop data for {mods.Count} mods.");
		var totalFound = 0;
		foreach (var mod in mods)
		{
			var name = Uri.EscapeDataString(mod.DisplayName);
			var url = $"https://api.steampowered.com/IPublishedFileService/QueryFiles/v1/?key={apiKey}&appid={appid}&search_text={name}&return_short_description=true&return_tags=true&numperpage=99&return_metadata=true&requiredtags[0]=Definitive+Edition";
			var responseData = "";
			try
			{
				var response = await WebHelper.GetAsync(url, token);
				responseData = await response.Content.ReadAsStringAsync();
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error requesting Steam API to get workshop mod data:\n{ex}");
			}

			//DivinityApp.LogMessage(responseData);
			if (!String.IsNullOrEmpty(responseData) && DivinityJsonUtils.TrySafeDeserialize<QueryFilesResponse>(responseData, out var pResponse))
			{
				if (pResponse.Response != null && pResponse.Response.PublishedFileDetails != null && pResponse.Response.PublishedFileDetails.Count > 0)
				{
					var details = pResponse.Response.PublishedFileDetails;

					foreach (var d in details)
					{
						try
						{
							var dUUID = d.GetGuid();
							if (!String.IsNullOrEmpty(dUUID))
							{
								var modTags = GetWorkshopTags(d);
								cachedData.AddOrUpdate(dUUID, d, modTags);

								if (dUUID == mod.UUID)
								{
									if (mod.WorkshopData.ModId <= DivinityApp.WORKSHOP_MOD_ID_START || mod.WorkshopData.ModId == d.PublishedFileId)
									{
										mod.WorkshopData.ModId = d.PublishedFileId;
										mod.WorkshopData.CreatedDate = DateUtils.UnixTimeStampToDateTime(d.TimeCreated);
										mod.WorkshopData.UpdatedDate = DateUtils.UnixTimeStampToDateTime(d.TimeUpdated);
										if (d.Tags != null && d.Tags.Count > 0)
										{
											mod.WorkshopData.Tags = modTags;
											mod.AddTags(mod.WorkshopData.Tags);
										}
										DivinityApp.Log($"Found workshop ID {mod.WorkshopData.ModId} for mod {mod.DisplayName}.");
										totalFound++;
									}
									else
									{
										DivinityApp.Log($"Found workshop entry for mod {mod.DisplayName}, but it has a different workshop ID? Current({mod.WorkshopData.ModId}) Found({d.PublishedFileId})");
									}
								}
							}
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error parsing mod data for {d.Title}({d.PublishedFileId})\n{ex}");
						}
					}
				}
				else
				{
					DivinityApp.Log($"Failed to find workshop data for mod {mod.DisplayName}");
					if (!cachedData.NonWorkshopMods.Contains(mod.UUID))
					{
						cachedData.NonWorkshopMods.Add(mod.UUID);
						cachedData.CacheUpdated = true;
					}
				}
			}
			else
			{
				DivinityApp.Log("Failed to load workshop data for mods - no response data.");
			}
		}

		if (totalFound > 0)
		{
			DivinityApp.Log($"Successfully loaded workshop data for {totalFound} mods.");
		}
		else
		{
			DivinityApp.Log($"Failed to find workshop data for {mods.Count} mods (they're probably not on the workshop).");
		}

		return totalFound;
	}
}
