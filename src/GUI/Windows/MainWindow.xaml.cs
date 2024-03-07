﻿using AdonisUI;
using AdonisUI.Controls;

using System.IO;

using AutoUpdaterDotNET;

using DivinityModManager.AppServices;
using DivinityModManager.Controls;
using DivinityModManager.Util;
using DivinityModManager.Util.ScreenReader;
using DivinityModManager.ViewModels;
using DivinityModManager.Views;

using DynamicData;

using ReactiveUI;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace DivinityModManager.Windows
{
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
		private static MainWindow self;
		public static MainWindow Self => self;

		[DllImport("user32")] public static extern int FlashWindow(IntPtr hwnd, bool bInvert);

		public MainViewControl MainView { get; private set; }

		private readonly System.Windows.Interop.WindowInteropHelper _hwnd;

		public LogTraceListener DebugLogListener { get; private set; }

		private readonly string _logsDir;
		private readonly string _logFileName;

		public AlertBar AlertBar => MainView.AlertBar;
		public Style MessageBoxStyle => MainView.MainWindowMessageBox_OK.Style;

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

		public static readonly Uri LightTheme = new("pack://application:,,,/BG3ModManager;component/Themes/Light.xaml", UriKind.Absolute);
		public static readonly Uri DarkTheme = new("pack://application:,,,/BG3ModManager;component/Themes/Dark.xaml", UriKind.Absolute);

		public void UpdateColorTheme(bool darkMode)
		{
			var theme = !darkMode ? LightTheme : DarkTheme;
			ResourceLocator.SetColorScheme(Resources, theme);
			App.WM.UpdateColorScheme(theme);
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

		private WindowInteropHelper _wih;

		public void FlashTaskbar()
		{
			FlashWindow(_wih.Handle, true);
		}

		public MainWindow()
		{
			InitializeComponent();
			self = this;

			_hwnd = new System.Windows.Interop.WindowInteropHelper(this);

			_logsDir = DivinityApp.GetAppDirectory("_Logs");
			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
#if DEBUG
			_logFileName = Path.Combine(_logsDir, "debug_" + DateTime.Now.ToString(sysFormat + "_HH-mm-ss") + ".log");
#else
			_logFileName = Path.Combine(_logsDir, "release_" + DateTime.Now.ToString(sysFormat + "_HH-mm-ss") + ".log");
#endif

			Application.Current.DispatcherUnhandledException += OnUIException;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			DivinityApp.DateTimeColumnFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern;
			DivinityApp.DateTimeTooltipFormat = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern;

			ViewModel = new MainWindowViewModel();
			MainView = new MainViewControl(this, ViewModel);
			MainGrid.Children.Add(MainView);

			Services.RegisterSingleton(new WindowManagerService());

			if (File.Exists(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "debug")))
			{
				ViewModel.DebugMode = true;
				ToggleLogging(true);
				DivinityApp.Log("Enable logging due to the debug file next to the exe.");
			}

			this.TaskbarItemInfo.ProgressState = System.Windows.Shell.TaskbarItemProgressState.None;

			Closed += (o, e) => OnClosing();
			AutoUpdater.ApplicationExitEvent += AutoUpdater_OnClosing;
			AutoUpdater.HttpUserAgent = "DivinityModManagerUser";
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

			this.WhenActivated(d =>
			{
				ViewModel.OnViewActivated(this, MainView);
				this.WhenAnyValue(x => x.ViewModel.Title).BindTo(this, view => view.Title);
				this.OneWayBind(ViewModel, vm => vm.MainProgressIsActive, view => view.TaskbarItemInfo.ProgressState, BoolToTaskbarItemProgressState);

				ViewModel.Keys.OpenPreferences.AddAction(() => App.WM.Settings.Toggle());
				ViewModel.Keys.OpenKeybindings.AddAction(() => {
					App.WM.Settings.Toggle();
					if(App.WM.Settings.Window.IsVisible) App.WM.Settings.Window.ViewModel.SelectedTabIndex = SettingsWindowTab.Keybindings;
				});
				ViewModel.Keys.OpenAboutWindow.AddAction(() => App.WM.About.Toggle());

				ViewModel.Keys.ToggleVersionGeneratorWindow.AddAction(() => App.WM.VersionGenerator.Toggle());

				this.WhenAnyValue(x => x.ViewModel.MainProgressValue).BindTo(this, view => view.TaskbarItemInfo.ProgressValue);

				MainView.OnActivated();

				//Allow launching the game if single instance mode is enabled, but the shift key is held
				Observable.Merge(
					Observable.FromEventPattern<KeyEventArgs>(this, nameof(KeyDown)),
					Observable.FromEventPattern<KeyEventArgs>(this, nameof(KeyUp))
				)
				.Select(e => (e.EventArgs.Key == Key.LeftShift || e.EventArgs.Key == Key.RightShift) && e.EventArgs.IsDown)
				.BindTo(ViewModel, x => x.CanForceLaunchGame);
			});

			Show();
		}
	}
}
