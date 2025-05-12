namespace ModManager.ViewModels;

public class AboutWindowViewModel : ReactiveObject, IClosableViewModel, IRoutableViewModel
{
	#region IClosableViewModel/IRoutableViewModel
	public string UrlPathSegment => "about";
	public IScreen HostScreen { get; }
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }
	#endregion

	[Reactive] public string Title { get; set; }
	[Reactive] public Uri RepoUrl { get; set; }
	[Reactive] public Uri AuthorUrl { get; set; }
	[Reactive] public Uri LicenseUrl { get; set; }
	[Reactive] public Uri DonationUrl { get; set; }
	[Reactive] public Uri IssuesUrl { get; set; }
	[Reactive] public Uri ChangelogUrl { get; set; }

	public AboutWindowViewModel(IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;
		CloseCommand = this.CreateCloseCommand();

		Title = "About";
		RepoUrl = new Uri(DivinityApp.URL_REPO);
		AuthorUrl = new Uri(DivinityApp.URL_AUTHOR);
		LicenseUrl = new Uri(DivinityApp.URL_LICENSE);
		DonationUrl = new Uri(DivinityApp.URL_DONATION);
		IssuesUrl = new Uri(DivinityApp.URL_ISSUES);
		ChangelogUrl = new Uri(DivinityApp.URL_CHANGELOG);
	}
}
