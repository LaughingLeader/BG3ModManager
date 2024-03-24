namespace ModManager.Models.App;

public class AppPathwayData
{
	public string Registry_32 { get; set; }
	public string Registry_64 { get; set; }
	public string AppID { get; set; }
	public string RootFolderName { get; set; }
	public string ExePath { get; set; }
}
public class DefaultPathwayData
{
	public AppPathwayData Steam { get; set; } = new AppPathwayData();
	public AppPathwayData GOG { get; set; } = new AppPathwayData();

	public string DocumentsGameFolder { get; set; }
	public string GameDataFolder { get; set; }
}
