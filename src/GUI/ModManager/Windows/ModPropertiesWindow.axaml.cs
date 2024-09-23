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

		if (!Design.IsDesignMode)
		{
			ViewModel = AppServices.Get<ModPropertiesWindowViewModel>();
		}

		this.WhenActivated(d =>
		{
			if(ViewModel != null)
			{
				this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);

				this.OneWayBind(ViewModel, x => x.IsEditorMod, x => x.ModTypeIconControl.Kind,
					b => b ? MaterialIconKind.Folder : MaterialIconKind.File);

				ViewModel.OKCommand.Subscribe(x => Hide());
				ViewModel.CancelCommand.Subscribe(x => Hide());
			}
		});
	}
}