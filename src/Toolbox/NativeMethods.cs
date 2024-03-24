using System.Runtime.InteropServices;

namespace Toolbox;

public static partial class NativeMethods
{
	[LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryA")]
	public static partial nint LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);

	[LibraryImport("kernel32.dll", SetLastError = true)]
	public static partial nint GetProcAddress(nint hModule, [MarshalAs(UnmanagedType.LPStr)] string procedureName);

	[LibraryImport("kernel32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	public static partial bool FreeLibrary(nint libraryReference);

	public static void ThrowExceptionForLastWin32Error()
	{
		var errorCode = Marshal.GetHRForLastWin32Error();
		Marshal.ThrowExceptionForHR(errorCode);
	}
}
