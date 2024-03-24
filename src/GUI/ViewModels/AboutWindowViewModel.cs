using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ModManager.ViewModels;

public class AboutWindowViewModel : ReactiveObject
{
	[Reactive] public string Title { get; set; }

	public AboutWindowViewModel()
	{
		Title = "About";
	}
}
