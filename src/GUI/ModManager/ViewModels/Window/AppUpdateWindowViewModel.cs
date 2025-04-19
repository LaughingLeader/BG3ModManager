using Avalonia.Controls.ApplicationLifetimes;

using ModManager.Util;

using AutoUpdateViaGitHubRelease;

using System.Net.Http;
using System.Text.RegularExpressions;

namespace ModManager.ViewModels;

public partial class AppUpdateWindowViewModel : ReactiveObject, IClosableViewModel, IRoutableViewModel
{
	#region IClosableViewModel/IRoutableViewModel
	public string UrlPathSegment => "appupdate";
	public IScreen HostScreen { get; }
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }
	#endregion

	[Reactive] public bool CanConfirm { get; set; }
	[Reactive] public bool CanSkip { get; set; }
	[Reactive] public string? SkipButtonText { get; set; }
	[Reactive] public string? UpdateDescription { get; set; }
	[Reactive] public string? UpdateChangelogView { get; set; }

	public RxCommandUnit ConfirmCommand { get; private set; }
	public RxCommandUnit SkipCommand { get; private set; }


	[GeneratedRegex(@"^\s+$[\r\n]*", RegexOptions.Multiline)]
	private static partial Regex RemoveEmptyLinesRe();

	private static readonly Regex RemoveEmptyLinesPattern = RemoveEmptyLinesRe();

	private bool _openAfterUpdateCheck = false;

	private async Task CheckForUpdatesAsync(IScheduler scheduler, CancellationToken token)
	{
		string markdownText;

		markdownText = await WebHelper.DownloadUrlAsStringAsync(DivinityApp.URL_CHANGELOG_RAW, CancellationToken.None);
		var updater = AppServices.AppUpdater;
		var result = await updater.CheckForUpdatesAsync();

		RxApp.MainThreadScheduler.Schedule(() =>
		{
			if (!String.IsNullOrEmpty(markdownText))
			{
				markdownText = RemoveEmptyLinesPattern.Replace(markdownText, string.Empty);
				UpdateChangelogView = markdownText;
			}

			if (result.IsAvailable)
			{
				UpdateDescription = $"{updater.AppTitle} {result.Version} is now available.{Environment.NewLine}You have version {updater.CurrentVersion} installed.";

				CanConfirm = true;
				SkipButtonText = "Skip";
				CanSkip = true;
				IsVisible = true;
			}
			else
			{
				UpdateDescription = $"{updater.AppTitle} is up-to-date.";
				CanConfirm = false;
				CanSkip = true;
				SkipButtonText = "Close";
				if(_openAfterUpdateCheck)
				{
					IsVisible = true;
				}
			}
			_openAfterUpdateCheck = false;
		});
	}

	public void ScheduleUpdateCheck(bool openWindowAfterwards = false)
	{
		_openAfterUpdateCheck = openWindowAfterwards;
		RxApp.TaskpoolScheduler.ScheduleAsync(CheckForUpdatesAsync);
	}

	private async Task RunUpdateAsync(CancellationToken token)
	{
		var result = await AppServices.AppUpdater.DownloadAndInstallUpdateAsync();
		if(result && Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			desktop.Shutdown();
		}
	}

	public AppUpdateWindowViewModel(IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;
		CloseCommand = this.CreateCloseCommand();

		CanSkip = true;
		SkipButtonText = "Close";

		var canConfirm = this.WhenAnyValue(x => x.CanConfirm);
		ConfirmCommand = ReactiveCommand.CreateFromTask(RunUpdateAsync, canConfirm, RxApp.MainThreadScheduler);

		var canSkip = this.WhenAnyValue(x => x.CanSkip);
		SkipCommand = ReactiveCommand.Create(() =>
		{
			IsVisible = false;
		}, canSkip);
	}
}
