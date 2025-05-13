using ModManager.Controls;
using ModManager.ViewModels;

namespace ModManager;

public partial class NexusModsCollectionDownloadWindow : HideWindowBase<NexusModsCollectionDownloadWindowViewModel>
{
	public NexusModsCollectionDownloadWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			ViewModel ??= AppServices.Get<NexusModsCollectionDownloadWindowViewModel>();
		});
	}
}