using CrossSpeak;

using ModManager.Util;

using System.Runtime.InteropServices;

namespace ModManager.Services;
public class ScreenReaderService(IFileSystemService fs) : IScreenReaderService
{
	private static readonly string[] _dlls;
	private static bool _loadedDlls = false;

	private readonly IFileSystemService _fs = fs;

	static ScreenReaderService()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			_dlls = ["nvdaControllerClient64.dll", "SAAPI64.dll", "Tolk.dll"];
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			_dlls = ["libspeechdwrapper.so"];
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			_dlls = ["libspeak.dylib"];
		}
		else
		{
			_dlls = [];
		}
	}

	public bool IsScreenReaderActive()
	{
		if (EnsureInit(false))
		{
			return !string.IsNullOrWhiteSpace(CrossSpeakManager.Instance.DetectScreenReader());
		}
		return false;
	}

	public void Close()
	{
		if (CrossSpeakManager.Instance.IsLoaded())
		{
			CrossSpeakManager.Instance.Close();
		}
	}

	public void Silence()
	{
		if (CrossSpeakManager.Instance.IsLoaded())
		{
			CrossSpeakManager.Instance.Silence();
		}
	}

	private bool EnsureInit(bool trySAPI = false)
	{
		//Since we don't bother to organize dlls into _Lib in non-publish builds, skip this in debug mode
		if (!_loadedDlls)
		{
			var libPath = _fs.Path.Join(DivinityApp.GetAppDirectory(), "_Lib");
			foreach (var dll in _dlls)
			{
				var filePath = _fs.Path.Combine(libPath, dll);
				try
				{
					if (_fs.File.Exists(filePath))
					{
						NativeLibraryHelper.LoadLibrary(filePath);
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error loading '{dll}':\n{ex}");
				}
			}
			_loadedDlls = true;
		}
		if (!CrossSpeakManager.Instance.IsLoaded())
		{
			CrossSpeakManager.Instance.Initialize();
			if (trySAPI && !CrossSpeakManager.Instance.HasSpeech())
			{
				CrossSpeakManager.Instance.TrySAPI(true);
			}
		}
		return CrossSpeakManager.Instance.IsLoaded();
	}

	public void Output(string text, bool interrupt = true)
	{
		if (EnsureInit(true))
		{
			CrossSpeakManager.Instance.Output(text, interrupt);
		}
	}

	public void Speak(string text, bool interrupt = true)
	{
		if (EnsureInit(true))
		{
			CrossSpeakManager.Instance.Output(text, interrupt);
		}
	}
}
