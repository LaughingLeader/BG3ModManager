using DynamicData;

using ModManager.Models;
using ModManager.Models.App;
using ModManager.Models.Mod;
using ModManager.Models.View;
using ModManager.Util;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace ModManager.Services;

public class ModManagerService : ReactiveObject, IModManagerService
{
	private readonly SourceCache<DivinityModData, string> mods = new(mod => mod.UUID);

	//Derived collections
	private readonly ReadOnlyObservableCollection<DivinityModData> _addonMods;
	private readonly ReadOnlyObservableCollection<DivinityModData> _adventureMods;
	private readonly ReadOnlyObservableCollection<DivinityModData> _forceLoadedMods;
	private readonly ReadOnlyObservableCollection<DivinityModData> _selectedPakMods;
	private readonly ReadOnlyObservableCollection<DivinityModData> _userMods;

	public IEnumerable<DivinityModData> AllMods => mods.Items;
	public ReadOnlyObservableCollection<DivinityModData> AddonMods => _addonMods;
	public ReadOnlyObservableCollection<DivinityModData> AdventureMods => _adventureMods;
	public ReadOnlyObservableCollection<DivinityModData> ForceLoadedMods => _forceLoadedMods;
	public ReadOnlyObservableCollection<DivinityModData> UserMods => _userMods;
	public ReadOnlyObservableCollection<DivinityModData> SelectedPakMods => _selectedPakMods;

	[ObservableAsProperty] public int ActiveSelected { get; }
	[ObservableAsProperty] public int InactiveSelected { get; }
	[ObservableAsProperty] public int OverrideModsSelected { get; }

	private readonly System.Reactive.Subjects.IConnectableObservable<IChangeSet<DivinityModData, string>> _modsConnection;
	public System.Reactive.Subjects.IConnectableObservable<IChangeSet<DivinityModData, string>> ModsConnection => _modsConnection;

	public bool ModExists(string uuid) => mods.Lookup(uuid) != null;

	public bool TryGetMod(string guid, out DivinityModData mod)
	{
		mod = null;
		var modResult = mods.Lookup(guid);
		if (modResult.HasValue)
		{
			mod = modResult.Value;
			return true;
		}
		return false;
	}

	public string GetModType(string guid)
	{
		if (TryGetMod(guid, out var mod))
		{
			return mod.ModType;
		}
		return "";
	}

	public bool ModIsAvailable(IDivinityModData divinityModData)
	{
		return ModExists(divinityModData.UUID)
			|| DivinityApp.IgnoredMods.Any(im => im.UUID == divinityModData.UUID)
			|| DivinityApp.IgnoredDependencyMods.Any(d => d.UUID == divinityModData.UUID);
	}

