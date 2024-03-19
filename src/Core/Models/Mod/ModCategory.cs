using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Mod;
public class ModCategory : IModEntry
{
	public ModEntryType EntryType => ModEntryType.Category;

	[Reactive] public string UUID { get; set; }
	[Reactive] public string DisplayName { get; set; }
	[Reactive] public int Index { get; set; }
	[Reactive] public bool IsActive { get; set; }

	public string Export(ModExportType exportType) => String.Empty;
}
