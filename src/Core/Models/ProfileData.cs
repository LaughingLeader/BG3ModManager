using ModManager.Models.Mod.Game;

namespace ModManager.Models;

public class ProfileData : ReactiveObject
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
	/// The mod data under the Mods node, from modsettings.lsx.
	/// </summary>
	public List<ModuleShortDesc> ActiveMods { get; set; } = [];

	public List<string> GetModOrder(bool includeIgnoredMods = false)
	{
		var order = new List<string>();
		foreach(var mod in ActiveMods)
		{
			if(!string.IsNullOrEmpty(mod.UUID) && (includeIgnoredMods || !DivinityApp.IgnoredMods.Any(x => x.UUID == mod.UUID)))
			{
				order.Add(mod.UUID);
			}
		}
		return order;
	}

	public ProfileData()
	{
		this.WhenAnyValue(x => x.FilePath).Select(x => !String.IsNullOrEmpty(x) ? Path.Join(x, "modsettings.lsx") : "").BindTo(this, x => x.ModSettingsFile);
	}
}
