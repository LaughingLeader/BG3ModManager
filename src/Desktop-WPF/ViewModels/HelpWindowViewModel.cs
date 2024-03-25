using ReactiveUI.Fody.Helpers;

namespace ModManager.ViewModels;

public class HelpWindowViewModel : BaseWindowViewModel
{
	[Reactive] public string WindowTitle { get; set; }
	[Reactive] public string HelpTitle { get; set; }
	[Reactive] public string HelpText { get; set; }

	public HelpWindowViewModel()
	{
		WindowTitle = "Help";
		HelpTitle = "";
		HelpText = "";
	}
}
