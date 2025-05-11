using System.ComponentModel.DataAnnotations;

namespace ModManager;
public enum LaunchGameType
{
	[Display(Name = "Default (Exe)", Description = "Open the game exe directly,\nand create a steam_appid.txt in the bin folder if it doesn't exist,\nallowing you to bypassing the launcher")]
	Exe,
	[Display(Name = "Steam", Description = "Open the game by running the Steam launch protocol ('steam://run/1086940')")]
	Steam,
	[Display(Name = "Custom", Description = "Open the game by opening a different file or protocol (ex. a batch file or protocol handler like playnite://playnite/start/id)")]
	Custom
}