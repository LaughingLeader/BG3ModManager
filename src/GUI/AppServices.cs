using HanumanInstitute.MvvmDialogs;
using HanumanInstitute.MvvmDialogs.Wpf;

using ModManager.Services;
using ModManager.Util;
using ModManager.ViewModels;
using ModManager.ViewModels.Main;
using ModManager.Views.Main;
using ModManager.Windows;

using ReactiveUI;

using Splat;

using System.Reflection;

namespace ModManager;

public static class AppServices
{
	public static ISettingsService Settings => Get<ISettingsService>()!;
	public static INexusModsService NexusMods => Get<INexusModsService>()!;
	public static IModManagerService Mods => Get<IModManagerService>()!;
	public static IPathwaysService Pathways => Get<IPathwaysService>()!;
	public static IModUpdaterService Updater => Get<IModUpdaterService>()!;
	public static ModImportService ModImporter => Get<ModImportService>()!;

	static AppServices()
	{
		var resolver = Locator.CurrentMutable;
		resolver.AddCommonServices();

		var settingsService = new SettingsService();
		SplatRegistrations.RegisterConstant<ISettingsService>(settingsService);
		SplatRegistrations.RegisterConstant<IPathwaysService>(new PathwaysService(settingsService));

		SplatRegistrations.RegisterLazySingleton<INexusModsService, NexusModsService>();
		SplatRegistrations.RegisterLazySingleton<IGitHubService, GitHubService>();
		//SplatRegistrations.RegisterLazySingleton<ISteamWorkshopService, SteamWorkshopService>();

		SplatRegistrations.RegisterConstant<IModManagerService>(new ModManagerService());
		SplatRegistrations.RegisterConstant<IModUpdaterService>(new ModUpdaterService());

		SplatRegistrations.RegisterConstant<IGameUtilitiesService>(new GameUtilitiesService());

		SplatRegistrations.RegisterLazySingleton<IStatsValidatorService, StatsValidatorService>();

		SplatRegistrations.RegisterConstant<IBackgroundCommandService>(new BackgroundCommandService(DivinityApp.PIPE_ID));

		SplatRegistrations.RegisterLazySingleton<ModImportService>();

		var viewLocator = new ViewLocator();

		resolver.RegisterConstant<ReactiveUI.IViewLocator>(viewLocator);

		resolver.RegisterLazySingleton(() => (IDialogService)new DialogService(
			new DialogManager(viewLocator, new DialogFactory()),
			viewModelFactory: x => Locator.Current.GetService(x)));

		// POCO type warning suppression
		SplatRegistrations.RegisterConstant<ICreatesObservableForProperty>(new CustomPropertyResolver());

		SplatRegistrations.Register<ModListDropHandler>();
		SplatRegistrations.Register<ModListDragHandler>();

		SplatRegistrations.RegisterLazySingleton<MainWindowViewModel>();

		resolver.RegisterLazySingleton<IScreen>(() => ViewModelLocator.Main);

		SplatRegistrations.RegisterLazySingleton<DeleteFilesViewModel>();
		SplatRegistrations.RegisterLazySingleton<ModOrderViewModel>();
		SplatRegistrations.RegisterLazySingleton<ModUpdatesViewModel>();

		SplatRegistrations.RegisterLazySingleton<SettingsWindowViewModel>();
		SplatRegistrations.RegisterLazySingleton<AboutWindowViewModel>();
		SplatRegistrations.RegisterLazySingleton<AppUpdateWindowViewModel>();
		SplatRegistrations.RegisterLazySingleton<CollectionDownloadWindowViewModel>();
		SplatRegistrations.RegisterLazySingleton<HelpWindowViewModel>();
		SplatRegistrations.RegisterLazySingleton<ModPropertiesWindowViewModel>();
		SplatRegistrations.RegisterLazySingleton<NxmDownloadWindowViewModel>();
		SplatRegistrations.RegisterLazySingleton<StatsValidatorWindowViewModel>();
		SplatRegistrations.RegisterLazySingleton<VersionGeneratorViewModel>();
		SplatRegistrations.RegisterLazySingleton<ExportOrderToArchiveViewModel>();

		SplatRegistrations.RegisterLazySingleton<AboutWindow>();
		SplatRegistrations.RegisterLazySingleton<AppUpdateWindow>();
		SplatRegistrations.RegisterLazySingleton<CollectionDownloadWindow>();
		SplatRegistrations.RegisterLazySingleton<HelpWindow>();
		SplatRegistrations.RegisterLazySingleton<ModPropertiesWindow>();
		SplatRegistrations.RegisterLazySingleton<NxmDownloadWindow>();
		SplatRegistrations.RegisterLazySingleton<SettingsWindow>();
		SplatRegistrations.RegisterLazySingleton<StatsValidatorWindow>();
		SplatRegistrations.RegisterLazySingleton<VersionGeneratorWindow>();

		SplatRegistrations.RegisterLazySingleton<DeleteFilesConfirmationView>();
		SplatRegistrations.RegisterLazySingleton<ModOrderView>();
		SplatRegistrations.RegisterLazySingleton<ModUpdatesLayout>();

		//SplatRegistrations.RegisterLazySingleton<MainWindow>();

		SplatRegistrations.RegisterLazySingleton<WindowManagerService>();

		SplatRegistrations.SetupIOC();
	}

	public static T Get<T>(string contract = null)
	{
		return Locator.Current.GetService<T>(contract);
	}

	public static void Register<T>(Func<object> constructorCallback, string contract = null)
	{
		Locator.CurrentMutable.Register(constructorCallback, typeof(T), contract);
	}

	public static void RegisterSingleton<T>(T instance, string contract = null)
	{
		Locator.CurrentMutable.RegisterConstant(instance, typeof(T), contract);
	}

	/// <summary>
	/// Register a singleton which won't get created until the first user accesses it.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="constructorCallback"></param>
	/// <param name="contract"></param>
	public static void RegisterLazySingleton<T>(Func<object> constructorCallback, string contract = null)
	{
		Locator.CurrentMutable.RegisterLazySingleton(constructorCallback, typeof(T), contract);
	}
}
