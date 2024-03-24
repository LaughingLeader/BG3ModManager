using System.IO.Abstractions;
using System.Net.Http;

using HanumanInstitute.Validators;

using ModManager.Services;

using Splat;

namespace ModManager;

public static class SplatContainerExtensions
{
	/// <summary>
	/// Registers Services classes into the IoC container.
	/// </summary>
	/// <param name="services">The IoC services container.</param>
	public static IMutableDependencyResolver AddCommonServices(this IMutableDependencyResolver services)
	{
		services.CheckNotNull(nameof(services));

		SplatRegistrations.RegisterLazySingleton<IEnvironmentService, EnvironmentService>();

		SplatRegistrations.RegisterLazySingleton<IFileSystem, FileSystem>();
		SplatRegistrations.RegisterLazySingleton<IFileSystemService, FileSystemService>();
		SplatRegistrations.RegisterLazySingleton<IFileWatcherService, FileWatcherService>();


		SplatRegistrations.SetupIOC();

		return services;
	}

	/// <summary>
	/// Registers Mod Manager-related service classes into the IoC container.
	/// </summary>
	/// <param name="services">The IoC services container.</param>
	public static IMutableDependencyResolver AddModManagerServices(this IMutableDependencyResolver services, string appPipeId)
	{
		services.CheckNotNull(nameof(services));

		SplatRegistrations.RegisterLazySingleton<ISettingsService, SettingsService>();
		SplatRegistrations.RegisterLazySingleton<IPathwaysService, PathwaysService>();

		SplatRegistrations.RegisterLazySingleton<INexusModsService, NexusModsService>();
		SplatRegistrations.RegisterLazySingleton<IGitHubService, GitHubService>();
		//SplatRegistrations.RegisterLazySingleton<ISteamWorkshopService, SteamWorkshopService>();

		SplatRegistrations.RegisterLazySingleton<IModManagerService, ModManagerService>();
		SplatRegistrations.RegisterLazySingleton<IModUpdaterService, ModUpdaterService>();

		SplatRegistrations.RegisterLazySingleton<IGameUtilitiesService, GameUtilitiesService>();

		SplatRegistrations.RegisterConstant<IBackgroundCommandService>(new BackgroundCommandService(appPipeId));

		SplatRegistrations.SetupIOC();

		return services;
	}
}
