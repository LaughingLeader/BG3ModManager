using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using ModManager.Controls;
using ModManager.ViewModels;

namespace ModManager;

public partial class AboutWindow : HideWindowBase<AboutWindowViewModel>
{
	public AboutWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			ViewModel ??= AppServices.Get<AboutWindowViewModel>();
		});
	}
}