using ModManager.ViewModels.Mods;

namespace ModManager.Views.Mods;

public partial class ModPickerView : ReactiveUserControl<ModPickerViewModel>
{
    public ModPickerView()
    {
        InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif

		this.WhenActivated(d =>
		{
			this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);
		});
	}
}