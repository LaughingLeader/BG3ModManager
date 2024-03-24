using ModManager.Util;

using NexusModsNET.DataModels.GraphQL.Types;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Linq;
using System.Windows;

namespace ModManager.Models.NexusMods;

public class NexusModsCollectionModData : ReactiveObject
{
	public NexusGraphModFile ModFileData { get; }

	[Reactive] public int Index { get; set; }
	[Reactive] public string Name { get; set; }
	[Reactive] public string Author { get; set; }
	[Reactive] public string Summary { get; set; }
	[Reactive] public string Description { get; set; }
	[Reactive] public string Version { get; set; }
	[Reactive] public string Category { get; set; }
	[Reactive] public long SizeInBytes { get; set; }
	[Reactive] public Uri AuthorAvatarUrl { get; set; }
	[Reactive] public Uri ImageUrl { get; set; }
	[Reactive] public DateTimeOffset CreatedAt { get; set; }
	[Reactive] public DateTimeOffset UpdatedAt { get; set; }
	[Reactive] public bool IsOptional { get; set; }

	//UI-related properties
	[Reactive] public bool IsSelected { get; set; }

	[ObservableAsProperty] public string SizeText { get; }
	[ObservableAsProperty] public string AuthorDisplayText { get; }
	[ObservableAsProperty] public string CreatedDateText { get; }
	[ObservableAsProperty] public string UpdatedDateText { get; }
	public string NexusModsURL { get; }
	public string NexusModsURLDisplayText { get; }
	[ObservableAsProperty] public Visibility DescriptionVisibility { get; }
	[ObservableAsProperty] public Visibility AuthorAvatarVisibility { get; }
	[ObservableAsProperty] public Visibility ImageVisibility { get; }


	public NexusModsCollectionModData(NexusGraphCollectionRevisionMod mod)
	{
		IsSelected = true;
		var modFile = mod.File;
		ModFileData = modFile;

		Name = ModFileData.Name;
		Summary = ModFileData.Mod.Summary;
		Description = ModFileData.Description;
		Author = ModFileData.Owner?.Name;
		AuthorAvatarUrl = new Uri(ModFileData.Owner?.Avatar);
		ImageUrl = StringUtils.StringToUri(ModFileData.Mod.PictureUrl);
		CreatedAt = ModFileData.Mod.CreatedAt;
		UpdatedAt = ModFileData.Mod.UpdatedAt;
		Version = ModFileData.Mod.Version;
		SizeInBytes = ModFileData.SizeInBytes;
		Category = ModFileData.Mod.Category;
		IsOptional = mod.Optional;

		NexusModsURL = $"https://www.nexusmods.com/{DivinityApp.NEXUSMODS_GAME_DOMAIN}/mods/{modFile.ModId}";
		NexusModsURLDisplayText = $"/{DivinityApp.NEXUSMODS_GAME_DOMAIN}/mods/{modFile.ModId}";

		this.WhenAnyValue(x => x.SizeInBytes).Select(StringUtils.BytesToString).ToUIProperty(this, x => x.SizeText);
		this.WhenAnyValue(x => x.Author).Select(x => $"Created by {x}").ToUIProperty(this, x => x.AuthorDisplayText);

		this.WhenAnyValue(x => x.Description).Select(PropertyConverters.StringToVisibility).ToUIProperty(this, x => x.DescriptionVisibility);
		this.WhenAnyValue(x => x.ImageUrl).Select(PropertyConverters.UriToVisibility).ToUIProperty(this, x => x.ImageVisibility);
		this.WhenAnyValue(x => x.AuthorAvatarUrl).Select(PropertyConverters.UriToVisibility).ToUIProperty(this, x => x.AuthorAvatarVisibility);

		this.WhenAnyValue(x => x.CreatedAt).Select(PropertyConverters.DateToString).ToUIProperty(this, x => x.CreatedDateText);
		this.WhenAnyValue(x => x.UpdatedAt).Select(PropertyConverters.DateToString).ToUIProperty(this, x => x.UpdatedDateText);
	}
}
