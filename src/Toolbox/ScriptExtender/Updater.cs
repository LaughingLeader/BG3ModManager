using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Toolbox.ScriptExtender;

public class Updater : IDisposable
{
	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
	private delegate void SEUpdaterInitialize([MarshalAs(UnmanagedType.LPStr)] string? configPath);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void SESetGameVersion(int major, int minor, int revision, int build);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate bool SEUpdate();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void SEUpdaterShowConsole();

	[UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
	private delegate void SEUpdaterGetError([MarshalAs(UnmanagedType.LPStr)] ref string buffer, uint length);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	private delegate void SEUpdaterShutdown();

	private readonly SEUpdaterInitialize? _initializeUpdaterWrapper;
	private readonly SESetGameVersion? _setGameVersionWrapper;
	private readonly SEUpdate? _updateWrapper;
	private readonly SEUpdaterShowConsole? _updaterShowConsoleWrapper;
	private readonly SEUpdaterGetError? _updaterGetErrorWrapper;
	private readonly SEUpdaterShutdown? _updaterShutdownWrapper;

	private readonly string _updaterPath;
	private readonly IntPtr _dll;

	private bool _consoleIsOpen;

	private readonly bool _loaded = false;
	public bool IsLoaded => _loaded;

#if DEBUG
	/*[DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SymInitialize(IntPtr hProcess, string UserSearchPath, [MarshalAs(UnmanagedType.Bool)] bool fInvadeProcess);

	[DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SymCleanup(IntPtr hProcess);

	[DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	public static extern ulong SymLoadModuleEx(IntPtr hProcess, IntPtr hFile,
		 string ImageName, string ModuleName, long BaseOfDll, int DllSize, IntPtr Data, int Flags);

	[DllImport("dbghelp.dll", SetLastError = true, CharSet = CharSet.Unicode)]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static extern bool SymEnumerateSymbols64(IntPtr hProcess,
	   ulong BaseOfDll, SymEnumerateSymbolsProc64 EnumSymbolsCallback, IntPtr UserContext);

	public delegate bool SymEnumerateSymbolsProc64(string SymbolName,
		  ulong SymbolAddress, uint SymbolSize, IntPtr UserContext);

	public static bool EnumSyms(string name, ulong address, uint size, IntPtr context)
	{
		Console.WriteLine(name);
		return true;
	}*/
#endif

	private T? GetWrapper<T>(string funcName) where T : Delegate
	{
		var address = NativeMethods.GetProcAddress(_dll, funcName);
		if (address != IntPtr.Zero)
		{
			return Marshal.GetDelegateForFunctionPointer<T>(address);
		}
		return null;
	}

	public Updater(string updaterPath, string? binPath)
	{
		_updaterPath = updaterPath;
		_dll = NativeMethods.LoadLibrary(_updaterPath);

		if (_dll != IntPtr.Zero)
		{
#if DEBUG
			/*IntPtr hCurrentProcess = Process.GetCurrentProcess().Handle;
			var status = SymInitialize(hCurrentProcess, null, false);
			var baseOfDll = SymLoadModuleEx(hCurrentProcess, IntPtr.Zero, updaterPath, null, 0, 0, IntPtr.Zero, 0);
			if (SymEnumerateSymbols64(hCurrentProcess, baseOfDll, EnumSyms, IntPtr.Zero) == false)
			{
				Console.WriteLine("Failed to enum symbols.");
			}*/
#endif
			_initializeUpdaterWrapper = GetWrapper<SEUpdaterInitialize>("SEUpdaterInitialize");
			_setGameVersionWrapper = GetWrapper<SESetGameVersion>("SESetGameVersion");
			_updateWrapper = GetWrapper<SEUpdate>("SEUpdate");
			_updaterShowConsoleWrapper = GetWrapper<SEUpdaterShowConsole>("SEUpdaterShowConsole");
			_updaterGetErrorWrapper = GetWrapper<SEUpdaterGetError>("SEUpdaterGetError");
			_updaterShutdownWrapper = GetWrapper<SEUpdaterShutdown>("SEUpdaterShutdown");

			_loaded = (_initializeUpdaterWrapper != null && _setGameVersionWrapper != null
				&& _updateWrapper != null && _updaterGetErrorWrapper != null && _updaterShutdownWrapper != null);
			if (_loaded)
			{
				Console.WriteLine($"Initializing updater with path '{binPath}'");
				_initializeUpdaterWrapper!(binPath);
			}
		}
	}

	public bool SetGameVersion(string exePath)
	{
		if (!_loaded) return false;
		try
		{
			var fvi = FileVersionInfo.GetVersionInfo(exePath);
			if (fvi != null)
			{
				Console.WriteLine($"Setting game version to {fvi.FileMajorPart}.{fvi.FileMinorPart}.{fvi.FileBuildPart}.{fvi.FilePrivatePart}");
				_setGameVersionWrapper!(fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart, fvi.FilePrivatePart);
				return true;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error setting game version:\n{ex}");
		}
		return false;
	}


	public bool ShowConsoleWindow()
	{
		if (!_loaded) return false;
		_updaterShowConsoleWrapper!();
		_consoleIsOpen = true;
		return true;
	}


	public bool Update()
	{
		if (!_loaded) return false;
		_updateWrapper!();
		return true;
	}


	public string GetError()
	{
		var error = "";
		if (!_loaded) return error;
		_updaterGetErrorWrapper!(ref error, 10);
		return error;
	}


	private void Shutdown()
	{
		if (!_loaded) return;
		Console.WriteLine("Shutting down the updater.");
		_updaterShutdownWrapper!();
	}

	private bool _disposed;

	protected virtual void Dispose(bool disposing)
	{
		if (_disposed) return;
		if (disposing)
		{
			if (_dll != IntPtr.Zero)
			{
				if (_loaded) Shutdown();
				if (_consoleIsOpen)
				{
					Console.WriteLine("Console window is open. Good luck.");
				}
				NativeMethods.FreeLibrary(_dll);
				Console.WriteLine("Library freed.");
			}
			_disposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}
}
