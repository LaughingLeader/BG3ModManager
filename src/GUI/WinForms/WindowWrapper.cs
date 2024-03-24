using System.Windows;
using System.Windows.Interop;

namespace ModManager.WinForms;

public class WindowWrapper : System.Windows.Forms.IWin32Window
{
	public WindowWrapper(IntPtr handle)
	{
		_hwnd = handle;
	}

	public WindowWrapper(Window window)
	{
		_hwnd = new WindowInteropHelper(window).Handle;
	}

	public IntPtr Handle
	{
		get { return _hwnd; }
	}

	private readonly IntPtr _hwnd;
}
