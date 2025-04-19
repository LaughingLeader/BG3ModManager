using System.Runtime.InteropServices;

namespace ModManager.Util;

public static partial class NativeLibraryHelper
{
	[LibraryImport("kernel32", EntryPoint = "LoadLibraryA")]
	public static partial nint LoadLibrary([MarshalAs(UnmanagedType.LPStr)] string lpFileName);
}