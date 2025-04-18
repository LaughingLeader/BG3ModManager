﻿namespace DivinityModManager.Views;

public class AboutWindowBase : HideWindowBase<AboutWindowViewModel> { }

public class AboutWindowViewModel : ReactiveObject
{
	[Reactive] public string Title { get; set; }

	public AboutWindowViewModel()
	{
		Title = "About";
	}
}

/// <summary>
/// Interaction logic for AboutWindow.xaml
/// </summary>
public partial class AboutWindow : AboutWindowBase
{
	public AboutWindow()
	{
		InitializeComponent();

		ViewModel = new AboutWindowViewModel();

		this.WhenActivated(d =>
		{
			d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.TitleText.Text));
			d(this.OneWayBind(ViewModel, vm => vm.Title, v => v.Title));
		});
	}
}
