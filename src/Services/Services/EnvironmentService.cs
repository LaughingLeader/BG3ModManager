using System.IO;
using System.Reflection;

namespace ModManager.Services;

/// <inheritdoc cref="IEnvironmentService" />
public class EnvironmentService : IEnvironmentService
{
	private static string GetEntryAssemblyAttribute<T>(Func<T, string> value) where T : Attribute
	{
		if(Assembly.GetEntryAssembly() is Assembly assembly && Attribute.GetCustomAttribute(assembly, typeof(T)) is T attribute)
		{
			return value.Invoke(attribute);
		}
		return string.Empty;
	}

	/// <inheritdoc />
	public IEnumerable<string> CommandLineArguments => Environment.GetCommandLineArgs();

	/// <inheritdoc />
	public Version AppVersion => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version();

	/// <inheritdoc />
	public string AppFriendlyName => AppDomain.CurrentDomain.FriendlyName;

	/// <inheritdoc />
	public string AppProductName => GetEntryAssemblyAttribute<AssemblyProductAttribute>(a => a.Product);

	/// <inheritdoc />
	public string ApplicationDataPath => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

	/// <inheritdoc />
	public string AppDirectory => AppDomain.CurrentDomain.BaseDirectory;

	/// <inheritdoc />
	public char DirectorySeparatorChar => Path.DirectorySeparatorChar;

	/// <inheritdoc />
	public char AltDirectorySeparatorChar => Path.AltDirectorySeparatorChar;

	/// <inheritdoc />
	public DateTimeOffset Now => DateTimeOffset.Now;

	/// <inheritdoc />
	public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

	/// <inheritdoc />
	public int ProcessorCount => Environment.ProcessorCount;

	/// <inheritdoc />
	public bool IsLinux => OperatingSystem.IsLinux();

	/// <inheritdoc />
	public bool IsWindows => OperatingSystem.IsWindows();

	/// <inheritdoc />
	public bool IsMacOS => OperatingSystem.IsMacOS();
}
