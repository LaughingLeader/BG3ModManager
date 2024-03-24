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

		SplatRegistrations.RegisterLazySingleton<HttpClient, AppHttpClient>();

		SplatRegistrations.SetupIOC();

		return services;
	}
}
