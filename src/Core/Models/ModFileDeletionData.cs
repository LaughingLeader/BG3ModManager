﻿using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;

namespace DivinityModManager.Models
{
	public class ModFileDeletionData : ReactiveObject
	{
		[Reactive] public bool IsSelected { get; set; }
		[Reactive] public bool IsWorkshop { get; set; }
		[Reactive] public string FilePath { get; set; }
		[Reactive] public string DisplayName { get; set; }
		[Reactive] public string UUID { get; set; }
		[Reactive] public string Duplicates { get; set; }

		public static ModFileDeletionData FromMod(DivinityModData mod, bool isWorkshopMod = false, bool isDeletingDuplicates = false, List<DivinityModData> loadedMods = null)
		{
			var data = new ModFileDeletionData { FilePath = mod.FilePath, DisplayName = mod.DisplayName, IsSelected = true, UUID = mod.UUID, IsWorkshop = isWorkshopMod };
			if (isDeletingDuplicates && loadedMods != null)
			{
				var duplicatesStr = loadedMods.FirstOrDefault(x => x.UUID == mod.UUID)?.FilePath;
				if (!String.IsNullOrEmpty(duplicatesStr))
				{
					data.Duplicates = duplicatesStr;
				}
			}
			return data;
		}
	}
}
