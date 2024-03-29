using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ModManager.ViewModels;
using ModManager.Views;

using Splat;

namespace ModManager;
public partial class App : Application
{
	public override void Initialize() => AvaloniaXamlLoader.Load(this);

	public IClassicDesktopStyleApplicationLifetime? DesktopLifetime => ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

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
			desktop.MainWindow = new MainWindow
			{
				DataContext = new MainWindowViewModel(),
			};


			Locator.CurrentMutable.InitializeSplat();

			Locator.CurrentMutable.InitializeReactiveUI();

			RxApp.MainThreadScheduler = AvaloniaScheduler.Instance;
		}

		base.OnFrameworkInitializationCompleted();
	}
}