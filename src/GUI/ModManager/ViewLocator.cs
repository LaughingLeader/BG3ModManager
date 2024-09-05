using ModManager.Models.Settings;
using ModManager.Models.View;
using ModManager.ViewModels;
using ModManager.ViewModels.Main;
using ModManager.ViewModels.Settings;
using ModManager.Views;
using ModManager.Views.Main;
using ModManager.Views.Settings;
using ModManager.Views.StatsValidator;

namespace ModManager;

public class ViewLocatorErrorView : TextBlock, IViewFor
{
	public object? ViewModel { get; set; }
}

public class ViewLocator : IViewLocator
{
	private static readonly Type _viewForType = typeof(IViewFor<>);

	public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
	{
		if (viewModel != null)
		{
			try
			{
				var viewType = _viewForType.MakeGenericType(viewModel.GetType());
				var registered = Locator.Current.GetService(viewType, contract);
				if (registered is IViewFor view)
				{
					return view;
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error fetching view: {ex}");
			}
		}
		return new ViewLocatorErrorView() { Text = $"Failed to find view for {viewModel}" };
	}

	private static void RegisterConstant<TViewModel, TView>(IMutableDependencyResolver resolver) where TViewModel : ReactiveObject where TView : IViewFor<TViewModel>
	{
		resolver.RegisterLazySingleton<IViewFor<TViewModel>>(() => AppServices.Get<TView>());
	}

	private static void Register<TViewModel, TView>(IMutableDependencyResolver resolver) where TViewModel : ReactiveObject where TView : IViewFor<TViewModel>, new()
	{
		resolver.Register(() => new TView());
	}

	static ViewLocator()
	{
		var resolver = Locator.CurrentMutable;

		RegisterConstant<MainCommandBarViewModel, MainCommandBar>(resolver);
		RegisterConstant<DeleteFilesViewModel, DeleteFilesView>(resolver);
		RegisterConstant<ModOrderViewModel, ModOrderView>(resolver);
		RegisterConstant<ModUpdatesViewModel, ModUpdatesView>(resolver);
		RegisterConstant<MessageBoxViewModel, MessageBoxView>(resolver);
		RegisterConstant<KeybindingsViewModel, KeybindingsView>(resolver);
		resolver.RegisterLazySingleton(() => (IViewFor<IProgressBarViewModel>)AppServices.Get<ProgressBarView>());

		//resolver.RegisterLazySingleton(() => (IViewFor<SettingsWindowViewModel>)AppServices.Settings);
		//resolver.RegisterLazySingleton(() => (IViewFor<ModManagerSettings>)AppServices.Settings.ManagerSettings);
		//resolver.RegisterLazySingleton(() => (IViewFor<ModManagerUpdateSettings>)AppServices.Settings.ManagerSettings.UpdateSettings);
		//resolver.RegisterLazySingleton(() => (IViewFor<ScriptExtenderSettings>)AppServices.Settings.ManagerSettings.ExtenderSettings);
		//resolver.RegisterLazySingleton(() => (IViewFor<ScriptExtenderUpdateConfig>)AppServices.Settings.ManagerSettings.ExtenderUpdaterSettings);

		//Register<DownloadActivityBarViewModel, DownloadActivityBar>(resolver);
		Register<StatsValidatorFileResults, StatsValidatorFileEntryView>(resolver);
		Register<StatsValidatorErrorEntry, StatsValidatorEntryView>(resolver);
		Register<StatsValidatorLineText, StatsValidatorLineView>(resolver);
	}
}