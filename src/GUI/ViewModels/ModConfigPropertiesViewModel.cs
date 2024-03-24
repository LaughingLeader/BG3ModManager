using ModManager.Models.Mod;
using ModManager.Util;

using DynamicData;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.ComponentModel;
using System.IO;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace ModManager.ViewModels;

public class ModConfigPropertiesViewModel : ReactiveObject
{
	[Reactive] public string Title { get; set; }
	[Reactive] public bool IsActive { get; set; }
	[Reactive] public bool Locked { get; set; }
	[Reactive] public bool HasChanges { get; private set; }
	[Reactive] public DivinityModData Mod { get; set; }
	[Reactive] public string Notes { get; set; }
	[Reactive] public string GitHub { get; set; }
	[Reactive] public long NexusModsId { get; set; }
	[Reactive] public long SteamWorkshopId { get; set; }

	[ObservableAsProperty] public string ModType { get; }
	[ObservableAsProperty] public string ModSizeText { get; }
	[ObservableAsProperty] public string ModFilePath { get; }
	[ObservableAsProperty] public bool IsEditorMod { get; }
	[ObservableAsProperty] public Visibility GitHubPlaceholderLabelVisibility { get; }

	public ICommand OKCommand { get; set; }
	public ICommand CancelCommand { get; set; }
	public ICommand ApplyCommand { get; }

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
				SteamWorkshopId = mod.ModManagerConfig.SteamWorkshopId;
				Notes = mod.ModManagerConfig.Notes;
			}
			else
			{
				GitHub = mod.GitHubData.Url;
				NexusModsId = mod.NexusModsData.ModId;
				SteamWorkshopId = mod.WorkshopData.ModId;
				Notes = "";
			}
		}
		Locked = HasChanges = false;
		//disp.Dispose();
	}

	public void Apply()
	{
		if (Mod.ModManagerConfig == null) throw new NullReferenceException($"ModManagerConfig is null for mod ({Mod})");
		var modConfigService = AppServices.Get<ISettingsService>().ModConfig;

		if (String.IsNullOrEmpty(Mod.ModManagerConfig.Id)) Mod.ModManagerConfig.Id = Mod.UUID;

		modConfigService.Mods.AddOrUpdate(Mod.ModManagerConfig);

		Mod.ModManagerConfig.GitHub = GitHub;
		Mod.ModManagerConfig.NexusModsId = NexusModsId;
		Mod.ModManagerConfig.SteamWorkshopId = SteamWorkshopId;
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

	private static string ModToTitle(DivinityModData mod) => mod != null ? $"{mod.DisplayName} Properties" : "Mod Properties";
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

	public ModConfigPropertiesViewModel()
	{
		Title = "Mod Properties";

		var whenModSet = this.WhenAnyValue(x => x.Mod).WhereNotNull();
		whenModSet.Select(ModToTitle).ObserveOn(RxApp.MainThreadScheduler).BindTo(this, x => x.Title);

		whenModSet.Subscribe(LoadConfigProperties);

		whenModSet.Select(GetModType).ToUIProperty(this, x => x.ModType);
		whenModSet.Select(GetModSize).ToUIProperty(this, x => x.ModSizeText);
		whenModSet.Select(GetModFilePath).ToUIProperty(this, x => x.ModFilePath);
		whenModSet.Select(x => x.IsEditorMod).ToUIProperty(this, x => x.IsEditorMod);

		var whenNotLocked = this.WhenAnyValue(x => x.Locked, x => x.IsActive).Select(x => !x.Item1 && x.Item2);
		var whenConfig = Observable.FromEventPattern<PropertyChangedEventArgs>(this, nameof(ReactiveObject.PropertyChanged));
		var autoSaveProperties = new HashSet<string>()
		{
			nameof(GitHub),
			nameof(NexusModsId),
			nameof(SteamWorkshopId),
			nameof(Notes),
		};

		whenConfig.Where(e => autoSaveProperties.Contains(e.EventArgs.PropertyName)).Subscribe(e =>
		{
			if (IsActive && !Locked) HasChanges = true;
		});

		this.WhenAnyValue(x => x.GitHub).Select(x => PropertyConverters.StringToVisibilityReversed(x, Visibility.Hidden))
			.ToUIProperty(this, x => x.GitHubPlaceholderLabelVisibility, Visibility.Visible);

		ApplyCommand = ReactiveCommand.Create(Apply, this.WhenAnyValue(x => x.HasChanges));
		/*whenConfig.Where(e => autoSaveProperties.Contains(e.EventArgs.PropertyName))
		.Throttle(TimeSpan.FromMilliseconds(100))
		.Select(x => Unit.Default)
		.InvokeCommand(ApplyConfigCommand);*/
	}
}
