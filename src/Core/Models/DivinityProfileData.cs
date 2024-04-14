namespace ModManager.Models;

public class DivinityProfileData : ReactiveObject
{
	[Reactive] public string? Name { get; set; }
	[Reactive] public string? FolderName { get; set; }

	/// <summary>
	/// The stored name in the profile.lsb or profile5.lsb file.
	/// </summary>
	[Reactive] public string? ProfileName { get; set; }
	[Reactive] public string? UUID { get; set; }
	[Reactive] public string? FilePath { get; set; }
	[Reactive] public string? ModSettingsFile { get; private set; }

	/// <summary>
	/// The saved load order from modsettings.lsx
	/// </summary>
	public List<string> ModOrder { get; set; } = [];

	/// <summary>
	/// The mod data under the Mods node, from modsettings.lsx.
	/// </summary>
	public List<DivinityProfileActiveModData> ActiveMods { get; set; } = [];

	/// <summary>
	/// The ModOrder transformed into a DivinityLoadOrder. This is the "Current" order.
	/// </summary>
	public DivinityLoadOrder SavedLoadOrder { get; set; }

	public DivinityProfileData()
	{
		this.WhenAnyValue(x => x.FilePath).Select(x => !String.IsNullOrEmpty(x) ? Path.Join(x, "modsettings.lsx") : "").BindTo(this, x => x.ModSettingsFile);
	}
}
