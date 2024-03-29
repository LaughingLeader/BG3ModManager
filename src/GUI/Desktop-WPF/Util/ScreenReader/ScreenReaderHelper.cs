using ModManager.Util;
using ModManager.Util.ScreenReader;

namespace ModManager;

public static class ScreenReaderHelper
{
	private static bool isLoaded = false;
	private static bool loadedLibraries = false;

	private static readonly string[] libraries = new string[3]
	{
		"nvdaControllerClient64.dll",
		"SAAPI64.dll",
		"Tolk.dll",
	};
	public static void Init()
	{
		if (!loadedLibraries)
		{
			/*
			 * Since the above DLLs are native, they need to be loaded manually from the _Lib directory since DLLImport and LoadLibrary in
			 * Tolk.dll won't be able to find nvdaControllerClient64.dll inside the _Lib folder.
			*/
			foreach (var lib in libraries)
			{
				NativeLibraryHelper.LoadLibrary("_Lib/" + lib);
			}
			loadedLibraries = true;
		}

		if (!isLoaded)
		{
			Tolk.Load();
		}
		isLoaded = Tolk.IsLoaded();
	}

	public static void Speak(string text, bool interrupt = true)
	{
		if (DivinityApp.IsScreenReaderActive())
		{
			if (!isLoaded)
			{
				Init();
			}
			if (isLoaded)
			{
				if (!Tolk.HasSpeech())
				{
					Tolk.TrySAPI(true);
				}
				Tolk.Output(text, interrupt);
			}
			//DivinityApp.Log($"Tolk.DetectScreenReader: {Tolk.DetectScreenReader()} Tolk.HasSpeech: {Tolk.HasSpeech()} Tolk.IsLoaded: {Tolk.IsLoaded()}");
		}
	}
}
