using ModManager.Controls;
using ModManager.Models.Settings;
using ModManager.ViewModels;
using ModManager.ViewModels.Settings;
using ModManager.Views.Generated;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ModManager.Windows;
public partial class SettingsWindow : HideWindowBase<SettingsWindowViewModel>
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

	private static readonly Type _modManagerSettingsType = typeof(ModManagerSettings);

	private static bool TryGetSettingsEntry(string propName, [NotNullWhen(true)] out SettingsEntryAttribute? attribute)
	{
		attribute = null;
		if (_modManagerSettingsType.GetProperty(propName) is PropertyInfo prop && prop.GetCustomAttribute<SettingsEntryAttribute>() is SettingsEntryAttribute att)
		{
			attribute = att;
			return true;
		}
		return false;
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

				if(TryGetSettingsEntry(nameof(ModManagerSettings.DebugModeEnabled), out var debugModeAttribute))
				{
					ModDeveloperModeTextBlock.Text = debugModeAttribute.DisplayName;
					ToolTip.SetTip(ModDeveloperModeTextBlock, debugModeAttribute.ToolTip);
					ToolTip.SetTip(ModDeveloperModeCheckBox, debugModeAttribute.ToolTip);
				}

				if(TryGetSettingsEntry(nameof(ModManagerSettings.LogEnabled), out var logAttribute))
				{
					LoggingTextBlock.Text = logAttribute.DisplayName;
					ToolTip.SetTip(LoggingTextBlock, logAttribute.ToolTip);
					ToolTip.SetTip(LoggingCheckBox, logAttribute.ToolTip);
				}
			}
		});
	}
}