using Avalonia.Controls;
using Avalonia.Controls.Presenters;

using Material.Icons;

using ModManager.ViewModels;

namespace ModManager.Windows;
public partial class ModPropertiesWindow : ReactiveWindow<ModPropertiesWindowViewModel>
{
	public ModPropertiesWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			if(ViewModel != null)
			{
				d(this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible));

				d(this.OneWayBind(ViewModel, x => x.IsEditorMod, x => x.ModTypeIconControl.Kind, 
					b => b ? MaterialIconKind.Folder : MaterialIconKind.File));

				d(ViewModel.OKCommand.CombineLatest(ViewModel.CancelCommand).Subscribe(_ =>
				{
					Hide();
				}));

				//new TextPresenter().FontSize = new TextBox().FontSize;
			}
		});
	}
}
