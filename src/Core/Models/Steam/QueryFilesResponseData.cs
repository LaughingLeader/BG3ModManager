using Newtonsoft.Json;

namespace ModManager.Models.Steam;

public class QueryFilesResponse
{
	[JsonProperty("response")] public QueryFilesResponseData Response { get; set; }
}

public class QueryFilesResponseData
{
	[JsonProperty("total")] public int Total { get; set; }

	[JsonProperty("publishedfiledetails")] public List<QueryFilesPublishedFileDetails> PublishedFileDetails { get; set; }
}

public class QueryFilesPublishedFileDetails : IWorkshopPublishFileDetails
{
	[JsonProperty("result")] public int Result { get; set; }
	[JsonProperty("publishedfileid")] public long PublishedFileId { get; set; }
	[JsonProperty("creator")] public string Creator { get; set; }
	[JsonProperty("filename")] public string FileName { get; set; }
	[JsonProperty("file_size")] public string FileSize { get; set; }
	[JsonProperty("file_url")] public string FileUrl { get; set; }
	[JsonProperty("preview_url")] public string PreviewUrl { get; set; }
	[JsonProperty("url")] public string Url { get; set; }
	[JsonProperty("title")] public string Title { get; set; }
	[JsonProperty("description")] public string Description { get; set; }
	[JsonProperty("timecreated")] public long TimeCreated { get; set; }
	[JsonProperty("timeupdated")] public long TimeUpdated { get; set; }
	[JsonProperty("visibility")] public EPublishedFileVisibility Visibility { get; set; }
	[JsonProperty("flags")] public int Flags { get; set; }
	[JsonProperty("tags")] public List<WorkshopTag> Tags { get; set; }
	[JsonProperty("metadata")] public QueryFilesPublishedFileDivinityMetadataMain Metadata { get; set; }
	[JsonProperty("language")] public int Language { get; set; }
	[JsonProperty("revision_change_number")] public string RevisionChangeNumber { get; set; }
	[JsonProperty("revision")] public int Revision { get; set; }

	public string GetGuid()
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
	[JsonProperty("root")] public QueryFilesPublishedFileDivinityMetadataRoot Root { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataRoot
{
	[JsonProperty("header")] public QueryFilesPublishedFileDivinityMetadataHeader Header { get; set; }
	[JsonProperty("regions")] public QueryFilesPublishedFileDivinityMetadataRegions Regions { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataHeader
{
	[JsonProperty("time")] public int Time { get; set; }
	[JsonProperty("version")] public string Version { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataRegions
{
	[JsonProperty("metadata")] public QueryFilesPublishedFileDivinityMetadataEntry Metadata { get; set; }
}

public class QueryFilesPublishedFileDivinityMetadataEntry
{
	[JsonProperty("guid")] public QueryFilesPublishedFileDivinityMetadataEntryAttribute<string> Guid { get; set; }
	[JsonProperty("type")] public QueryFilesPublishedFileDivinityMetadataEntryAttribute<int> Type { get; set; }
	[JsonProperty("Version")] public QueryFilesPublishedFileDivinityMetadataEntryAttribute<int> Version { get; set; }
}
public class QueryFilesPublishedFileDivinityMetadataEntryAttribute<T>
{
	[JsonProperty("type")] public int Type { get; set; }
	[JsonProperty("value")] public T Value { get; set; }
}
