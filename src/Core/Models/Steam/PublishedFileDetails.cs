using Newtonsoft.Json;

namespace ModManager.Models.Steam;

public enum EPublishedFileVisibility
{
	Public,
	FriendsOnly,
	Private
}

public struct WorkshopTag
{
	[JsonProperty("tag")]
	public string Tag { get; set; }
}

public class PublishedFileDetailsResponse
{
	[JsonProperty("response")]
	public PublishedFileDetailsResponseData Response { get; set; }
}

public class PublishedFileDetailsResponseData
{
	[JsonProperty("result")]
	public int Result { get; set; }

	[JsonProperty("resultcount")]
	public int ResultCount { get; set; }

	[JsonProperty("publishedfiledetails")]
	public List<PublishedFileDetails> PublishedFileDetails { get; set; }
}


public class PublishedFileDetails : IWorkshopPublishFileDetails
{
	[JsonProperty("publishedfileid")] public long PublishedFileId { get; set; }
	[JsonProperty("result")] public int Result { get; set; }
	[JsonProperty("creator")] public string Creator { get; set; }
	[JsonProperty("creator_app_id")] public int CreatorAppId { get; set; }
	[JsonProperty("consumer_app_id")] public int ConsumerAppId { get; set; }
	[JsonProperty("filename")] public string FileName { get; set; }
	[JsonProperty("file_size")] public string FileSize { get; set; }
	[JsonProperty("file_url")] public string FileUrl { get; set; }
	[JsonProperty("hcontent_file")] public string HContentFile { get; set; }
	[JsonProperty("preview_url")] public string PreviewUrl { get; set; }
	[JsonProperty("hcontent_preview")] public string HContentPreview { get; set; }
	[JsonProperty("title")] public string Title { get; set; }
	[JsonProperty("description")] public string Description { get; set; }
	[JsonProperty("time_created")] public long TimeCreated { get; set; }
	[JsonProperty("time_updated")] public long TimeUpdated { get; set; }
	[JsonProperty("visibility")] public EPublishedFileVisibility Visibility { get; set; }
	[JsonProperty("banned")] public bool Banned { get; set; }
	[JsonProperty("ban_reason")] public string BanReason { get; set; }
	[JsonProperty("subscriptions")] public int Subscriptions { get; set; }
	[JsonProperty("favorited")] public int Favorited { get; set; }
	[JsonProperty("lifetime_subscriptions")] public int LifetimeSubscriptions { get; set; }
	[JsonProperty("lifetime_favorited")] public int LifetimeFavorited { get; set; }
	[JsonProperty("views")] public int Views { get; set; }
	[JsonProperty("tags")] public List<WorkshopTag> Tags { get; set; }
}
