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
		var env = new EnvironmentService();
		var fileSystem = new FileSystem();
		var fileSystemService = new FileSystemService(fileSystem);

		SplatRegistrations.RegisterConstant<IEnvironmentService>(env);

		SplatRegistrations.RegisterConstant<IFileSystem>(fileSystem);
		SplatRegistrations.RegisterConstant<IFileSystemService>(fileSystemService);
		SplatRegistrations.RegisterLazySingleton<IFileWatcherService, FileWatcherService>();

		var settingsService = new SettingsService(fileSystemService);
		SplatRegistrations.RegisterConstant<ISettingsService>(settingsService);
		SplatRegistrations.RegisterConstant<IPathwaysService>(new PathwaysService(settingsService));

		SplatRegistrations.RegisterLazySingleton<HttpClient, AppHttpClient>();

		SplatRegistrations.RegisterConstant<INexusModsService>(new NexusModsService(env));
		SplatRegistrations.RegisterConstant<IGitHubService>(new GitHubService(env));
		SplatRegistrations.RegisterConstant<IModioService>(new ModioService(fileSystemService));

		SplatRegistrations.SetupIOC();

		return services;
	}

	/// <summary>
	/// Registers standard Services classes with a DepedencyResolver.
	/// </summary>
	/// <param name="services">The IoC services container.</param>
	public static IMutableDependencyResolver AddAppServices(this IMutableDependencyResolver services)
	{
		SplatRegistrations.RegisterLazySingleton<IInteractionsService, InteractionsService>();
		SplatRegistrations.RegisterLazySingleton<IGlobalCommandsService, GlobalCommandsService>();

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
