namespace ModManager.Models.Mod;

public class MissingModData
{
	public string? Name { get; set; }
	public int Index { get; set; }
	public string? UUID { get; set; }
	public string? Author { get; set; }
	public bool Dependency { get; set; }

	public override string ToString()
	{
		var str = "";
		if (Index > 0)
		{
			str += $"{Index}. ";
		}
		str += Name;
		if (!string.IsNullOrEmpty(Author))
		{
			str += " by " + Author;
		}
		if (Dependency) str += " (Dependency)";
		return str;
	}

	public static MissingModData FromData(ModData modData)
	{
		return new MissingModData
		{
			Name = modData.Name,
			UUID = modData.UUID,
			Index = modData.Index,
			Author = modData.AuthorDisplayName
		};
	}

	public static MissingModData FromData(ModLoadOrderEntry modData, List<ModLoadOrderEntry> orderList)
	{
		return new MissingModData
		{
			Name = modData.Name,
			UUID = modData.UUID,
			Index = orderList.IndexOf(modData)
		};
	}
}
