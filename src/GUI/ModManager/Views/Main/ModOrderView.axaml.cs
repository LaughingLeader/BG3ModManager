using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class ModOrderView : ReactiveUserControl<ModOrderViewModel>
{
	public ModOrderView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			this.OneWayBind(ViewModel, vm => vm.CommandBar, x => x.CommandBar.ViewModel);
		});
	}
}