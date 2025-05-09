using ModManager.Services;
using ModManager.ViewModels;
using ModManager.ViewModels.Main;
using ModManager.ViewModels.Settings;
using ModManager.ViewModels.Window;
using ModManager.Views;
using ModManager.Views.Main;
using ModManager.Windows;

namespace ModManager;

public static class AppServices
{
	public static ISettingsService Settings => Get<ISettingsService>()!;
	public static INexusModsService NexusMods => Get<INexusModsService>()!;
	public static IModManagerService Mods => Get<IModManagerService>()!;
	public static IPathwaysService Pathways => Get<IPathwaysService>()!;
	public static IModUpdaterService Updater => Get<IModUpdaterService>()!;
	public static IAppUpdaterService AppUpdater => Get<IAppUpdaterService>()!;
	public static ModImportService ModImporter => Get<ModImportService>()!;

	public static IGlobalCommandsService Commands => Get<IGlobalCommandsService>()!;
	public static IInteractionsService Interactions => Get<IInteractionsService>()!;
	public static IDialogService Dialog => Get<IDialogService>()!;
	public static IScreenReaderService ScreenReader => Get<IScreenReaderService>()!;
	public static AppKeysService Keybindings => Get<AppKeysService>()!;

	static AppServices()
	{
		var resolver = Locator.CurrentMutable;
		resolver.AddCommonServices();
		resolver.AddAppServices();

		SplatRegistrations.RegisterConstant<IBackgroundCommandService>(new BackgroundCommandService(DivinityApp.PIPE_ID));
		SplatRegistrations.RegisterLazySingleton<ModImportService>();
		SplatRegistrations.RegisterLazySingleton<IDialogService, DialogService>();
		SplatRegistrations.RegisterLazySingleton<AppKeysService>();

		//SplatRegistrations.Register<ModListDropHandler>();
		//SplatRegistrations.Register<ModListDragHandler>();

		SplatRegistrations.RegisterLazySingleton<MainWindowViewModel>();
		resolver.RegisterLazySingleton<IScreen>(() => ViewModelLocator.Main);

		SplatRegistrations.RegisterLazySingleton<ModOrderViewModel>();

		resolver.RegisterLazySingleton(() => new MainCommandBarViewModel(ViewModelLocator.Main, ViewModelLocator.ModOrder, ModImporter, Get<IFileSystemService>()));

		SplatRegistrations.RegisterLazySingleton<DeleteFilesViewModel>();
		SplatRegistrations.RegisterLazySingleton<ModUpdatesViewModel>();
		SplatRegistrations.RegisterLazySingleton<IProgressBarViewModel, ProgressBarViewModel>();

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
		SplatRegistrations.RegisterLazySingleton<PakFileExplorerWindowViewModel>();

		SplatRegistrations.RegisterLazySingleton<KeybindingsViewModel>();

		SplatRegistrations.RegisterLazySingleton<MessageBoxViewModel>();

		SplatRegistrations.RegisterLazySingleton<MainCommandBar>();
		SplatRegistrations.RegisterLazySingleton<DeleteFilesView>();
		SplatRegistrations.RegisterLazySingleton<ModOrderView>();
		SplatRegistrations.RegisterLazySingleton<ModUpdatesView>();

		SplatRegistrations.RegisterLazySingleton<MessageBoxView>();
		SplatRegistrations.RegisterLazySingleton<ProgressBarView>();

		SplatRegistrations.RegisterLazySingleton<SettingsWindow>();
		SplatRegistrations.RegisterLazySingleton<ModPropertiesWindow>();
		SplatRegistrations.RegisterLazySingleton<PakFileExplorerWindow>();
		SplatRegistrations.RegisterLazySingleton<StatsValidatorWindow>();
		SplatRegistrations.RegisterLazySingleton<VersionGeneratorWindow>();
		/*SplatRegistrations.RegisterLazySingleton<AboutWindow>();
		SplatRegistrations.RegisterLazySingleton<AppUpdateWindow>();
		SplatRegistrations.RegisterLazySingleton<CollectionDownloadWindow>();
		SplatRegistrations.RegisterLazySingleton<HelpWindow>();
		SplatRegistrations.RegisterLazySingleton<NxmDownloadWindow>();

		SplatRegistrations.RegisterLazySingleton<DeleteFilesConfirmationView>();
		SplatRegistrations.RegisterLazySingleton<ModUpdatesLayout>();*/

		//SplatRegistrations.RegisterLazySingleton<MainWindow>();

		SplatRegistrations.SetupIOC();
	}

	public static T Get<T>(string? contract = null)
	{
		return Locator.Current.GetService<T>(contract)!;
	}

	public static void Register<T>(Func<object> constructorCallback, string? contract = null)
	{
		Locator.CurrentMutable.Register(constructorCallback, typeof(T), contract);
	}

	public static void RegisterSingleton<T>(T instance, string? contract = null)
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
