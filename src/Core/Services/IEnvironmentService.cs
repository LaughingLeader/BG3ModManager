using System.IO;

namespace ModManager;

/// <summary>
/// Shortcuts for common application environment variables.
/// </summary>
public interface IEnvironmentService
{
	/// <inheritdoc cref="Environment.GetCommandLineArgs"/>
	IEnumerable<string> CommandLineArguments { get; }

	/// <inheritdoc cref="System.Reflection.AssemblyName.Version"/>
	Version AppVersion { get; }

	/// <inheritdoc cref="AppDomain.FriendlyName"/>
	string AppFriendlyName { get; }

	/// <inheritdoc cref="System.Reflection.AssemblyProductAttribute"/>
	string AppProductName { get; }

	/// <inheritdoc cref="Environment.SpecialFolder.ApplicationData"/>
	string ApplicationDataPath { get; }

	/// <inheritdoc cref="AppDomain.BaseDirectory"/>
	string AppDirectory { get; }
	
	/// <inheritdoc cref="Path.DirectorySeparatorChar"/>
	char DirectorySeparatorChar { get; }

	/// <inheritdoc cref="Path.AltDirectorySeparatorChar"/>
	char AltDirectorySeparatorChar { get; }

	/// <inheritdoc cref="DateTimeOffset.Now"/>
	DateTimeOffset Now { get; }

	/// <inheritdoc cref="DateTimeOffset.UtcNow"/>
	DateTimeOffset UtcNow { get; }

	/// <inheritdoc cref="Environment.ProcessorCount"/>
	int ProcessorCount { get; }

	/// <inheritdoc cref="OperatingSystem.IsLinux"/>
	bool IsLinux { get; }

	/// <inheritdoc cref="OperatingSystem.IsWindows"/>
	bool IsWindows { get; }

	/// <inheritdoc cref="OperatingSystem.IsMacOS"/>
	bool IsMacOS { get; }
}
