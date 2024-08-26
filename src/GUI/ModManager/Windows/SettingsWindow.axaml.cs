using Avalonia.Controls;

using ModManager.ViewModels;

namespace ModManager.Windows;
public partial class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
{
	public SettingsWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			if (ViewModel != null)
			{
				d(this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible));

				var settings = AppServices.Settings.ManagerSettings;
				GeneralSettingsView.ViewModel = settings;
				//settings.WhenAnyValue(x => x.ExtenderSettings).BindTo(UpdateSettingsView, x => x.ViewModel);
			}
		});
	}
}
