using ModManager.Util;

using NexusModsNET.DataModels.GraphQL.Types;

namespace ModManager.Models.NexusMods;

public class NexusModsBaseCollectionData : ReactiveObject
{
	[Reactive] public bool HasAdultContent { get; set; }
	[Reactive] public string? Name { get; set; }
	[Reactive] public string? Author { get; set; }
	[Reactive] public string? Description { get; set; }
	[Reactive] public Uri? AuthorAvatarUrl { get; set; }
	[Reactive] public Uri? TileImageUrl { get; set; }
	[Reactive] public Uri? TileImageThumbnailUrl { get; set; }
	[Reactive] public DateTimeOffset CreatedAt { get; set; }
	[Reactive] public DateTimeOffset UpdatedAt { get; set; }

	public NexusModsBaseCollectionData() { }

	public NexusModsBaseCollectionData(NexusGraphCollectionRevision collectionRevision)
	{
		var collection = collectionRevision.Collection;

		HasAdultContent = collectionRevision.AdultContent;
		Name = collection.Name;
		Description = collection.Summary;
		Author = collection.User.Name;
		if(collection.User?.Avatar != null) AuthorAvatarUrl = new Uri(collection.User.Avatar);
		TileImageUrl = StringUtils.StringToUri(collection.TileImage?.Url);
		TileImageThumbnailUrl = StringUtils.StringToUri(collection.TileImage?.ThumbnailUrl);
		CreatedAt = collectionRevision.CreatedAt;
		UpdatedAt = collectionRevision.UpdatedAt;
	}
}
