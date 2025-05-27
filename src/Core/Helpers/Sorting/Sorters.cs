using DynamicData.Binding;

using ModManager.Helpers.Sorting;
using ModManager.Models;
using ModManager.Models.Interfaces;
using ModManager.Models.Mod;
using ModManager.Models.Mod.Game;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager;
public static class Sorters
{
	public static readonly SortExpressionComparer<ModuleShortDesc> ModuleShortDesc = SortExpressionComparer<ModuleShortDesc>
		.Ascending(p => p.UUID.IsValid() && !DivinityApp.IgnoredMods.Lookup(p.UUID).HasValue)
		.ThenByAscending(p => p.Name ?? string.Empty);

	public static readonly SortExpressionComparer<ModData> ModDataDisplayName = SortExpressionComparer<ModData>
		.Ascending(x => x.DisplayName ?? string.Empty);

	public static readonly INamedComparer INamedIgnoreCase = new(StringComparison.OrdinalIgnoreCase);

	public static readonly SortExpressionComparer<ProfileData> Profile = SortExpressionComparer<ProfileData>.Ascending(p => p.FolderName != "Public").ThenByAscending(p => p.Name ?? string.Empty);

	public static readonly NaturalFileSortComparer FileIgnoreCase = new(StringComparison.OrdinalIgnoreCase);
}
