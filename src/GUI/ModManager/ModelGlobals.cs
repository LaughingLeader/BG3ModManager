using Avalonia.Controls.Models.TreeDataGrid;

using DynamicData.Binding;

using ModManager.Models;
using ModManager.Models.Mod;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager
{
	public static class ModelGlobals
	{
		public static ObservableCollectionExtended<IModEntry> TestMods { get; }

		private static bool _addedDataFolderPak = false;

		private static string GetModFilePath(Random ran, bool isToolkitProject, string modFolder)
		{
			if(isToolkitProject)
			{
				return $"C:\\Games\\BG3\\Data\\Mods\\{modFolder}\\meta.lsx";
			}
			if(!_addedDataFolderPak || ran.Next(10) <= 1)
			{
				_addedDataFolderPak = true;
				return $"C:\\Games\\BG3\\Data\\{modFolder}.pak";
			}
			return $"C:\\Users\\TestUser\\AppData\\Local\\Larian Studios\\Baldur's Gate 3\\Mods\\{modFolder}.pak";
		}

		static ModelGlobals()
		{
			TestMods = [];

			var ran = new Random(1337);

			for (var i = 0; i < 30; i++)
			{
				var isToolkitProject = ran.Next(10) <= 3;
				var modNum = i + 1;
				var modName = $"Mod {modNum}";
				var uuid = Guid.NewGuid().ToString();
				var modFolder = $"Mod{modNum}_{uuid}";
				TestMods.Add(new ModEntry(new ModData(uuid)
				{ 
					Index = i, 
					Name = modName,
					Folder = modFolder,
					Description = $"Random mod {modNum}",
					Author = i % 2 <= 0 ? "LaughingLeader" : "Rando",
					FilePath = GetModFilePath(ran, isToolkitProject, modFolder),
					IsToolkitProject = isToolkitProject,
					IsLooseMod = isToolkitProject,
					LastModified = DateTimeOffset.Now,
					Version = new LarianVersion((ulong)ran.Next(0, 24), (ulong)ran.Next(0, 48), (ulong)ran.Next(0, 128), (ulong)ran.Next(0, 256))
				}));
			}
		}
	}
}
