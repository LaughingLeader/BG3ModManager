using DivinityModManager.Models;

using DynamicData;

using ReactiveUI;

using Splat;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.ViewModels.Main;
public class ModOrderViewModel : ReactiveObject, IRoutableViewModel
{
	public string UrlPathSegment => "modorder";
	public IScreen HostScreen { get; }

	protected readonly SourceCache<DivinityModData, string> mods = new(mod => mod.UUID);

	protected ReadOnlyObservableCollection<DivinityModData> addonMods;
	public ReadOnlyObservableCollection<DivinityModData> Mods => addonMods;

	protected ReadOnlyObservableCollection<DivinityModData> adventureMods;
	public ReadOnlyObservableCollection<DivinityModData> AdventureMods => adventureMods;

	public bool ModExists(string uuid)
	{
		return mods.Lookup(uuid) != null;
	}

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


	public ModOrderViewModel(IScreen hostScreen)
	{
		HostScreen = hostScreen ?? Locator.Current.GetService<IScreen>();
	}
}