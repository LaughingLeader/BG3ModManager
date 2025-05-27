using Avalonia.Media;

using ModManager.Controls;
using ModManager.ViewModels.Window;

namespace ModManager.Windows;

public partial class PakFileExplorerWindow : HideWindowBase<PakFileExplorerWindowViewModel>
{
	public PakFileExplorerWindow()
	{
		InitializeComponent();

		ViewModel = AppServices.Get<PakFileExplorerWindowViewModel>();

		this.WhenActivated(d =>
		{
			if (ViewModel != null)
			{
				var commands = AppServices.Commands;

				this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);
			}
		});
	}
}