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

		static ModelGlobals()
		{
			TestMods = [];

			var ran = new Random(1337);

			for (var i = 0; i < 30; i++)
			{
				TestMods.Add(new ModEntry(new ModData()
				{ 
					Index = i, 
					Name = $"Mod {i+1}",
					Description = $"Random mod {i+1}",
					Author = i % 2 <= 0 ? "LaughingLeader" : "Rando",
					LastModified = DateTimeOffset.Now,
					Version = new LarianVersion((ulong)ran.Next(0, 24), (ulong)ran.Next(0, 48), (ulong)ran.Next(0, 128), (ulong)ran.Next(0, 256)),
					UUID = $"{i}" 
				}));
			}
		}
	}
}
