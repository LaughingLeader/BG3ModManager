using ModManager.Models.Mod.Game;

namespace ModManager.Models.Mod;

public class MissingModData(string uuid)
{
	public string? Name { get; set; }
	public int Index { get; set; }
	public string UUID { get; set; } = uuid;
	public string? Author { get; set; }
	public bool IsDependency { get; set; }
	public List<string> RequiredBy { get; } = [];

	public override string ToString()
	{
		List<string> text = [];
		if (Index > 0)
		{
			text.Add($"{Index}. ");
		}
		if (Name.IsValid())
		{
			text.Add(Name);
		}
		else
		{
			text.Add(UUID);
		}
		if (Author.IsValid())
		{
			text.Add(" by " + Author);
		}
		if (RequiredBy.Count > 0)
		{
			text.Add(", Required By " + string.Join(';', RequiredBy.Order().Distinct()));
		}
		return string.Join("", text);
	}

	public static MissingModData FromData(ModData modData, bool isDependency = true, string[]? requiredBy = null)
	{
		var data = new MissingModData(modData.UUID)
		{
			Name = modData.Name,
			Index = modData.Index,
			Author = modData.AuthorDisplayName,
			IsDependency = isDependency
		};
		if (requiredBy != null)
		{
			data.RequiredBy.AddRange(requiredBy);
		}
		return data;
	}

	public static MissingModData FromData(ModuleShortDesc modData, int index, bool isDependency = true, string[]? requiredBy = null)
	{
		var data = new MissingModData(modData.UUID)
		{
			Name = modData.Name,
			Index = index,
			IsDependency = isDependency
		};
		if (requiredBy != null)
		{
			data.RequiredBy.AddRange(requiredBy);
		}
		return data;
	}
}
