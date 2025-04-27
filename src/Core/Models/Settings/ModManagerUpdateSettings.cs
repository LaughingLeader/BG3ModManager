using ModManager.Extensions;
using ModManager.Util;

using System.ComponentModel;
using System.Runtime.Serialization;

namespace ModManager.Models.Settings;

[DataContract]
public class ModManagerUpdateSettings : ReactiveObject
{
	[DefaultValue(true)]
	[SettingsEntry("Update the Script Extender", "If the Script Extender updater is installed (DXGI.dll), automatically update it via Tools/Toolbox.exe")]
	[DataMember, Reactive] public bool UpdateScriptExtender { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Update GitHub Mods", "Automatically check for updates for mods configured with GitHub repository releases")]
	[DataMember, Reactive] public bool UpdateGitHubMods { get; set; }

	[DefaultValue(true)]
	[SettingsEntry("Update NexusMods Mods", "Automatically check for updates for mods configured with NexusMods releases")]
	[DataMember, Reactive] public bool UpdateNexusMods { get; set; }

	//TODO: Remove if Larian doesn't add workshop support
	[DefaultValue(true)]
	[SettingsEntry("Update Mod.io Mods", "Automatically check for updates for mods configured with Mod.io releases")]
	[DataMember, Reactive] public bool UpdateModioMods { get; set; }

	[DefaultValue("")]
	[SettingsEntry("NexusMods API Key", "Your NexusMods API key, which will allow the mod manager to fetch mod updates/information")]
	[DataMember, Reactive] public string? NexusModsAPIKey { get; set; }

	[DefaultValue("")]
	[SettingsEntry("Mod.io API Key", "Your Mod.io Web API key, which will allow the mod manager to fetch mod updates/information from Mod.io")]
	[DataMember, Reactive] public string? ModioAPIKey { get; set; }

	[DefaultValue(typeof(TimeSpan), "00:30:00")] // 30 minutes
	[SettingsEntry("Minimum Update Time Period", "Prevent checking for updates for individual mods until this amount of time has passed since the last check\nThis is to prevent hitting API limits too quickly")]
	[DataMember, Reactive] public TimeSpan MinimumUpdateTimePeriod { get; set; }

	[DefaultValue(false)]
	[SettingsEntry("Allow Adult Content", "Allow adult content when downloading collections from NexusMods")]
	[DataMember, Reactive] public bool AllowAdultContent { get; set; }

	[Reactive] public bool IsAssociatedWithNXM { get; set; }

	public ModManagerUpdateSettings()
	{
		IsAssociatedWithNXM = RegistryHelper.IsAssociatedWithNXMProtocol(DivinityApp.GetExePath());
		this.SetToDefault();
	}
}
