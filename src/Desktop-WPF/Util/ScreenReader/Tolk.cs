using System.Runtime.InteropServices;

namespace ModManager.Util.ScreenReader;

public sealed class Tolk
{
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	private static extern void Tolk_Load();
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool Tolk_IsLoaded();
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	private static extern void Tolk_Unload();
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	private static extern void Tolk_TrySAPI(
		[MarshalAs(UnmanagedType.I1)] bool trySAPI);
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	private static extern void Tolk_PreferSAPI(
		[MarshalAs(UnmanagedType.I1)] bool preferSAPI);
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	private static extern IntPtr Tolk_DetectScreenReader();
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool Tolk_HasSpeech();
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool Tolk_HasBraille();
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool Tolk_Output(
		[MarshalAs(UnmanagedType.LPWStr)] String str,
		[MarshalAs(UnmanagedType.I1)] bool interrupt);
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool Tolk_Speak(
		[MarshalAs(UnmanagedType.LPWStr)] String str,
		[MarshalAs(UnmanagedType.I1)] bool interrupt);
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool Tolk_Braille(
		[MarshalAs(UnmanagedType.LPWStr)] String str);
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool Tolk_IsSpeaking();
	[DllImport("Tolk.dll", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
	[return: MarshalAs(UnmanagedType.I1)]
	private static extern bool Tolk_Silence();

	// Prevent construction
	private Tolk() { }

	public static void Load() { Tolk_Load(); }
	public static bool IsLoaded() { return Tolk_IsLoaded(); }
	public static void Unload() { Tolk_Unload(); }
	public static void TrySAPI(bool trySAPI) { Tolk_TrySAPI(trySAPI); }
	public static void PreferSAPI(bool preferSAPI) { Tolk_PreferSAPI(preferSAPI); }
	// Prevent the marshaller from freeing the unmanaged string
	public static String DetectScreenReader() { return Marshal.PtrToStringUni(Tolk_DetectScreenReader()); }
	public static bool HasSpeech() { return Tolk_HasSpeech(); }
	public static bool HasBraille() { return Tolk_HasBraille(); }
	public static bool Output(String str, bool interrupt = false) { return Tolk_Output(str, interrupt); }
	public static bool Speak(String str, bool interrupt = false) { return Tolk_Speak(str, interrupt); }
	public static bool Braille(String str) { return Tolk_Braille(str); }
	public static bool IsSpeaking() { return Tolk_IsSpeaking(); }
	public static bool Silence() { return Tolk_Silence(); }
}
