namespace ModManager.ViewModels;

public class NxmDownloadWindowViewModel : ReactiveObject, IClosableViewModel, IRoutableViewModel
{
	#region IClosableViewModel/IRoutableViewModel
	public string UrlPathSegment => "nxmdownload";
	public IScreen HostScreen { get; }
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }
	#endregion

	[Reactive] public string? Url { get; set; }

	public RxCommandUnit DownloadCommand { get; }

	public NxmDownloadWindowViewModel(IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;
		CloseCommand = this.CreateCloseCommand();

		var canConfirm = this.WhenAnyValue(x => x.Url).Select(x => x.IsValid() && x.StartsWith("nxm://"));
		DownloadCommand = ReactiveCommand.Create(() =>
		{
			AppServices.NexusMods.ProcessNXMLinkBackground(Url!);
		}, canConfirm);
	}
}
