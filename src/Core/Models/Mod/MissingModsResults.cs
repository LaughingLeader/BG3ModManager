using ModManager.Models.Mod.Game;

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

		if (Dependencies.TryGetValue(mod.UUID, out var dep))
		{
			Dependencies.Remove(mod.UUID);
			dep.Index = index;
			Missing.Add(mod.UUID, dep);
		}
		else if (!Missing.ContainsKey(mod.UUID))
		{
			Missing.Add(mod.UUID, MissingModData.FromData(mod, index, false));
		}
	}

	public void AddDependency(ModuleShortDesc mod, params string[] requiredBy)
	{
		if (!mod.UUID.IsValid()) return;

		if (Missing.TryGetValue(mod.UUID, out var existingMissing))
		{
			if (requiredBy != null) existingMissing.RequiredBy.AddRange(requiredBy);
		}
		else
		{
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
	}

	public void AddDependency(ModuleShortDesc mod, ModData requiredByMod)
	{
		if (!mod.UUID.IsValid()) return;

		var requiredByName = requiredByMod.Name ?? requiredByMod.FileName!;

		AddDependency(mod, requiredByName);
	}

	public void AddExtenderRequirement(ModData mod, params string[] requiredBy)
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
