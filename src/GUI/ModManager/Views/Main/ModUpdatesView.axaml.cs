using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class ModUpdatesView : ReactiveUserControl<ModUpdatesViewModel>
{
	public ModUpdatesView()
	{
		InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif
	}
}