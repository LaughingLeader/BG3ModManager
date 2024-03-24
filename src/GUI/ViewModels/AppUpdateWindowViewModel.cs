using AutoUpdaterDotNET;

using DivinityModManager.Util;
using DivinityModManager.Windows;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive.Concurrency;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace DivinityModManager.ViewModels;

public partial class AppUpdateWindowViewModel : ReactiveObject
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


	[GeneratedRegex(@"^\s+$[\r\n]*", RegexOptions.Multiline)]
	private static partial Regex RemoveEmptyLinesRe();

	private static readonly Regex RemoveEmptyLinesPattern = RemoveEmptyLinesRe();

	private async Task CheckArgsAsync(IScheduler scheduler, CancellationToken token)
	{
		string markdownText;

		if (!UpdateArgs.ChangelogURL.EndsWith(".md"))
		{
			markdownText = await WebHelper.DownloadUrlAsStringAsync(DivinityApp.URL_CHANGELOG_RAW, CancellationToken.None);
		}
		else
		{
			markdownText = await WebHelper.DownloadUrlAsStringAsync(UpdateArgs.ChangelogURL, CancellationToken.None);
		}

		RxApp.MainThreadScheduler.Schedule(() =>
		{
			if (!String.IsNullOrEmpty(markdownText))
			{
				markdownText = RemoveEmptyLinesPattern.Replace(markdownText, string.Empty);
				UpdateChangelogView = markdownText;
			}

			if (UpdateArgs.IsUpdateAvailable)
			{
				UpdateDescription = $"{AutoUpdater.AppTitle} {UpdateArgs.CurrentVersion} is now available.{Environment.NewLine}You have version {UpdateArgs.InstalledVersion} installed.";

				CanConfirm = true;
				SkipButtonText = "Skip";
				CanSkip = UpdateArgs.Mandatory?.Value != true;
			}
			else
			{
				UpdateDescription = $"{AutoUpdater.AppTitle} is up-to-date.";
				CanConfirm = false;
				CanSkip = true;
				SkipButtonText = "Close";
			}

			App.WM.AppUpdate.Toggle(true);
		});
	}

	public void CheckArgsAndOpen(UpdateInfoEventArgs args)
	{
		if (args == null) return;
		UpdateArgs = args;
		RxApp.TaskpoolScheduler.ScheduleAsync(CheckArgsAsync);
	}

	public AppUpdateWindowViewModel(AppUpdateWindow view)
	{
		_view = view;

		CanSkip = true;
		SkipButtonText = "Close";

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
