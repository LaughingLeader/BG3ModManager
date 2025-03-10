﻿using DivinityModManager.Enums.Extender;
using DivinityModManager.Extensions;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System.ComponentModel;
using System.Runtime.Serialization;

namespace DivinityModManager.Models.Extender;

[DataContract]
public class ScriptExtenderUpdateConfig : ReactiveObject
{
	[Reactive] public bool UpdaterIsAvailable { get; set; }
	[Reactive] public int UpdaterVersion { get; set; }

	[SettingsEntry("Update Channel", "Use a specific update channel", HideFromUI = true)]
	[Reactive]
	[DataMember]
	[DefaultValue(ExtenderUpdateChannel.Release)]
	[JsonConverter(typeof(StringEnumConverter))]
	public ExtenderUpdateChannel UpdateChannel { get; set; }

	[SettingsEntry("Target Version", "Update to a specific version of the script extender (ex. '5.0.0.0')")]
	[Reactive]
	[DataMember]
	[DefaultValue("")]
	public string TargetVersion { get; set; }

	[SettingsEntry("Target Resource Digest", "Use a specific Digest for the target update", true)]
	[Reactive]
	[DataMember]
	[DefaultValue("")]
	public string TargetResourceDigest { get; set; }

	[SettingsEntry("Disable Updates", "Disable automatic updating to the latest extender version")]
	[Reactive]
	[DataMember]
	[DefaultValue(false)]
	public bool DisableUpdates { get; set; }

	[SettingsEntry("IPv4Only", "Use only IPv4 when fetching the latest update")]
	[Reactive]
	[DataMember]
	[DefaultValue(false)]
	public bool IPv4Only { get; set; }

	[SettingsEntry("Debug", "Enable debug mode in the extender updater, which prints more messages to the console window")]
	[Reactive]
	[DataMember]
	[DefaultValue(false)]
	public bool Debug { get; set; }

	[SettingsEntry("Manifest URL", "", true)]
	[Reactive]
	[DataMember]
	[DefaultValue("")]
	public string ManifestURL { get; set; }

	[SettingsEntry("Manifest Name", "", true)]
	[Reactive]
	[DataMember]
	[DefaultValue("")]
	public string ManifestName { get; set; }

	[SettingsEntry("CachePath", "", true)]
	[Reactive]
	[DataMember]
	[DefaultValue("")]
	public string CachePath { get; set; }

	[SettingsEntry("Validate Signature", "", true)]
	[Reactive]
	[DataMember]
	[DefaultValue(false)]
	public bool ValidateSignature { get; set; }

	public ScriptExtenderUpdateConfig()
	{
		this.SetToDefault();
		UpdaterVersion = -1;
	}
}
