using DivinityModManager.Models.Mod;

namespace DivinityModManager.Models;

public class DivinityMissingModData
{
	public string Name { get; set; }
	public int Index { get; set; }
	public string UUID { get; set; }
	public string Author { get; set; }
	public bool Dependency { get; set; }

	public override string ToString()
	{
		var str = "";
		if (Index > 0)
		{
			str += $"{Index}. ";
		}
		str += Name;
		if (!String.IsNullOrEmpty(Author))
		{
			str += " by " + Author;
		}
		if (Dependency) str += (" (Dependency)");
		return str;
	}

	public static DivinityMissingModData FromData(DivinityModData modData)
	{
		return new DivinityMissingModData
		{
			Name = modData.Name,
			UUID = modData.UUID,
			Index = modData.Index,
			Author = modData.AuthorDisplayName
		};
	}

	public static DivinityMissingModData FromData(DivinityLoadOrderEntry modData, List<DivinityLoadOrderEntry> orderList)
	{
		return new DivinityMissingModData
		{
			Name = modData.Name,
			UUID = modData.UUID,
			Index = orderList.IndexOf(modData)
		};
	}
}
