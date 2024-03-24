using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ModManager.Services;

/// <inheritdoc />
/// Source: https://github.com/mysteryx93/HanumanInstituteApps
/// Etienne Charland aka Hanuman - MIT License
public class EnvironmentService : IEnvironmentService
{
	/// <inheritdoc />
	public IEnumerable<string> CommandLineArguments => Environment.GetCommandLineArgs();
	/// <inheritdoc />
	public Version AppVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();
	/// <inheritdoc />
	public string AppFriendlyName => AppDomain.CurrentDomain.FriendlyName;
	/// <inheritdoc />
	public string ApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
	/// <inheritdoc />
	public string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;
	/// <inheritdoc />
	public string SystemRootDirectory => Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System)) ?? string.Empty;
	/// <inheritdoc />
	public string ProgramFilesX86 => Environment.GetFolderPath(Environment.Is64BitOperatingSystem ? Environment.SpecialFolder.ProgramFilesX86 : Environment.SpecialFolder.ProgramFiles);
	/// <inheritdoc />
	public char DirectorySeparatorChar => Path.DirectorySeparatorChar;
	/// <inheritdoc />
	public char AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;
	/// <inheritdoc />
	public DateTime Now => DateTime.Now;
	/// <inheritdoc />
	public DateTime UtcNow => DateTime.UtcNow;
	/// <inheritdoc />
	public int ProcessorCount => Environment.ProcessorCount;
	/// <inheritdoc />
	public bool IsLinux => OperatingSystem.IsLinux();
	/// <inheritdoc />
	public bool IsWindows => OperatingSystem.IsWindows();
	/// <inheritdoc />
	public bool IsMacOS => OperatingSystem.IsMacOS();
	/// <inheritdoc />
	public IFormatProvider CurrentCulture => CultureInfo.CurrentCulture;

	/// <inheritdoc />
	public string GetRuntimeIdentifier()
	{
		if (OperatingSystem.IsWindows())
		{
			return RuntimeInformation.ProcessArchitecture == Architecture.X86 ? "win-x86" : "win-x64";
		}
		if (OperatingSystem.IsLinux())
		{
			return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "linux-arm64" : "linux-x64";
		}
		if (OperatingSystem.IsMacOS())
		{
			return RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64";
		}
		return string.Empty;
	}
}
