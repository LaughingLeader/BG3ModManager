using AutoUpdaterDotNET;

using DivinityModManager.Util;
using DivinityModManager.Windows;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Text.RegularExpressions;
using System.Windows.Input;

namespace DivinityModManager.ViewModels;

public class AppUpdateWindowViewModel : ReactiveObject
{
	private readonly AppUpdateWindow _view;

	public UpdateInfoEventArgs UpdateArgs { get; set; }

	[Reactive] public bool CanConfirm { get; set; }
	[Reactive] public bool CanSkip { get; set; }
	[Reactive] public string SkipButtonText { get; set; }
	[Reactive] public string UpdateDescription { get; set; }
	[Reactive] public string UpdateChangelogView { get; set; }

	public ICommand ConfirmCommand { get; private set; }
	public ICommand SkipCommand { get; private set; }

	public void CheckArgs(UpdateInfoEventArgs args)
	{
		if (args == null) return;
		UpdateArgs = args;
		//Title = $"{AutoUpdater.AppTitle} {args.CurrentVersion}";

		string markdownText;

		if (!args.ChangelogURL.EndsWith(".md"))
		{
			markdownText = WebHelper.DownloadUrlAsStringAsync(DivinityApp.URL_CHANGELOG_RAW, CancellationToken.None).Result;
		}
		else
		{
			markdownText = WebHelper.DownloadUrlAsStringAsync(args.ChangelogURL, CancellationToken.None).Result;
		}
		if (!String.IsNullOrEmpty(markdownText))
		{
			markdownText = Regex.Replace(markdownText, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
			UpdateChangelogView = markdownText;
		}

		if (args.IsUpdateAvailable)
		{
			UpdateDescription = $"{AutoUpdater.AppTitle} {args.CurrentVersion} is now available.{Environment.NewLine}You have version {args.InstalledVersion} installed.";

			CanConfirm = true;
			SkipButtonText = "Skip";
			CanSkip = args.Mandatory?.Value != true;
		}
		else
		{
			UpdateDescription = $"{AutoUpdater.AppTitle} is up-to-date.";
			CanConfirm = false;
			CanSkip = true;
			SkipButtonText = "Close";
		}
	}

	public AppUpdateWindowViewModel(AppUpdateWindow view)
	{
		_view = view;

		var canConfirm = this.WhenAnyValue(x => x.CanConfirm);
		ConfirmCommand = ReactiveCommand.Create(() =>
		{
			try
			{
				if (AutoUpdater.DownloadUpdate(UpdateArgs))
				{
					System.Windows.Application.Current.Shutdown();
				}
			}
			catch (Exception ex)
			{
				App.WM.Main.Window.DisplayError($"Error occurred while updating:\n{ex}");
				_view.Hide();
			}
		}, canConfirm, RxApp.MainThreadScheduler);

		var canSkip = this.WhenAnyValue(x => x.CanSkip);
		SkipCommand = ReactiveCommand.Create(() => _view.Hide(), canSkip);
	}
}
