using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class FooterView : ReactiveUserControl<FooterViewModel>
{
    public FooterView()
    {
        InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif
		this.WhenActivated(d =>
		{
			ViewModel ??= ViewModelLocator.Footer;
		});
	}
}