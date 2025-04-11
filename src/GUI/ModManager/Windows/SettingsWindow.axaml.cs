using ModManager.ViewModels;
using ModManager.ViewModels.Settings;
using ModManager.Views.Generated;

namespace ModManager.Windows;
public partial class SettingsWindow : ReactiveWindow<SettingsWindowViewModel>
{
	private static SettingsWindowTab ValidateIndex(int index)
	{
		var result = SettingsWindowTab.Default;
		if (index > 0)
		{
			result = (SettingsWindowTab)index;
		}
		return result;
	}

	public SettingsWindow()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			ViewModel = AppServices.Get<SettingsWindowViewModel>();

			if (ViewModel != null)
			{
				this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible);
				SettingsTabControl.GetObservable(TabIndexProperty).Select(ValidateIndex).BindTo(ViewModel, x => x.SelectedTabIndex);
				ViewModel.WhenAnyValue(x => x.SelectedTabIndex).Select(x => (int)x).BindTo(SettingsTabControl, x => x.TabIndex);

				var settings = AppServices.Settings.ManagerSettings;
				GeneralSettingsView.ViewModel = settings;
				UpdateSettingsView.ViewModel = settings.UpdateSettings;
				ExtenderSettingsView.ViewModel = settings.ExtenderSettings;
				ExtenderUpdateSettingsView.ViewModel = settings.ExtenderUpdaterSettings;
			}
		});
	}
}