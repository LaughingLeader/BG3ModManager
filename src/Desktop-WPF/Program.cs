using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;

namespace ModManager;

internal class Program
{
	private static SplashScreen _splash;

	private static bool EnsureSingleInstance(string[] args)
	{
		var procName = Process.GetCurrentProcess().ProcessName;
		if (Process.GetProcessesByName(procName).Length > 1)
		{
			if (args.Length > 0)
			{
				var argsMessage = String.Join(" ", args);
				try
				{
					using var pipe = new NamedPipeClientStream(".", DivinityApp.PIPE_ID,
					PipeDirection.Out, PipeOptions.WriteThrough, System.Security.Principal.TokenImpersonationLevel.Impersonation);
					pipe.Connect(500);
					using var sw = new StreamWriter(pipe, Encoding.UTF8);
					sw.Write(argsMessage);
					sw.Flush();
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error sending args to server:\n{ex}");
				}
#if DEBUG
				return true;
#endif
			}
#if !DEBUG
			return true;
#endif
		}
		return false;
	}

	[STAThread]
	static void Main(string[] args)
	{
		//Only close if args are passed in and we're a debug build,
		//otherwise always close if another instance exists in release
		if (EnsureSingleInstance(args))
		{
			System.Environment.Exit(0);
			return;
		}
		_splash = new SplashScreen("Resources/BG3MMSplashScreen.png");
		_splash.Show(false, false);

		var app = new App
		{
			Splash = _splash
		};
		app.InitializeComponent();
		app.Run();
	}
}
