using ModManager.ViewModels;

namespace ModManager.Views;
public partial class MessageBoxView : ReactiveUserControl<MessageBoxViewModel>
{
	public MessageBoxView()
	{
		InitializeComponent();

		//Something is setting the DataContext to MainWindowViewModel briefly, which throws an exception if x:CompileBindings is enabled

		this.WhenActivated(d =>
		{
			if (!Design.IsDesignMode) ViewModel ??= ViewModelLocator.MessageBox;
		});
	}
}