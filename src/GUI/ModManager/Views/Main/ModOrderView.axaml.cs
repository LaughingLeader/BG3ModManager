using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class ModOrderView : ReactiveUserControl<ModOrderViewModel>
{
	public ModOrderView()
	{
		InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif
	}
}