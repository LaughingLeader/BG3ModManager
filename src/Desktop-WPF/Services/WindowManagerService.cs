using AdonisUI;

using ModManager.Windows;

using ReactiveUI;

using System.Reactive.Concurrency;
using System.Reactive.Subjects;
using System.Windows;

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
				Window.Show();
				if (Window != _owner) Window.Owner = _owner;
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
	public WindowWrapper<AboutWindow> About { get; }
	public WindowWrapper<AppUpdateWindow> AppUpdate { get; }
	public WindowWrapper<CollectionDownloadWindow> CollectionDownload { get; }
	public WindowWrapper<HelpWindow> Help { get; }
	public WindowWrapper<ModPropertiesWindow> ModProperties { get; }
	public WindowWrapper<NxmDownloadWindow> NxmDownload { get; }
	public WindowWrapper<SettingsWindow> Settings { get; }
	public WindowWrapper<VersionGeneratorWindow> VersionGenerator { get; }
	public WindowWrapper<StatsValidatorWindow> StatsValidator { get; }

	public MainWindow MainWindow { get; }

	private readonly List<Window> _windows = [];

	public void UpdateColorScheme(Uri theme)
	{
		foreach (var window in _windows)
		{
			ResourceLocator.SetColorScheme(window.Resources, theme);
		}
	}

	public WindowManagerService(MainWindow main)
	{
		MainWindow = main;

		Main = new(main);
		About = new(main);
		AppUpdate = new(main);
		CollectionDownload = new(main);
		Help = new(main);
		ModProperties = new(main);
		NxmDownload = new(main);
		Settings = new(main);
		VersionGenerator = new(main);
		StatsValidator = new(main);

		_windows.Add(Main.Window);
		_windows.Add(About.Window);
		_windows.Add(AppUpdate.Window);
		_windows.Add(CollectionDownload.Window);
		_windows.Add(Help.Window);
		_windows.Add(ModProperties.Window);
		_windows.Add(NxmDownload.Window);
		_windows.Add(Settings.Window);
		_windows.Add(VersionGenerator.Window);
		_windows.Add(StatsValidator.Window);

		Settings.OnToggle.Subscribe(b =>
		{
			if (b)
			{
				if (Settings.Window.ViewModel == null)
				{
					Settings.Window.Init(Main.Window.ViewModel);
				}
			}
			Main.Window.ViewModel.Settings.SettingsWindowIsOpen = b;
		});

		AppServices.Settings.ManagerSettings.WhenAnyValue(x => x.DarkThemeEnabled).Subscribe(darkMode =>
		{
			var theme = !darkMode ? App.LightTheme : App.DarkTheme;
			UpdateColorScheme(theme);
		});
	}
}
