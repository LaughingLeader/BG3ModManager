using ReactiveUI.Fody.Helpers;

namespace ModManager.ViewModels;

public class AboutWindowViewModel : BaseWindowViewModel
{
	[Reactive] public string Title { get; set; }

	public AboutWindowViewModel()
	{
		Title = "About";
	}
}
