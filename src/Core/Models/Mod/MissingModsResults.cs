using ModManager.Models.Mod.Game;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Models.Mod;
public class MissingModsResults
{
	public Dictionary<string, MissingModData> Missing { get; } = [];
	public Dictionary<string, MissingModData> Dependencies { get; } = [];
	public Dictionary<string, MissingModData> ExtenderRequired { get; } = [];

	public int TotalMissing
	{
		get
		{
			return Missing.Count + Dependencies.Count;
		}
	}

	public void AddMissing(ModuleShortDesc mod, int index)
	{
		if (!mod.UUID.IsValid()) return;

		if (!Missing.ContainsKey(mod.UUID))
		{
			Missing.Add(mod.UUID, MissingModData.FromData(mod, index, false));
		}
	}

	public void AddDependency(ModuleShortDesc mod, string[]? requiredBy = null)
	{
		if (!mod.UUID.IsValid()) return;

		if (Dependencies.TryGetValue(mod.UUID, out var existing))
		{
			if (requiredBy != null)
			{
				existing.RequiredBy.AddRange(requiredBy);
			}
		}
		else
		{
			Dependencies.Add(mod.UUID, MissingModData.FromData(mod, -1, true, requiredBy));
		}
	}

	public void AddDependency(ModuleShortDesc mod, ModData requiredByMod)
	{
		if (!mod.UUID.IsValid()) return;

		var requiredByName = requiredByMod.Name ?? requiredByMod.FileName!;

		if (Dependencies.TryGetValue(mod.UUID, out var existing))
		{
			existing.RequiredBy.Add(requiredByName);
		}
		else
		{
			Dependencies.Add(mod.UUID, MissingModData.FromData(mod, -1, true, [requiredByName]));
		}
	}

	public void AddExtenderRequirement(ModData mod, string[]? requiredBy = null)
	{
		if (!mod.UUID.IsValid()) return;

		if (ExtenderRequired.TryGetValue(mod.UUID, out var existing))
		{
			if (requiredBy != null)
			{
				existing.RequiredBy.AddRange(requiredBy);
			}
		}
		else
		{
			ExtenderRequired.Add(mod.UUID, MissingModData.FromData(mod, false, requiredBy));
		}
	}

	public string GetMissingMessage()
	{
		if (Missing.Count == 0) return string.Empty;
		var message = string.Join(Environment.NewLine, Missing.Values.OrderBy(x => x.Index));
		return message;
	}

	public string GetDependenciesMessage()
	{
		if(Dependencies.Count == 0) return string.Empty;
		var message = string.Join(Environment.NewLine, Dependencies.Values.OrderBy(x => x.Name));
		return message;
	}

	public string GetExtenderRequiredMessage()
	{
		if(ExtenderRequired.Count == 0) return string.Empty;
		var message = string.Join(Environment.NewLine, ExtenderRequired.Values.OrderBy(x => x.Name));
		return message;
	}
}
