namespace ModManager.Models.Steam;

public enum EPublishedFileVisibility
{
	Public,
	FriendsOnly,
	Private
}

public struct WorkshopTag
{
	[JsonPropertyName("tag")]
	public string? Tag { get; set; }
}

public class PublishedFileDetailsResponse
{
	[JsonPropertyName("response")]
	public PublishedFileDetailsResponseData? Response { get; set; }
}

public class PublishedFileDetailsResponseData
{
	[JsonPropertyName("result")]
	public int Result { get; set; }

	[JsonPropertyName("resultcount")]
	public int ResultCount { get; set; }

	[JsonPropertyName("publishedfiledetails")]
	public List<PublishedFileDetails>? PublishedFileDetails { get; set; }
}

public class PublishedFileDetails : IWorkshopPublishFileDetails
{
	[JsonPropertyName("publishedfileid")] public long PublishedFileId { get; set; }
	[JsonPropertyName("result")] public int Result { get; set; }
	[JsonPropertyName("creator")] public string? Creator { get; set; }
	[JsonPropertyName("creator_app_id")] public int CreatorAppId { get; set; }
	[JsonPropertyName("consumer_app_id")] public int ConsumerAppId { get; set; }
	[JsonPropertyName("filename")] public string? FileName { get; set; }
	[JsonPropertyName("file_size")] public string? FileSize { get; set; }
	[JsonPropertyName("file_url")] public string? FileUrl { get; set; }
	[JsonPropertyName("hcontent_file")] public string? HContentFile { get; set; }
	[JsonPropertyName("preview_url")] public string? PreviewUrl { get; set; }
	[JsonPropertyName("hcontent_preview")] public string? HContentPreview { get; set; }
	[JsonPropertyName("title")] public string? Title { get; set; }
	[JsonPropertyName("description")] public string? Description { get; set; }
	[JsonPropertyName("time_created")] public long TimeCreated { get; set; }
	[JsonPropertyName("time_updated")] public long TimeUpdated { get; set; }
	[JsonPropertyName("visibility")] public EPublishedFileVisibility Visibility { get; set; }
	[JsonPropertyName("banned")] public bool Banned { get; set; }
	[JsonPropertyName("ban_reason")] public string? BanReason { get; set; }
	[JsonPropertyName("subscriptions")] public int Subscriptions { get; set; }
	[JsonPropertyName("favorited")] public int Favorited { get; set; }
	[JsonPropertyName("lifetime_subscriptions")] public int LifetimeSubscriptions { get; set; }
	[JsonPropertyName("lifetime_favorited")] public int LifetimeFavorited { get; set; }
	[JsonPropertyName("views")] public int Views { get; set; }
	[JsonPropertyName("tags")] public List<WorkshopTag>? Tags { get; set; }
}