using ModManager.ViewModels.Main;

namespace ModManager.Views.Main;

public partial class DeleteFilesView : ReactiveUserControl<DeleteFilesViewModel>
{
	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if(e.Key == Key.Escape)
		{
			ViewModel?.Close();
		}
	}

	public DeleteFilesView()
	{
		InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif

		this.WhenActivated(d =>
		{
			Focus(NavigationMethod.Tab);
		});
	}
}