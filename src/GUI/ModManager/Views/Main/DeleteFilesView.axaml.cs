using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class DeleteFilesView : ReactiveUserControl<DeleteFilesViewModel>
{
	public DeleteFilesView()
	{
		InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif
	}
}