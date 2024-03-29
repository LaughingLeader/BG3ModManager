using DynamicData.Binding;

using ModManager.Models.NexusMods;
using ModManager.Util;

using NexusModsNET.DataModels.GraphQL.Types;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace ModManager.ViewModels;

public class CollectionDownloadWindowViewModel : BaseWindowViewModel
{
	[Reactive] public NexusModsCollectionData Data { get; private set; }
	[Reactive] public bool IsCardView { get; set; }

	public ObservableCollectionExtended<NexusModsCollectionModData> Mods { get; }

	[ObservableAsProperty] public string Title { get; }
	[ObservableAsProperty] public Visibility AuthorAvatarVisibility { get; }
	[ObservableAsProperty] public BitmapImage AuthorAvatar { get; }
	[ObservableAsProperty] public Visibility GridViewVisibility { get; }
	[ObservableAsProperty] public Visibility CardViewVisibility { get; }

	public ICommand SelectAllCommand { get; }
	public ICommand SetGridViewCommand { get; }
	public ICommand SetCardViewCommand { get; }
	public ICommand EnableAllCommand { get; }
	public ICommand DisableAllCommand { get; }
	public ICommand ConfirmCommand { get; set; }
	public ICommand CancelCommand { get; set; }

	public void Load(NexusGraphCollectionRevision collectionRevision)
	{
		Data = NexusModsCollectionData.FromCollectionRevision(collectionRevision);

		Mods.Clear();

		if (Data?.Mods?.Count > 0)
		{
			Mods.AddRange(Data.Mods.Items);
		}

		this.RaisePropertyChanged("Mods");
	}

	private static string ToTitleText(ValueTuple<string, string> x)
	{
		var text = x.Item1;
		var author = x.Item2;
		if (!String.IsNullOrEmpty(author))
		{
			text += " by " + author;
		}
		return text;
	}

	private void SelectAll(bool b)
	{
		foreach (var mod in Mods)
		{
			mod.IsSelected = b;
		}
	}

	public CollectionDownloadWindowViewModel()
	{
		Mods = [];

		this.WhenAnyValue(x => x.Data.Name, x => x.Data.Author).Select(ToTitleText).ToUIProperty(this, x => x.Title);

		var whenAvatar = this.WhenAnyValue(x => x.Data.AuthorAvatarUrl);
		whenAvatar.Select(x => x != null ? Visibility.Visible : Visibility.Collapsed).ToUIProperty(this, x => x.AuthorAvatarVisibility);
		whenAvatar.Select(PropertyHelpers.UriToImage).ToUIProperty(this, x => x.AuthorAvatar);

		this.WhenAnyValue(x => x.IsCardView).Select(PropertyConverters.BoolToVisibilityReversed).ToUIProperty(this, x => x.GridViewVisibility, Visibility.Visible);
		this.WhenAnyValue(x => x.IsCardView).Select(PropertyConverters.BoolToVisibility).ToUIProperty(this, x => x.CardViewVisibility, Visibility.Collapsed);

		SelectAllCommand = ReactiveCommand.Create<bool>(b =>
		{
			foreach (var mod in Data.Mods.Items)
			{
				mod.IsSelected = b;
			}
		});

		SetGridViewCommand = ReactiveCommand.Create(() => IsCardView = false);
		SetCardViewCommand = ReactiveCommand.Create(() => IsCardView = true);
		EnableAllCommand = ReactiveCommand.Create(() => SelectAll(true));
		DisableAllCommand = ReactiveCommand.Create(() => SelectAll(false));
	}
}

public class CollectionDownloadWindowDesignViewModel : CollectionDownloadWindowViewModel
{
	public CollectionDownloadWindowDesignViewModel() : base()
	{
		var mod1 = new NexusGraphMod()
		{
			PictureUrl = "https://staticdelivery.nexusmods.com/mods/3474/images/746/746-1691682009-457832810.png",
			CreatedAt = DateTimeOffset.Now,
			UpdatedAt = DateTimeOffset.Now,
			Version = "1.13",
			Category = "Gameplay",
			ModId = 746
		};
		var mod2 = new NexusGraphMod()
		{
			PictureUrl = "https://staticdelivery.nexusmods.com/mods/3474/images/956/956-1692067257-2128087246.png",
			CreatedAt = DateTimeOffset.Now,
			UpdatedAt = DateTimeOffset.Now,
			Version = "1.0.0.1",
			Category = "Gameplay"
		};
		var mod3 = new NexusGraphMod()
		{
			PictureUrl = "https://staticdelivery.nexusmods.com/mods/3474/images/522/522-1691008217-392937994.jpeg",
			CreatedAt = DateTimeOffset.Now,
			UpdatedAt = DateTimeOffset.Now,
			Version = "3.0",
			Category = "Gameplay"
		};
		var user = new NexusGraphUser()
		{
			Name = "LaughingLeader",
			Avatar = "https://avatars.nexusmods.com/8743560/100",
		};
		var designData = new NexusGraphCollectionRevision()
		{
			AdultContent = false,
			CreatedAt = DateTimeOffset.Now,
			UpdatedAt = DateTimeOffset.Now,
			Collection = new NexusGraphCollection()
			{
				Name = "Test Collection",
				Summary = "A collection of various mods",
				TileImage = new NexusGraphCollectionImage()
				{
					Url = "https://media.nexusmods.com/d/0/d01c8b3d-4849-457f-8754-71ce4ee27b8b.webp",
					ThumbnailUrl = "https://media.nexusmods.com/d/0/t/small/d01c8b3d-4849-457f-8754-71ce4ee27b8b.webp"
				},
				User = user
			},
			ModFiles = new NexusGraphCollectionRevisionMod[3] {
				new(){ Optional = true, File = new NexusGraphModFile(){ SizeInBytes = 66920, Name = "Companion AI", Description = "This mod will give you 10 AI for you to choose from for any class or any role your companions may have. there are also 3 blank customizable AI for you to edit it yourself, just like in Baldur's Gate 1&amp;2 in case you want to.", Owner = new NexusGraphUser(){ Name = "TPadvance", Avatar = "https://avatars.nexusmods.com/3054620/100" }, Mod = mod1}},
				new(){ File = new NexusGraphModFile(){ SizeInBytes = 2773, Name = "Extra Warlock Spell Slots", Description = "This mod adds additional spell slots to the Warlock class.", Owner = new NexusGraphUser(){ Name = "Some1ellse", Avatar = "https://avatars.nexusmods.com/8049857/100" }, Mod = mod2}},
				new(){ File = new NexusGraphModFile(){ SizeInBytes = 1302, Name = "Carry Weight Increased - Up To Over 9000", Description = "Get ready for your extensive loot hoarding with plenty of options to bolster your carry weight limit. Ranges from a minor x1.5 increase all the way up to the quite legendary x9000!", Owner = new NexusGraphUser(){ Name = "Mharius", Avatar = "https://avatars.nexusmods.com/14200939/100" }, Mod = mod3}},
			}
		};
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			Load(designData);
			DivinityApp.Log($"Mods: {Mods.Count} / {Data?.Mods?.Count}");
		});
	}
}
