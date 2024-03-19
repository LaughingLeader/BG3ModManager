using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DivinityModManager.Models.Mod;

public interface IDivinityModData
{
	string UUID { get; set; }
	string Name { get; set; }
	string Folder { get; set; }
	string MD5 { get; set; }
	LarianVersion Version { get; set; }
	public DateTimeOffset? LastModified { get; }
}