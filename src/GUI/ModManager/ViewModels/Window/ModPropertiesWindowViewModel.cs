﻿using DynamicData;

using ModManager.Models.Mod;
using ModManager.Util;

using System.ComponentModel;

namespace ModManager.ViewModels;

public class ModPropertiesWindowViewModel : ReactiveObject
{
	[Reactive] public string? Title { get; set; }
	[Reactive] public bool IsVisible { get; set; }
	[Reactive] public bool Locked { get; set; }
	[Reactive] public bool HasChanges { get; private set; }
	[Reactive] public DivinityModData? Mod { get; set; }
	[Reactive] public string? Notes { get; set; }
	[Reactive] public string? GitHub { get; set; }
	[Reactive] public long NexusModsId { get; set; }
	[Reactive] public string? ModioId { get; set; }

	[ObservableAsProperty] public string? ModFileName { get; }
	[ObservableAsProperty] public string? ModName { get; }
	[ObservableAsProperty] public string? ModDescription { get; }
	[ObservableAsProperty] public string? ModType { get; }
	[ObservableAsProperty] public string? ModSizeText { get; }
	[ObservableAsProperty] public string? ModFilePath { get; }
	[ObservableAsProperty] public bool IsEditorMod { get; }
	[ObservableAsProperty] public bool GitHubPlaceholderLabelVisibility { get; }

	public RxCommandUnit OKCommand { get; }
	public RxCommandUnit CancelCommand { get; }
	public RxCommandUnit ApplyCommand { get; }

	public long NexusModsIDMinimum => DivinityApp.NEXUSMODS_MOD_ID_START;

	public void SetMod(DivinityModData mod)
	{
		Mod = mod;
		HasChanges = false;
	}

	private void LoadConfigProperties(DivinityModData mod)
	{
		Locked = true;
		//var disp = this.SuppressChangeNotifications();
		if (mod != null)
		{
			if (mod.ModManagerConfig != null && mod.ModManagerConfig.IsLoaded)
			{
				GitHub = mod.ModManagerConfig.GitHub;
				NexusModsId = mod.ModManagerConfig.NexusModsId;
				ModioId = mod.ModManagerConfig.ModioId;
				Notes = mod.ModManagerConfig.Notes;
			}
			else
			{
				GitHub = mod.GitHubData.Url;
				NexusModsId = mod.NexusModsData.ModId;
				ModioId = mod.ModioData.ModId;
				Notes = "";
			}
		}
		Locked = HasChanges = false;
		//disp.Dispose();
	}

	public void Apply()
	{
		if (Mod?.ModManagerConfig == null) throw new NullReferenceException($"ModManagerConfig is null for mod ({Mod})");
		var modConfigService = AppServices.Get<ISettingsService>().ModConfig;

		if (String.IsNullOrEmpty(Mod.ModManagerConfig.Id)) Mod.ModManagerConfig.Id = Mod.UUID;

		modConfigService.Mods.AddOrUpdate(Mod.ModManagerConfig);

		Mod.ModManagerConfig.GitHub = GitHub;
		Mod.ModManagerConfig.NexusModsId = NexusModsId;
		Mod.ModManagerConfig.ModioId = ModioId;
		Mod.ModManagerConfig.Notes = Notes;
		Mod.ApplyModConfig(Mod.ModManagerConfig);

		//Should be called automatically when the mod config is updated
		//AppServices.Get<ISettingsService>().ModConfig.TrySave();
	}

	public void OnClose()
	{
		HasChanges = false;
		Mod = null;
	}

	private static string ModToTitle(ValueTuple<bool, DivinityModData?> x)
	{
		var (b, mod) = x;
		var result = b ? "*" : string.Empty;
		result += mod != null ? $"{mod.Name} Properties" : "Mod Properties";
		return result;
	}
	private static string GetModType(DivinityModData mod) => mod?.IsEditorMod == true ? "Editor Project" : "Pak";
	private static string GetModFilePath(DivinityModData mod) => StringUtils.ReplaceSpecialPathways(mod.FilePath);

