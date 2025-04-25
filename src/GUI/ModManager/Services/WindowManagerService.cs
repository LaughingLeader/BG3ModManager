
using Avalonia.Styling;

using ModManager.ViewModels;
using ModManager.Windows;

using SukiUI;

using System.Reactive.Subjects;

namespace ModManager.Services;

public class WindowWrapper<T> where T : Window
{
	private readonly Window _owner;

	public T Window { get; }

	private readonly Subject<bool> _onToggle = new();
	public IObservable<bool> OnToggle => _onToggle;

	public void Toggle(bool setVisible)
	{
		RxApp.MainThreadScheduler.Schedule(() =>
		{
			_onToggle.OnNext(setVisible);

			if (setVisible)
			{
				if (Window != _owner)
				{
					Window.Show(_owner);
				}
				else
				{
					Window.Show();
				}
			}
			else
			{
				Window.Close();
			}
		});
	}

	public void Toggle() => Toggle(!Window.IsVisible);

	public WindowWrapper(Window ownerWindow = null)
	{
		Window = AppServices.Get<T>()!;
		_owner = ownerWindow ?? Window;
	}
}

public class WindowManagerService
{
	public WindowWrapper<MainWindow> Main { get; }
	//public WindowWrapper<AboutWindow> About { get; }
	//public WindowWrapper<AppUpdateWindow> AppUpdate { get; }
	//public WindowWrapper<CollectionDownloadWindow> CollectionDownload { get; }
	//public WindowWrapper<HelpWindow> Help { get; }
	//public WindowWrapper<ModPropertiesWindow> ModProperties { get; }
	//public WindowWrapper<NxmDownloadWindow> NxmDownload { get; }
	//public WindowWrapper<SettingsWindow> Settings { get; }
	//public WindowWrapper<VersionGeneratorWindow> VersionGenerator { get; }
	//public WindowWrapper<StatsValidatorWindow> StatsValidator { get; }

	public MainWindow MainWindow { get; }

	private readonly List<Window> _windows = [];

	public void UpdateColorScheme(ThemeVariant theme)
	{
		//foreach (var window in _windows)
		//{
		//	window.RequestedThemeVariant = theme;
		//	//ResourceLocator.SetColorScheme(window.Resources, theme);
		//}
	}

	public void RestoreSavedWindowPosition()
	{
		var window = MainWindow;
		var settings = AppServices.Settings.ManagerSettings;
		if (settings.SaveWindowLocation)
		{
			var windowSettings = settings.Window;
			window.WindowStartupLocation = WindowStartupLocation.Manual;

			var screens = window.Screens.All;
			var screenCount = screens.Count;
			if (screenCount > 0)
			{
				var screen = window.Screens.Primary;
				if (windowSettings.Screen > -1 && windowSettings.Screen < screenCount - 1)
				{
					screen = screens[windowSettings.Screen];
				}

				if (screen != null)
				{
					var x = Math.Max(screen.Bounds.X, Math.Min(screen.Bounds.Right, screen.Bounds.X + windowSettings.X));
					var y = Math.Max(screen.Bounds.Y, Math.Min(screen.Bounds.Bottom, screen.Bounds.Y + windowSettings.Y));

					window.Position = new PixelPoint((int)x, (int)y);
				}
			}

			if (windowSettings.Maximized)
			{
				window.WindowState = WindowState.Maximized;
			}
		}
	}

	public WindowManagerService(MainWindow main, IInteractionsService interactions)
	{
		MainWindow = main;

		Main = new(main);
		//About = new(main);
		//AppUpdate = new(main);
		//CollectionDownload = new(main);
		//Help = new(main);
		//ModProperties = new(main);
		//NxmDownload = new(main);
		//Settings = new(main);
		//VersionGenerator = new(main);
		//StatsValidator = new(main);

		_windows.Add(Main.Window);
		//_windows.Add(About.Window);
		//_windows.Add(AppUpdate.Window);
		//_windows.Add(CollectionDownload.Window);
		//_windows.Add(Help.Window);
		//_windows.Add(ModProperties.Window);
		//_windows.Add(NxmDownload.Window);
		//_windows.Add(Settings.Window);
		//_windows.Add(VersionGenerator.Window);
		//_windows.Add(StatsValidator.Window);

		//Settings.OnToggle.Subscribe(b =>
		//{
		//	if (b)
		//	{
		//		if (Settings.Window.ViewModel == null)
		//		{
		//			Settings.Window.Init(Main.Window.ViewModel);
		//		}
		//	}
		//	Main.Window.ViewModel.Settings.SettingsWindowIsOpen = b;
		//});

		AppServices.Settings.ManagerSettings.WhenAnyValue(x => x.DarkThemeEnabled).Subscribe(darkMode =>
		{
			var themeVariant = darkMode ? ThemeVariant.Dark : ThemeVariant.Light;
			App.Current.RequestedThemeVariant = themeVariant;
			SukiTheme.GetInstance().ChangeBaseTheme(themeVariant);
			UpdateColorScheme(themeVariant);
		});

		interactions.ValidateModStats.RegisterHandler(async context =>
		{
			var vm = AppServices.Get<StatsValidatorWindowViewModel>();
			if(vm != null)
			{
				context.SetOutput(true);
				await vm.StartValidationAsync(context.Input);
			}
			else
			{
				context.SetOutput(false);
			}
		});

		interactions.OpenValidateStatsResults.RegisterHandler(context =>
		{
			var window = AppServices.Get<StatsValidatorWindow>();
			if (window != null)
			{
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					window.ViewModel?.Load(context.Input);
					if (!window.IsVisible) window.Show(MainWindow);
				});
				context.SetOutput(true);
			}
			else
			{
				context.SetOutput(false);
			}
		});
	}
}
