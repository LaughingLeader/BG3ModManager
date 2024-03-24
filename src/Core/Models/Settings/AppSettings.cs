using ModManager.Models.App;

namespace ModManager.Models.Settings;

public class AppSettings : ReactiveObject
{
	[Reactive] public DefaultPathwayData DefaultPathways { get; set; }
	[Reactive] public AppFeatures Features { get; set; }

	public string GetDirectory() => DivinityApp.GetAppDirectory("Resources");

	public AppSettings()
	{
		DefaultPathways = new DefaultPathwayData();
		Features = new AppFeatures();
	}
}
