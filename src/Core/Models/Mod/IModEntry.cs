using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Models.Mod;

public interface IModEntry
{
	ModEntryType EntryType { get; }

	string UUID { get; }
	string DisplayName { get; }

	int Index { get; set; }
	bool IsActive { get; set; }

	string Export(ModExportType exportType);
}