	private static string GetModSize(DivinityModData mod)
	{
		if (mod == null) return "0 bytes";

		try
		{
			if (mod != null && File.Exists(mod.FilePath))
			{
				if (mod.IsEditorMod)
				{
					var dir = new DirectoryInfo(mod.FilePath);
					var length = dir.EnumerateFiles("*.*", System.IO.SearchOption.AllDirectories).Sum(file => file.Length);
					return StringUtils.BytesToString(length);
				}
				else
				{
					return StringUtils.BytesToString(new FileInfo(mod.FilePath).Length);
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error checking mod file size at path '{mod?.FilePath}':\n{ex}");
		}
		return "0 bytes";
	}

	public ModPropertiesWindowViewModel()
	{
		Title = "Mod Properties";

		var whenModSet = this.WhenAnyValue(x => x.Mod).WhereNotNull();

		whenModSet.Subscribe(LoadConfigProperties);

		whenModSet.Select(GetModType).ToUIProperty(this, x => x.ModType);
		whenModSet.Select(GetModSize).ToUIProperty(this, x => x.ModSizeText);
		whenModSet.Select(GetModFilePath).ToUIProperty(this, x => x.ModFilePath);
		whenModSet.Select(x => x.IsEditorMod).ToUIProperty(this, x => x.IsEditorMod);
		whenModSet.Select(x => x.FileName).ToUIProperty(this, x => x.ModFileName);
		whenModSet.Select(x => x.Name).ToUIProperty(this, x => x.ModName);
		whenModSet.Select(x => x.Description).ToUIProperty(this, x => x.ModDescription);

		var autoSaveProperties = new HashSet<string>()
		{
			nameof(GitHub),
			nameof(NexusModsId),
			nameof(ModioId),
			nameof(Notes),
		};

		var whenAutosavePropertiesChange = Observable.FromEvent<PropertyChangedEventHandler, PropertyChangedEventArgs>(
			h => (sender, e) => h(e),
			h => PropertyChanged += h,
			h => PropertyChanged -= h
		)
		.Where(e => e.PropertyName.IsValid() && autoSaveProperties.Contains(e.PropertyName));

		this.WhenAnyValue(x => x.IsVisible, x => x.Locked).Select(x => x.Item1 && !x.Item2)
		.CombineLatest(whenAutosavePropertiesChange)
		.Subscribe(_ =>
		{
			HasChanges = true;
		});

		this.WhenAnyValue(x => x.GitHub).Select(x => !x.IsValid())
			.ToUIProperty(this, x => x.GitHubPlaceholderLabelVisibility);

		var hasChanges = this.WhenAnyValue(x => x.HasChanges);
		hasChanges.CombineLatest(whenModSet).Select(ModToTitle).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.Title);

		OKCommand = ReactiveCommand.Create(Apply);
		CancelCommand = ReactiveCommand.Create(OnClose);
		ApplyCommand = ReactiveCommand.Create(Apply, hasChanges);
	}
}

public class DesignModPropertiesWindowViewModel : ModPropertiesWindowViewModel
{
	public DesignModPropertiesWindowViewModel()
	{
		Mod = new DivinityModData()
		{
			UUID = "98a0d3f4-1c87-444c-8559-51c1d5ba650f",
			Name = "Test Mod",
			FilePath = "%LOCALAPPDATA%\\Larian Studios\\Baldur's Gate 3\\Mods\\TestMod.pak",
			Author = "LaughingLeader",
			Version = new Models.LarianVersion("1.2.3.4"),
			Folder = "TestMod_98a0d3f4-1c87-444c-8559-51c1d5ba650f",
			ModType = "Add-on",
			Description = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed quis efficitur velit. Nullam nibh ex, pharetra eu bibendum pretium, mollis sit amet sapien. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Vestibulum vestibulum accumsan odio sed interdum. Morbi in dictum urna. Sed dapibus velit congue libero pharetra, in egestas nisi vehicula. Aliquam erat volutpat. Integer malesuada tincidunt lacus, dictum gravida augue maximus eu. Donec lobortis, urna quis convallis vehicula, arcu arcu vehicula massa, sed fermentum nisl nisi nec lacus. Suspendisse porttitor sem magna, nec sollicitudin nibh efficitur at. Sed metus enim, lobortis sed risus id, lacinia imperdiet tellus. Phasellus enim est, tristique iaculis ornare non, blandit vitae nibh. Morbi mollis magna id enim congue iaculis. Maecenas ipsum mauris, dignissim nec imperdiet et, elementum vel magna."
		};
	}
}
