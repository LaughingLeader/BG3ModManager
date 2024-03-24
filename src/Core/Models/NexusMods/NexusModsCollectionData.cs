using ModManager.Util;

using DynamicData;

using NexusModsNET.DataModels.GraphQL.Types;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ModManager.Models.NexusMods;

public class NexusModsCollectionData : ReactiveObject
{
	[Reactive] public bool HasAdultContent { get; set; }
	[Reactive] public string Name { get; set; }
	[Reactive] public string Author { get; set; }
	[Reactive] public string Description { get; set; }
	[Reactive] public Uri AuthorAvatarUrl { get; set; }
	[Reactive] public Uri TileImageUrl { get; set; }
	[Reactive] public Uri TileImageThumbnailUrl { get; set; }
	[Reactive] public DateTimeOffset CreatedAt { get; set; }
	[Reactive] public DateTimeOffset UpdatedAt { get; set; }

	public SourceList<NexusModsCollectionModData> Mods { get; }

	public NexusModsCollectionData()
	{
		Mods = new();
	}

	private static NexusModsCollectionModData ModFileToReactiveData(int index, NexusGraphCollectionRevisionMod mod)
	{
		return new NexusModsCollectionModData(mod)
		{
			Index = index
		};
	}

	public static NexusModsCollectionData FromCollectionRevision(NexusGraphCollectionRevision collectionRevision)
	{
		var collection = collectionRevision.Collection;
		var data = new NexusModsCollectionData()
		{
			HasAdultContent = collectionRevision.AdultContent,
			Name = collection.Name,
			Description = collection.Summary,
			Author = collection.User.Name,
			AuthorAvatarUrl = new Uri(collection.User?.Avatar),
			TileImageUrl = StringUtils.StringToUri(collection.TileImage?.Url),
			TileImageThumbnailUrl = StringUtils.StringToUri(collection.TileImage?.ThumbnailUrl),
			CreatedAt = collectionRevision.CreatedAt,
			UpdatedAt = collectionRevision.UpdatedAt
		};
		var mods = Enumerable.Range(0, collectionRevision.ModFiles.Length).Select(i => ModFileToReactiveData(i, collectionRevision.ModFiles[i]));
		data.Mods.AddRange(mods);

		return data;
	}
}
