using DynamicData;

using ModManager.Models.App;
using ModManager.Util;

using Newtonsoft.Json;

using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;

namespace ModManager.ViewModels;

public class AppKeys : ReactiveObject
{
	[MenuSettings("File", "Import Mods...", true)]
	public Hotkey ImportMod { get; } = new Hotkey(Key.O, ModifierKeys.Control);

	[MenuSettings("File", "Import Nexus Mods Data from Archives...", true)]
	public Hotkey ImportNexusModsIds { get; } = new Hotkey();

	[MenuSettings("File", "Add New Order", true)]
	public Hotkey NewOrder { get; } = new Hotkey(Key.N, ModifierKeys.Control);

	[MenuSettings("File", "Save Order")]
	public Hotkey Save { get; } = new Hotkey(Key.S, ModifierKeys.Control);

	[MenuSettings("File", "Save Order As...", true)]
	public Hotkey SaveAs { get; } = new Hotkey(Key.S, ModifierKeys.Control | ModifierKeys.Alt);

	[MenuSettings("File", "Import Order from Save...")]
	public Hotkey ImportOrderFromSave { get; } = new Hotkey(Key.I, ModifierKeys.Control);

	[MenuSettings("File", "Import Order from Save As New Order...")]
	public Hotkey ImportOrderFromSaveAsNew { get; } = new Hotkey(Key.I, ModifierKeys.Control | ModifierKeys.Shift);

	[MenuSettings("File", "Import Order from File...")]
	public Hotkey ImportOrderFromFile { get; } = new Hotkey(Key.O, ModifierKeys.Control | ModifierKeys.Shift);

	[MenuSettings("File", "Import Order & Mods from Archive...", true)]
	public Hotkey ImportOrderFromZipFile { get; } = new Hotkey(Key.None);

	//TODO GM/DM mode isn't a thing in BG3
#if DOS2
	[MenuSettings("File", "Load Order From Selected GM Campaign", true)]
#endif
	public Hotkey ImportOrderFromSelectedGMCampaign { get; } = new Hotkey(Key.None);

	[MenuSettings("File", "Export Order to Game")]
	public Hotkey ExportOrderToGame { get; } = new Hotkey(Key.E, ModifierKeys.Control);

	[MenuSettings("File", "Export Order to Text File...")]
	public Hotkey ExportOrderToList { get; } = new Hotkey(Key.E, ModifierKeys.Control | ModifierKeys.Shift);

	[MenuSettings("File", "Export Order to Archive (.zip)")]
	public Hotkey ExportOrderToZip { get; } = new Hotkey(Key.R, ModifierKeys.Control);

	[MenuSettings("File", "Export Order to Archive As...", true)]
	public Hotkey ExportOrderToArchiveAs { get; } = new Hotkey(Key.R, ModifierKeys.Control | ModifierKeys.Shift);

	[MenuSettings("File", "Reload All")]
	public Hotkey Refresh { get; } = new Hotkey(Key.F5);

	[MenuSettings("File", "Refresh Mod Updates")]
	public Hotkey RefreshModUpdates { get; } = new Hotkey(Key.F6);

	[MenuSettings("Edit", "Moved Selected Mods to Opposite List", true)]
	public Hotkey Confirm { get; } = new Hotkey(Key.Enter);

	[MenuSettings("Edit", "Focus Active Mods List")]
	public Hotkey MoveFocusLeft { get; } = new Hotkey(Key.Left);

	[MenuSettings("Edit", "Focus Inactive Mods List")]
	public Hotkey MoveFocusRight { get; } = new Hotkey(Key.Right);

	[MenuSettings("Edit", "Go to Other List")]
	public Hotkey SwapListFocus { get; } = new Hotkey(Key.Tab);

	[MenuSettings("Edit", "Move to Top of Active List")]
	public Hotkey MoveToTop { get; } = new Hotkey(Key.PageUp, ModifierKeys.Control);