	public void DeselectAllMods()
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			foreach (var mod in AddonMods)
			{
				mod.IsSelected = false;
			}
		});
	}

	public void Refresh()
	{
		mods.Clear();
	}

	public void ApplyUserModConfig()
	{
		var settings = Locator.Current.GetService<ISettingsService>().ModConfig;

		foreach (var mod in AddonMods)
		{
			var config = settings.Mods.Lookup(mod.UUID);
			if (config.HasValue)
			{
				mod.ApplyModConfig(config.Value);
			}
		}
	}

	public void Add(DivinityModData mod) => mods.AddOrUpdate(mod);
	public void RemoveByUUID(string uuid) => mods.RemoveKey(uuid);
	public void RemoveByUUID(IEnumerable<string> uuids) => mods.RemoveKeys(uuids);

	public void SetLoadedMods(IEnumerable<DivinityModData> loadedMods, bool nexusModsEnabled = false)
	{
		mods.Clear();
		foreach (var mod in loadedMods)
		{
			//mod.SteamWorkshopEnabled = SteamWorkshopSupportEnabled;
			mod.NexusModsEnabled = nexusModsEnabled;

			if (mod.IsLarianMod)
			{
				var existingIgnoredMod = DivinityApp.IgnoredMods.FirstOrDefault(x => x.UUID == mod.UUID);
				if (existingIgnoredMod != null && existingIgnoredMod != mod)
				{
					DivinityApp.IgnoredMods.Remove(existingIgnoredMod);
				}
				DivinityApp.IgnoredMods.Add(mod);
			}

			if (TryGetMod(mod.UUID, out var existingMod))
			{
				if (mod.Version.VersionInt > existingMod.Version.VersionInt)
				{
					mods.AddOrUpdate(mod);
					DivinityApp.Log($"Updated mod data from pak: Name({mod.Name}) UUID({mod.UUID}) Type({mod.ModType}) Version({mod.Version.VersionInt})");
				}
			}
			else
			{
				mods.AddOrUpdate(mod);
			}
		}
	}

	public IEnumerable<IModEntry> GetAllModsAsInterface() => mods.Items.Select(x => new ModEntry(x));

	#region Mod Loading

	private static CancellationTokenSource GetCancellationToken(int delay, CancellationTokenSource last = null)
	{
		CancellationTokenSource token = new();
		if (last != null && last.IsCancellationRequested)
		{
			last.Dispose();
		}
		token.CancelAfter(delay);
		return token;
	}

	private async static Task<TResult> RunTask<TResult>(Task<TResult> task, TResult defaultValue)
	{
		try
		{
			return await task;
		}
		catch (OperationCanceledException)
		{
			DivinityApp.Log("Operation timed out/canceled.");
		}
		catch (TimeoutException)
		{
			DivinityApp.Log("Operation timed out.");
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error awaiting task:\n{ex}");
		}
		return defaultValue;
	}

	private static void MergeModLists(ref List<DivinityModData> finalMods, IEnumerable<DivinityModData> newMods, bool preferNew = false)
	{
		foreach (var mod in newMods)
		{
			var existing = finalMods.FirstOrDefault(x => x.UUID == mod.UUID);
			if (existing != null)
			{
				if (preferNew || existing.Version.VersionInt < mod.Version.VersionInt)
				{
					finalMods.Replace(existing, mod);
				}
			}
			else
			{
				finalMods.Add(mod);
			}
		}
	}

	public async Task<List<DivinityModData>> LoadModsAsync(string gameDataPath, string userModsDirectoryPath, CancellationToken token)
	{
		var mods = await DivinityModDataLoader.LoadModsAsync(gameDataPath, userModsDirectoryPath, token);
		
		var baseMods = mods.DataDirectoryMods.Mods;
		var userMods = mods.UserDirectoryMods.Mods;

		var allMods = new List<DivinityModData>();

		if (baseMods.Count < DivinityApp.IgnoredMods.Count)
		{
			if (baseMods.Count == 0)
			{
				foreach(var mod in DivinityApp.IgnoredMods)
				{
					baseMods[mod.UUID] = mod;
				}
			}
			else
			{
				foreach (var mod in DivinityApp.IgnoredMods)
				{
					if (!baseMods.ContainsKey(mod.UUID)) baseMods[mod.UUID] = mod;
				}
			}
		}

		MergeModLists(ref allMods, baseMods.Values);
		MergeModLists(ref allMods, userMods.Values);

		var dupes = mods.UserDirectoryMods.Duplicates;

		var dupeCount = dupes.Count;
		if (dupeCount > 0)
		{
			DivinityApp.Log($"{dupeCount} duplicate(s) found:");
			DivinityApp.Log("=======");
			DivinityApp.Log($"{String.Join(Environment.NewLine, dupes.Select(x => x.ToString()))}");
			DivinityApp.Log("=======");
			_commands.ShowAlert($"{dupeCount} duplicate mod(s) found", AlertType.Danger, 30);
			await _interactions.DeleteMods.Handle(new DeleteModsRequest(dupes.ToModInterface(), true));
		}

		var finalMods = allMods.OrderBy(m => m.Name).ToList();
		DivinityApp.Log($"Loaded '{finalMods.Count}' mods.");
		return finalMods;
	}

	public async Task<List<DivinityGameMasterCampaign>> LoadGameMasterCampaignsAsync(string campaignsDirectoryPath, ProgressUpdateActions progress, double taskStepAmount = 0.1d)
	{
		List<DivinityGameMasterCampaign> data = null;

		var cancelTokenSource = GetCancellationToken(int.MaxValue);

		if (!String.IsNullOrWhiteSpace(campaignsDirectoryPath) && Directory.Exists(campaignsDirectoryPath))
		{
			DivinityApp.Log($"Loading gamemaster campaigns from '{campaignsDirectoryPath}'.");
			await progress.UpdateProgressText("Loading GM Campaigns from documents folder...");
			cancelTokenSource.CancelAfter(60000);
			data = DivinityModDataLoader.LoadGameMasterData(campaignsDirectoryPath, cancelTokenSource.Token);
			cancelTokenSource = GetCancellationToken(int.MaxValue);
			await progress.IncreaseAmount(taskStepAmount);
		}

		if (data != null)
		{
			data = data.OrderBy(m => m.Name).ToList();
			DivinityApp.Log($"Loaded '{data.Count}' GM campaigns.");
		}

		return data;
	}

	#endregion

	private readonly IInteractionsService _interactions;
	private readonly IGlobalCommandsService _commands;

	public ModManagerService(IInteractionsService interactions, IGlobalCommandsService commands)
	{
		_interactions = interactions;
		_commands = commands;

		_modsConnection = mods.Connect().Publish();

		_modsConnection.Filter(x => x.IsUserMod).Bind(out _userMods).Subscribe();
		_modsConnection.AutoRefresh(x => x.CanAddToLoadOrder).Filter(x => x.CanAddToLoadOrder).Bind(out _addonMods).Subscribe();
		_modsConnection.Filter(x => x.ModType == "Adventure" && (!x.IsHidden || x.UUID == DivinityApp.MAIN_CAMPAIGN_UUID)).Bind(out _adventureMods).Subscribe();

		var forceLoadedObs = _modsConnection.AutoRefresh(x => x.ForceAllowInLoadOrder)
			.Filter(x => x.IsForceLoaded && !x.IsForceLoadedMergedMod && !x.ForceAllowInLoadOrder)
			.ObserveOn(RxApp.MainThreadScheduler);
		forceLoadedObs.Bind(out _forceLoadedMods).Subscribe();

		var selectedModsConnection = _modsConnection.AutoRefresh(x => x.IsSelected, TimeSpan.FromMilliseconds(25)).AutoRefresh(x => x.IsActive, TimeSpan.FromMilliseconds(25)).Filter(x => x.IsSelected);

		selectedModsConnection.Filter(x => x.IsSelected && !x.IsEditorMod && File.Exists(x.FilePath)).Bind(out _selectedPakMods).Subscribe();
		selectedModsConnection.Filter(x => x.IsActive).Count().ToUIProperty(this, x => x.ActiveSelected);
		selectedModsConnection.Filter(x => !x.IsActive).Count().ToUIProperty(this, x => x.InactiveSelected);
		forceLoadedObs.AutoRefresh(x => x.IsSelected).Filter(x => x.IsSelected).Count().ToUIProperty(this, x => x.OverrideModsSelected);

		_modsConnection.Connect();

		_interactions.ToggleModFileNameDisplay.RegisterHandler(interaction =>
		{
			foreach (var mod in mods.Items)
			{
				mod.DisplayFileForName = interaction.Input;
			}
			interaction.SetOutput(true);
		});
	}
}
