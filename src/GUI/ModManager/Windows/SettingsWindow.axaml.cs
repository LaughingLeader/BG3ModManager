using ModManager.ViewModels;
using ModManager.ViewModels.Settings;
using ModManager.Views.Generated;

namespace ModManager.Windows;
public partial class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
{
	public SettingsWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			ViewModel = AppServices.Get<SettingsWindowViewModel>();

			if (ViewModel != null)
			{
				d(this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible));


				var settings = AppServices.Settings.ManagerSettings;
				GeneralSettingsView.ViewModel = settings;
				UpdateSettingsView.ViewModel = settings.UpdateSettings;
				ExtenderSettingsView.ViewModel = settings.ExtenderSettings;
				ExtenderUpdateSettingsView.ViewModel = settings.ExtenderUpdaterSettings;
			}
		});
	}
}