	[MenuSettings("Edit", "Move to Bottom of Active List", true)]
	public Hotkey MoveToBottom { get; } = new Hotkey(Key.PageDown, ModifierKeys.Control);

	[MenuSettings("Edit", "Toggle Focus Filter for Current List", AddSeparator = true)]
	public Hotkey ToggleFilterFocus { get; } = new Hotkey(Key.F, ModifierKeys.Control);

	[MenuSettings("Edit", "Show File Names for Mods")]
	public Hotkey ToggleFileNameDisplay { get; } = new Hotkey(Key.None);

	[MenuSettings("Edit", "Delete Selected Mods...", AddSeparator = true)]
	public Hotkey DeleteSelectedMods { get; } = new Hotkey(Key.Delete);

	[MenuSettings("Settings", "Open Preferences")]
	public Hotkey OpenPreferences { get; } = new Hotkey(Key.P, ModifierKeys.Control);

	[MenuSettings("Settings", "Open Keyboard Shortcuts")]
	public Hotkey OpenKeybindings { get; } = new Hotkey(Key.K, ModifierKeys.Control);

	[MenuSettings("Settings", "Toggle Light/Dark Mode")]
	public Hotkey ToggleViewTheme { get; } = new Hotkey(Key.L, ModifierKeys.Control);

	[MenuSettings("View", "Toggle Updates View")]
	public Hotkey ToggleUpdatesView { get; } = new Hotkey(Key.U, ModifierKeys.Control);

	[MenuSettings("Go", "Open Mods Folder")]
	public Hotkey OpenModsFolder { get; } = new Hotkey(Key.D1, ModifierKeys.Control);

	[MenuSettings("Go", "Open Game Folder")]
	public Hotkey OpenGameFolder { get; } = new Hotkey(Key.D2, ModifierKeys.Control);

	[MenuSettings("Go", "Open Workshop Folder")]
	public Hotkey OpenWorkshopFolder { get; } = new Hotkey(Key.D3, ModifierKeys.Control);

	[MenuSettings("Go", "Open Extender Logs Folder")]
	public Hotkey OpenLogsFolder { get; } = new Hotkey(Key.D4, ModifierKeys.Control);

	[MenuSettings("Go", "Launch Game")]
	public Hotkey LaunchGame { get; } = new Hotkey(Key.G, ModifierKeys.Control | ModifierKeys.Shift);

	[MenuSettings("Download", "Download & Extract the Script Extender...")]
	public Hotkey DownloadScriptExtender { get; } = new Hotkey(Key.None);

	[MenuSettings("Download", @"Download nxm:\\ Link...", ToolTip = "Download a NexusMods link for a mod file or a collection", AddSeparator = true)]
	public Hotkey DownloadNXMLink { get; } = new Hotkey(Key.None);

	[MenuSettings("Download", @"Open Collection Downloader Window")]
	public Hotkey OpenCollectionDownloaderWindow { get; } = new Hotkey(Key.None);

	[MenuSettings("Tools", "Extract Selected Mods To...")]
	public Hotkey ExtractSelectedMods { get; } = new Hotkey(Key.OemPeriod, ModifierKeys.Control);

	[MenuSettings("Tools", "Extract Active Adventure Mod To...")]
	public Hotkey ExtractSelectedAdventure { get; } = new Hotkey(Key.None);

	[MenuSettings("Tools", "Toggle Version Generator Window", ToolTip = "A tool for mod authors to generate version numbers for a mod's meta.lsx")]
	public Hotkey ToggleVersionGeneratorWindow { get; } = new Hotkey(Key.G, ModifierKeys.Control);

	[MenuSettings("Tools", "Speak Active Order")]
	public Hotkey SpeakActiveModOrder { get; } = new Hotkey(Key.Home, ModifierKeys.Control);

	[MenuSettings("Help", "Check for Updates...")]
	public Hotkey CheckForUpdates { get; } = new Hotkey(Key.F7);

	[MenuSettings("Help", "Donate a Coffee...")]
	public Hotkey OpenDonationLink { get; } = new Hotkey(Key.F10);

