using AdonisUI;

using AutoUpdaterDotNET;

using DivinityModManager.AppServices;
using DivinityModManager.Controls;
using DivinityModManager.Util;
using DivinityModManager.Util.ScreenReader;
using DivinityModManager.ViewModels;
using DivinityModManager.Views;

using DynamicData;

using ReactiveUI;

using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DivinityModManager.Windows;

public class MainWindowBase : HideWindowBase<MainWindowViewModel>
{
	//TODO Hide to notification tray if option is enabled

	public MainWindowBase() : base()
	{
		HideOnEscapeKey = false;
	}

	public override void OnClosing(object sender, CancelEventArgs e)
	{
		//base.OnClosing(sender, e);
	}
}

public partial class MainWindow : MainWindowBase
{
	[LibraryImport("user32")] public static partial int FlashWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool bInvert);

	private readonly System.Windows.Interop.WindowInteropHelper _hwnd;

	public LogTraceListener DebugLogListener { get; private set; }

	private readonly string _logsDir;
	private readonly string _logFileName;

	public AlertBar AlertBar => MainView.AlertBar;
	public Style MessageBoxStyle => MainWindowMessageBox.Style;

	public void ToggleLogging(bool enabled)
	{
		if (enabled || ViewModel?.DebugMode == true)
		{
			if (DebugLogListener == null)
			{
				if (!Directory.Exists(_logsDir))
				{
					Directory.CreateDirectory(_logsDir);
					DivinityApp.Log($"Creating logs directory: {_logsDir}");
				}

				DebugLogListener = new LogTraceListener(_logFileName, "DebugLogListener");
				Trace.Listeners.Add(DebugLogListener);
				Trace.AutoFlush = true;
			}
		}
		else if (DebugLogListener != null && ViewModel?.DebugMode != true)
		{
			Trace.Listeners.Remove(DebugLogListener);
			DebugLogListener.Dispose();
			DebugLogListener = null;
			Trace.AutoFlush = false;
		}
	}

	public void DisplayError(string msg)
	{
		ToggleLogging(true);
		DivinityApp.Log(msg);
		var result = Xceed.Wpf.Toolkit.MessageBox.Show(msg,
			"Open the logs folder?",
			System.Windows.MessageBoxButton.YesNo,
			System.Windows.MessageBoxImage.Error,
			System.Windows.MessageBoxResult.No, MessageBoxStyle);
		if (result == System.Windows.MessageBoxResult.Yes)
		{
			FileUtils.TryOpenPath(DivinityApp.GetAppDirectory("_Logs"));
		}
	}

	public void DisplayError(string msg, string caption, bool showLog = false)
	{
		if (!showLog)
		{
			Xceed.Wpf.Toolkit.MessageBox.Show(msg, caption,
			System.Windows.MessageBoxButton.OK,
			System.Windows.MessageBoxImage.Warning,
			System.Windows.MessageBoxResult.OK, MessageBoxStyle);
		}
		else
		{
			DisplayError(msg);
		}
	}

	private void OnUIException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
	{
		e.Handled = true;
		ToggleLogging(true);
		var doShutdown = ViewModel?.IsInitialized != true;
		var shutdownText = doShutdown ? " The program will close." : "";
		DivinityApp.Log($"An exception in the UI occurred.{shutdownText}\n{e.Exception}");

		var result = Xceed.Wpf.Toolkit.MessageBox.Show($"An exception in the UI occurred.{shutdownText}\n{e.Exception}",
			"Open the logs folder?",
			System.Windows.MessageBoxButton.YesNo,
			System.Windows.MessageBoxImage.Error,
			System.Windows.MessageBoxResult.No, MessageBoxStyle);
		if (result == System.Windows.MessageBoxResult.Yes)
		{
			FileUtils.TryOpenPath(DivinityApp.GetAppDirectory("_Logs"));
		}

		//Shutdown if we had an exception when loading.
		if (doShutdown)
		{
			App.Current.Shutdown(1);
		}
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
	{
		ToggleLogging(true);
		var doShutdown = ViewModel?.IsInitialized != true;
		var shutdownText = doShutdown ? " The program will close." : "";

		DivinityApp.Log($"An unhandled exception occurred.{shutdownText}\n{e.ExceptionObject}");
		var result = Xceed.Wpf.Toolkit.MessageBox.Show($"An unhandled exception occurred.{shutdownText}\n{e.ExceptionObject}",
			"Open the logs folder?",
			System.Windows.MessageBoxButton.YesNo,
			System.Windows.MessageBoxImage.Error,
			System.Windows.MessageBoxResult.No, MessageBoxStyle);
		if (result == System.Windows.MessageBoxResult.Yes)
		{
			FileUtils.TryOpenPath(DivinityApp.GetAppDirectory("_Logs"));
		}

		if (doShutdown)
		{
			App.Current.Shutdown(1);
		}
	}

	void OnStateChanged(object sender, EventArgs e)
	{
		if (ViewModel?.Settings?.Loaded == true)
		{
			var windowSettings = ViewModel.Settings.Window;
			windowSettings.Maximized = WindowState == WindowState.Maximized;
			var screen = System.Windows.Forms.Screen.FromHandle(_hwnd.Handle);
			windowSettings.Screen = System.Windows.Forms.Screen.AllScreens.IndexOf(screen);
			ViewModel.QueueSave();
		}
	}

	void OnLocationChanged(object sender, EventArgs e)
	{
		if (ViewModel?.Settings?.Loaded == true)
		{
			var windowSettings = ViewModel.Settings.Window;
			var screen = System.Windows.Forms.Screen.FromHandle(_hwnd.Handle);
			windowSettings.X = Left - screen.WorkingArea.Left;
			windowSettings.Y = Top - screen.WorkingArea.Top;
			windowSettings.Screen = System.Windows.Forms.Screen.AllScreens.IndexOf(screen);
			ViewModel.QueueSave();
		}
	}

	public void ToggleWindowPositionSaving(bool b)
	{
		if (b)
		{
			StateChanged += OnStateChanged;
			LocationChanged += OnLocationChanged;
		}
		else
		{
			StateChanged -= OnStateChanged;
			LocationChanged -= OnLocationChanged;
		}
	}

	private static System.Windows.Shell.TaskbarItemProgressState BoolToTaskbarItemProgressState(bool b)
	{
		return b ? System.Windows.Shell.TaskbarItemProgressState.Normal : System.Windows.Shell.TaskbarItemProgressState.None;
	}

	protected override System.Windows.Automation.Peers.AutomationPeer OnCreateAutomationPeer()
	{
		return new CachedAutomationPeer(this);
	}


	private void OnClosing()
	{
		ViewModel.SaveSettings();
		Application.Current.Shutdown();
	}

	private void AutoUpdater_OnClosing()
	{
		ViewModel.Settings.LastUpdateCheck = DateTimeOffset.Now.ToUnixTimeSeconds();
		OnClosing();
	}

	private readonly WindowInteropHelper _wih;

	public void FlashTaskbar()
	{
		FlashWindow(_wih.Handle, true);
	}

	public MainWindow()
	{
		InitializeComponent();
		_hwnd = new System.Windows.Interop.WindowInteropHelper(this);

		_logsDir = DivinityApp.GetAppDirectory("_Logs");
		var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
#if DEBUG
		_logFileName = Path.Join(_logsDir, "debug_" + DateTime.Now.ToString(sysFormat + "_HH-mm-ss") + ".log");
#else
		_logFileName = Path.Join(_logsDir, "release_" + DateTime.Now.ToString(sysFormat + "_HH-mm-ss") + ".log");
#endif

		Application.Current.DispatcherUnhandledException += OnUIException;
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

		DivinityApp.DateTimeColumnFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
		DivinityApp.DateTimeTooltipFormat = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;

		ViewModel = new MainWindowViewModel();

		Services.RegisterSingleton(new WindowManagerService(this));

		if (File.Exists(Path.Join(System.AppDomain.CurrentDomain.BaseDirectory, "debug")))
		{
			ViewModel.DebugMode = true;
			ToggleLogging(true);
			DivinityApp.Log("Enable logging due to the debug file next to the exe.");
		}

		this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

		Closed += (o, e) => OnClosing();
		AutoUpdater.ApplicationExitEvent += AutoUpdater_OnClosing;
		AutoUpdater.HttpUserAgent = DivinityApp.URL_AUTHOR;
		AutoUpdater.RunUpdateAsAdmin = false;

		DataContext = ViewModel;

		_wih = new WindowInteropHelper(this);

		DivinityInteractions.OpenModProperties.RegisterHandler(interaction =>
		{
			var modProperties = App.WM.ModProperties;
			modProperties.Window.ViewModel.SetMod(interaction.Input);
			modProperties.Toggle(true);
			interaction.SetOutput(true);
		});

		this.WhenAnyValue(x => x.ViewModel).BindTo(this, view => view.MainView.ViewModel);

		MainBusyIndicator.Visibility = Visibility.Visible;
		MainBusyIndicator.IsBusy = true;
		MainView.Visibility = Visibility.Hidden;

		this.OneWayBind(ViewModel, vm => vm.MainProgressIsActive, view => view.MainBusyIndicator.IsBusy);
		this.OneWayBind(ViewModel, vm => vm.MainProgressTitle, view => view.ProgressTitleTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.MainProgressWorkText, view => view.ProgressWorkTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.MainProgressValue, view => view.ProgressValueBar.Value);

		this.OneWayBind(ViewModel, vm => vm.CanCancelProgress, view => view.ProgressCancelButton.Visibility, PropertyConverters.BoolToVisibility);
		this.BindCommand(ViewModel, vm => vm.CancelMainProgressCommand, view => view.ProgressCancelButton);

		this.WhenAnyValue(x => x.ViewModel.Title).BindTo(this, view => view.Title);
		this.OneWayBind(ViewModel, vm => vm.MainProgressIsActive, view => view.TaskbarItemInfo.ProgressState, BoolToTaskbarItemProgressState);
		this.WhenAnyValue(x => x.ViewModel.MainProgressValue).BindTo(this, view => view.TaskbarItemInfo.ProgressValue);

		ViewModel.Keys.OpenPreferences.AddAction(() => App.WM.Settings.Toggle());
		ViewModel.Keys.OpenKeybindings.AddAction(() =>
		{
			App.WM.Settings.Toggle();
			if (App.WM.Settings.Window.IsVisible) App.WM.Settings.Window.ViewModel.SelectedTabIndex = SettingsWindowTab.Keybindings;
		});
		ViewModel.Keys.OpenAboutWindow.AddAction(() => App.WM.About.Toggle());
		ViewModel.Keys.ToggleVersionGeneratorWindow.AddAction(() => App.WM.VersionGenerator.Toggle());

		//Allow launching the game if single instance mode is enabled, but the shift key is held
		Observable.Merge(
			Observable.FromEventPattern<KeyEventArgs>(this, nameof(KeyDown)),
			Observable.FromEventPattern<KeyEventArgs>(this, nameof(KeyUp))
		)
		.Select(e => (e.EventArgs.Key == Key.LeftShift || e.EventArgs.Key == Key.RightShift) && e.EventArgs.IsDown)
		.BindTo(ViewModel, x => x.CanForceLaunchGame);

		this.WhenActivated(d =>
		{
			ViewModel.OnViewActivated(this, MainView);
			MainView.RegisterKeyBindings(this);
			Dispatcher.BeginInvoke(ViewModel.LoadInitial, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
			//RxApp.MainThreadScheduler.Schedule(TimeSpan.FromMilliseconds(999), ViewModel.LoadInitial);
		});

		Show();
	}
}
