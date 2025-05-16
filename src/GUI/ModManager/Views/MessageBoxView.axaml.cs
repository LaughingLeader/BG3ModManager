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

			if(ViewModel != null)
			{
				ViewModel.WhenAnyValue(x => x.IsInput).Subscribe(b =>
				{
					if(ViewModel.IsVisible)
					{
						InputTextBox.Focus(NavigationMethod.Pointer);
						InputTextBox.SelectAll();
					}
				});
			}
		});
	}
}