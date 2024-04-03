using ModManager.ViewModels;

namespace ModManager.Views;
public partial class MessageBoxView : ReactiveUserControl<MessageBoxViewModel>
{
	public MessageBoxView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			if (!Design.IsDesignMode) ViewModel ??= ViewModelLocator.MessageBox;
		});
	}
}
