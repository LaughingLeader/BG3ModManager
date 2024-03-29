namespace ModManager.ViewModels;

public class HelpWindowViewModel : ReactiveObject, IClosableViewModel, IRoutableViewModel
{
	#region IClosableViewModel/IRoutableViewModel
	[Reactive] public string WindowTitle { get; set; }
	[Reactive] public string HelpTitle { get; set; }
	[Reactive] public string HelpText { get; set; }
	#endregion

	//IClosableViewModel
	public string UrlPathSegment => "help";
	public IScreen HostScreen { get; }
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }

	public HelpWindowViewModel(IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;
		CloseCommand = this.CreateCloseCommand();

		WindowTitle = "Help";
		HelpTitle = "";
		HelpText = "";
	}
}
