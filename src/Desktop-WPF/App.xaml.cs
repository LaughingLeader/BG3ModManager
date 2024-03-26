using ModManager.Services;
using ModManager.Util;
using ModManager.ViewModels;
using ModManager.ViewModels.Main;
using ModManager.Windows;

using ReactiveUI;

using Splat;

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace ModManager;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public SplashScreen Splash { get; set; }

	public static WindowManagerService WM => AppServices.Get<WindowManagerService>();

	private static string _appDir;

	public static readonly Uri LightTheme = new("pack://application:,,,/BG3ModManager;component/Themes/Light.xaml", UriKind.Absolute);
	public static readonly Uri DarkTheme = new("pack://application:,,,/BG3ModManager;component/Themes/Dark.xaml", UriKind.Absolute);

	public App()
	{
		_appDir = DivinityApp.GetAppDirectory();
		Directory.SetCurrentDirectory(_appDir);
		// Fix for loading C++ dlls from _Lib
		AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

#if DEBUG
		RxApp.SuppressViewCommandBindingMessage = false;
#else
		RxApp.DefaultExceptionHandler = new RxExceptionHandler();
		RxApp.SuppressViewCommandBindingMessage = true;
#endif
	}

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		//For making date display use the current system's culture
		FrameworkElement.LanguageProperty.OverrideMetadata(
			typeof(FrameworkElement),
			new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));

		EventManager.RegisterClassHandler(typeof(Window), Window.PreviewMouseDownEvent, new MouseButtonEventHandler(OnPreviewMouseDown));

		var splashFade = new System.Threading.Thread(() =>
		{
			Splash.Close(TimeSpan.FromSeconds(1));
		});

		var mainWindow = new MainWindow() { ViewModel = ViewModelLocator.Main };
		Locator.CurrentMutable.RegisterConstant(mainWindow);

		AppServices.Get<IGameUtilitiesService>()?.AddGameProcessName(DivinityApp.GameExes);

		splashFade.Start();
		mainWindow.Show();
	}

	private static System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
	{
		var assyName = new AssemblyName(args.Name);

		var newPath = Path.Join(_appDir, "_Lib", assyName.Name);
		if (!newPath.EndsWith(".dll"))
		{
			newPath += ".dll";
		}

		if (File.Exists(newPath))
		{
			var assy = Assembly.LoadFile(newPath);
			return assy;
		}
		return null;
	}

	private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
	{
		DivinityApp.IsKeyboardNavigating = false;
	}
}
