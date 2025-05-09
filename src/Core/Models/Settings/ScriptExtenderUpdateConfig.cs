using ModManager.Enums.Extender;
using ModManager.Extensions;

using System.ComponentModel;
using System.Runtime.Serialization;

namespace ModManager.Models.Settings;

[DataContract]
public class ScriptExtenderUpdateConfig : ReactiveObject
{
	[Reactive] public bool DevOptionsEnabled { get; set; }
	[Reactive] public bool UpdaterIsAvailable { get; set; }
	[Reactive] public int UpdaterVersion { get; set; }

	[SettingsEntry("Update Channel", "Use a specific update channel")]
	[DataMember, Reactive]
	[DefaultValue(ExtenderUpdateChannel.Release)]
	public ExtenderUpdateChannel UpdateChannel { get; set; }

	[SettingsEntry("Target Version", "Update to a specific version of the script extender (ex. '5.0.0.0')")]
	[DataMember, Reactive]
	[DefaultValue(null)]
	public string? TargetVersion { get; set; }

	[SettingsEntry("Target Resource Digest", "Use a specific Digest for the target update", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(null)]
	public string? TargetResourceDigest { get; set; }

	[SettingsEntry("Disable Updates", "Disable automatic updating to the latest extender version")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool DisableUpdates { get; set; }

	[SettingsEntry("IPv4Only", "Use only IPv4 when fetching the latest update")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool IPv4Only { get; set; }

	[SettingsEntry("Debug", "Enable debug mode in the extender updater, which prints more messages to the console window")]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool Debug { get; set; }

	[SettingsEntry("Manifest URL", "", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(null)]
	public string? ManifestURL { get; set; }

	[SettingsEntry("Manifest Name", "", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(null)]
	public string? ManifestName { get; set; }

	[SettingsEntry("CachePath", "", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(null)]
	public string? CachePath { get; set; }

	[SettingsEntry("Validate Signature", "", BindVisibilityTo = nameof(DevOptionsEnabled))]
	[DataMember, Reactive]
	[DefaultValue(false)]
	public bool ValidateSignature { get; set; }

	public ScriptExtenderUpdateConfig()
	{
		this.SetToDefault();
		UpdaterVersion = -1;
	}
}