	[MenuSettings("Help", "About")]
	public Hotkey OpenAboutWindow { get; } = new Hotkey(Key.F1);

	[MenuSettings("Help", "Open Repository Page...")]
	public Hotkey OpenRepositoryPage { get; } = new Hotkey(Key.F11);

	private readonly SourceCache<Hotkey, string> keyMap = new((hk) => hk.ID);

	protected readonly ReadOnlyObservableCollection<Hotkey> allKeys;
	public ReadOnlyObservableCollection<Hotkey> All => allKeys;

	public static string SettingsFilePath() => DivinityApp.GetAppDirectory("Data", "keybindings.json");
	public static string DefaultSettingsFilePath() => DivinityApp.GetAppDirectory("Data", "keybindings-default.json");

	public void SaveDefaultKeybindings()
	{
		var filePath = DefaultSettingsFilePath();
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			var keyMapDict = new Dictionary<string, Hotkey>();
			foreach (var key in All)
			{
				keyMapDict.Add(key.ID, key);
			}
			var contents = JsonConvert.SerializeObject(keyMapDict, Newtonsoft.Json.Formatting.Indented);
			File.WriteAllText(filePath, contents);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error saving default keybindings at '{filePath}': {ex}");
		}
	}

	public bool SaveKeybindings(out string result)
	{
		result = "";
		var filePath = SettingsFilePath();
		try
		{
			Directory.CreateDirectory(Path.GetDirectoryName(filePath));
			var keyMapDict = new Dictionary<string, Hotkey>();
			foreach (var key in All)
			{
				if (!key.IsDefault)
				{
					keyMapDict.Add(key.ID, key);
				}
			}
			if (keyMapDict.Count > 0)
			{
				var contents = JsonConvert.SerializeObject(keyMapDict, Newtonsoft.Json.Formatting.Indented);
				File.WriteAllText(filePath, contents);
			}
			else
			{
				File.WriteAllText(filePath, "{}");
			}
			result = $"Saved keybindings to '{filePath}'";
			return true;
		}
		catch (Exception ex)
		{
			result = $"Error saving keybindings at '{filePath}': {ex}";
		}
		return false;
	}

	public bool LoadKeybindings(MainWindowViewModel vm)
	{
		var filePath = SettingsFilePath();
		try
		{
			if (DivinityJsonUtils.TrySafeDeserializeFromPath<Dictionary<string, Hotkey>>(filePath, out var allKeybindings))
			{
				foreach (var kvp in allKeybindings)
				{
					var existingHotkey = All.FirstOrDefault(x => x.ID.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
					if (existingHotkey != null)
					{
						existingHotkey.Key = kvp.Value.Key;
						existingHotkey.Modifiers = kvp.Value.Modifiers;
						existingHotkey.UpdateDisplayBindingText();
					}
				}
				return true;
			}
		}
		catch (Exception ex)
		{
			vm.ShowAlert($"Error loading keybindings at '{filePath}': {ex}", AlertType.Danger);
		}
		return false;
	}

	public void SetToDefault()
	{
		foreach (var entry in keyMap.Items)
		{
			entry.ResetToDefault();
		}
	}

	public AppKeys(MainWindowViewModel vm)
	{
		keyMap.Connect().Bind(out allKeys).Subscribe();
		var baseCanExecute = vm.WhenAnyValue(x => x.IsLocked, b => !b);
		var t = typeof(AppKeys);
		// Building a list of keys / key names from properties, because lazy
		var keyProps = t.GetRuntimeProperties().Where(prop => Attribute.IsDefined(prop, typeof(MenuSettingsAttribute)) && prop.GetGetMethod() != null).ToList();
		foreach (var prop in keyProps)
		{
			var hotkey = (Hotkey)t.GetProperty(prop.Name).GetValue(this);
			hotkey.AddCanExecuteCondition(baseCanExecute);
			hotkey.ID = prop.Name;
			keyMap.AddOrUpdate(hotkey);
		}
	}
}
