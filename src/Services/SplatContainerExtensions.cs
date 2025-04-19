using ModManager.Services;

using System.IO.Abstractions;
using System.Net.Http;

namespace ModManager;

public static class SplatContainerExtensions
{
	/// <summary>
	/// Registers standard Services classes with a DepedencyResolver.
	/// </summary>
	/// <param name="services">The IoC services container.</param>
	public static IMutableDependencyResolver AddCommonServices(this IMutableDependencyResolver services)
	{
		SplatRegistrations.RegisterLazySingleton<IEnvironmentService, EnvironmentService>();

		SplatRegistrations.RegisterLazySingleton<IFileSystem, FileSystem>();
		SplatRegistrations.RegisterLazySingleton<IFileSystemService, FileSystemService>();
		SplatRegistrations.RegisterLazySingleton<IFileWatcherService, FileWatcherService>();

		SplatRegistrations.RegisterLazySingleton<HttpClient, AppHttpClient>();

		SplatRegistrations.SetupIOC();

		return services;
	}

	/// <summary>
	/// Registers standard Services classes with a DepedencyResolver.
	/// </summary>
	/// <param name="services">The IoC services container.</param>
	public static IMutableDependencyResolver AddAppServices(this IMutableDependencyResolver services)
	{
		var settingsService = new SettingsService();

		SplatRegistrations.RegisterLazySingleton<IInteractionsService, InteractionsService>();
		SplatRegistrations.RegisterLazySingleton<IGlobalCommandsService, GlobalCommandsService>();

		SplatRegistrations.RegisterConstant<ISettingsService>(settingsService);
		SplatRegistrations.RegisterConstant<IPathwaysService>(new PathwaysService(settingsService));

		SplatRegistrations.RegisterLazySingleton<INexusModsService, NexusModsService>();
		SplatRegistrations.RegisterLazySingleton<IGitHubService, GitHubService>();
		//SplatRegistrations.RegisterLazySingleton<ISteamWorkshopService, SteamWorkshopService>();

		SplatRegistrations.RegisterLazySingleton<IAppUpdaterService, AppUpdaterService>();

		SplatRegistrations.RegisterLazySingleton<IModManagerService, ModManagerService>();
		SplatRegistrations.RegisterLazySingleton<IModUpdaterService, ModUpdaterService>();

		SplatRegistrations.RegisterConstant<IGameUtilitiesService>(new GameUtilitiesService());

		SplatRegistrations.RegisterLazySingleton<IStatsValidatorService, StatsValidatorService>();

		SplatRegistrations.RegisterLazySingleton<IScreenReaderService, ScreenReaderService>();

		SplatRegistrations.SetupIOC();

		return services;
	}
}
