using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Linq;
using System.Windows.Input;

namespace ModManager.ViewModels;

public class NxmDownloadWindowViewModel : ReactiveObject
{
	[Reactive] public string Url { get; set; }

	public ICommand DownloadCommand { get; }

	public NxmDownloadWindowViewModel()
	{
		var canConfirm = this.WhenAnyValue(x => x.Url).Select(x => !String.IsNullOrEmpty(x) && x.StartsWith("nxm://"));
		DownloadCommand = ReactiveCommand.Create(() =>
		{
			Services.NexusMods.ProcessNXMLinkBackground(Url);
		}, canConfirm);
	}
}
