using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class ModOrderView : ReactiveUserControl<ModOrderViewModel>
{
	public ModOrderView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			if(ViewModel != null)
			{
				//ActiveModsGrid.Children.Add(new ModListView() { ViewModel = ViewModel.ActiveModsView });
			}
		});
	}
}