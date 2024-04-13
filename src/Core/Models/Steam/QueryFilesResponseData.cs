namespace ModManager.Models.Steam;

public class QueryFilesResponse
{
	[JsonPropertyName("response")] public QueryFilesResponseData? Response { get; set; }
}

public class QueryFilesResponseData
{
	[JsonPropertyName("total")] public int Total { get; set; }

	[JsonPropertyName("publishedfiledetails")] public List<QueryFilesPublishedFileDetails>? PublishedFileDetails { get; set; }
}

public class QueryFilesPublishedFileDetails : IWorkshopPublishFileDetails
{
	[JsonPropertyName("result")] public int Result { get; set; }
	[JsonPropertyName("publishedfileid")] public long PublishedFileId { get; set; }
	[JsonPropertyName("creator")] public string? Creator { get; set; }
	[JsonPropertyName("filename")] public string? FileName { get; set; }
	[JsonPropertyName("file_size")] public string? FileSize { get; set; }
	[JsonPropertyName("file_url")] public string? FileUrl { get; set; }
	[JsonPropertyName("preview_url")] public string? PreviewUrl { get; set; }
	[JsonPropertyName("url")] public string? Url { get; set; }
	[JsonPropertyName("title")] public string? Title { get; set; }
	[JsonPropertyName("description")] public string? Description { get; set; }
	[JsonPropertyName("timecreated")] public long TimeCreated { get; set; }
	[JsonPropertyName("timeupdated")] public long TimeUpdated { get; set; }
	[JsonPropertyName("visibility")] public EPublishedFileVisibility Visibility { get; set; }
	[JsonPropertyName("flags")] public int Flags { get; set; }
	[JsonPropertyName("tags")] public List<WorkshopTag>? Tags { get; set; }
	[JsonPropertyName("metadata")] public QueryFilesPublishedFileDivinityMetadataMain? Metadata { get; set; }
	[JsonPropertyName("language")] public int Language { get; set; }
	[JsonPropertyName("revision_change_number")] public string? RevisionChangeNumber { get; set; }
	[JsonPropertyName("revision")] public int Revision { get; set; }

	public string? GetGuid()
	{
		if (this.Metadata != null)
		{
			try
			{
				return this.Metadata.Root.Regions.Metadata.Guid.Value;
			}
			catch
			{
			}
		}
		return null;
	}
}

public class QueryFilesPublishedFileDivinityMetadataMain
{
	[JsonPropertyName("root")] public QueryFilesPublishedFileDivinityMetadataRoot? Root { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataRoot
{
	[JsonPropertyName("header")] public QueryFilesPublishedFileDivinityMetadataHeader? Header { get; set; }
	[JsonPropertyName("regions")] public QueryFilesPublishedFileDivinityMetadataRegions? Regions { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataHeader
{
	[JsonPropertyName("time")] public int Time { get; set; }
	[JsonPropertyName("version")] public string? Version { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataRegions
{
	[JsonPropertyName("metadata")] public QueryFilesPublishedFileDivinityMetadataEntry? Metadata { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataEntry
{
	[JsonPropertyName("guid")] public QueryFilesPublishedFileDivinityMetadataEntryAttribute<string>? Guid { get; set; }
	[JsonPropertyName("type")] public QueryFilesPublishedFileDivinityMetadataEntryAttribute<int>? Type { get; set; }
	[JsonPropertyName("Version")] public QueryFilesPublishedFileDivinityMetadataEntryAttribute<int>? Version { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataEntryAttribute<T>
{
	[JsonPropertyName("type")] public int Type { get; set; }
	[JsonPropertyName("value")] public T? Value { get; set; }
}
