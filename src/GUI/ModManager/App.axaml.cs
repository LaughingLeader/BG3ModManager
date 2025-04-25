using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ModManager.Services;
using ModManager.Windows;

namespace ModManager;
public partial class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public IClassicDesktopStyleApplicationLifetime? DesktopLifetime => ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

	public void Shutdown(int errorCode = 0)
	{
		DesktopLifetime?.Shutdown(errorCode);
	}

	public static new App Current => (App)Application.Current!;

	public override void OnFrameworkInitializationCompleted()
	{
#if DEBUG
		if (Design.IsDesignMode)
		{
			base.OnFrameworkInitializationCompleted();
			return;
		}
#endif
		var desktop = DesktopLifetime;
		if (desktop != null)
		{
			var viewLocator = new ViewLocator();
			Locator.CurrentMutable.RegisterConstant<IViewLocator>(viewLocator);
			DataTemplates.Add(viewLocator);

			var mainWindow = new MainWindow();
			desktop.MainWindow = mainWindow;
			Locator.CurrentMutable.RegisterConstant(mainWindow);
			mainWindow.DataContext = ViewModelLocator.Main;

			Locator.CurrentMutable.InitializeSplat();
			Locator.CurrentMutable.InitializeReactiveUI();

			Locator.CurrentMutable.RegisterConstant(new WindowManagerService(mainWindow, AppServices.Interactions));

			RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
		}

		base.OnFrameworkInitializationCompleted();
	}
}