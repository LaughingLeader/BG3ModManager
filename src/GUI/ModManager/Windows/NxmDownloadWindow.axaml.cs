using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using ModManager.Controls;
using ModManager.ViewModels;

namespace ModManager;

public partial class NxmDownloadWindow : HideWindowBase<NxmDownloadWindowViewModel>
{
    public NxmDownloadWindow()
    {
        InitializeComponent();

		this.WhenActivated(d =>
		{
			ViewModel ??= AppServices.Get<NxmDownloadWindowViewModel>();
		});
	}
}