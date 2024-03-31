using Avalonia.Controls.ApplicationLifetimes;

using ModManager.Util;

using Onova;
using Onova.Models;
using Onova.Services;

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

	public CheckForUpdatesResult? UpdateArgs { get; set; }

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

	private readonly UpdateManager _updateManager;

	private async Task CheckArgsAsync(IScheduler scheduler, CancellationToken token)
	{
		string markdownText;

		markdownText = await WebHelper.DownloadUrlAsStringAsync(DivinityApp.URL_CHANGELOG_RAW, CancellationToken.None);

		RxApp.MainThreadScheduler.Schedule(() =>
		{
			if (!String.IsNullOrEmpty(markdownText))
			{
				markdownText = RemoveEmptyLinesPattern.Replace(markdownText, string.Empty);
				UpdateChangelogView = markdownText;
			}

			var env = AppServices.Get<IEnvironmentService>()!;

			if (UpdateArgs?.CanUpdate == true)
			{
				UpdateDescription = $"{env.AppFriendlyName} {UpdateArgs.LastVersion} is now available.{Environment.NewLine}You have version {env.AppVersion} installed.";

				CanConfirm = true;
				SkipButtonText = "Skip";
				CanSkip = true;
			}
			else
			{
				UpdateDescription = $"{env.AppFriendlyName} is up-to-date.";
				CanConfirm = false;
				CanSkip = true;
				SkipButtonText = "Close";
			}

			IsVisible = true;
		});
	}

	public void CheckArgsAndOpen(CheckForUpdatesResult? args)
	{
		UpdateArgs = args;
		RxApp.TaskpoolScheduler.ScheduleAsync(CheckArgsAsync);
	}

	private async Task RunUpdateAsync(CancellationToken token)
	{
		if (UpdateArgs?.LastVersion != null)
		{
			await _updateManager.PrepareUpdateAsync(UpdateArgs.LastVersion, null, token);
			_updateManager.LaunchUpdater(UpdateArgs.LastVersion);
			if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
			{
				desktop.Shutdown();
			}
		}
	}

	public AppUpdateWindowViewModel(IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;
		CloseCommand = this.CreateCloseCommand();

		CanSkip = true;
		SkipButtonText = "Close";

		_updateManager = new UpdateManager(
			new GithubPackageResolver(Locator.Current.GetService<HttpClient>()!,
			DivinityApp.GITHUB_USER, DivinityApp.GITHUB_REPO, DivinityApp.GITHUB_RELEASE_ASSET),
			new ZipPackageExtractor());

		var canConfirm = this.WhenAnyValue(x => x.CanConfirm);
		ConfirmCommand = ReactiveCommand.CreateFromTask(RunUpdateAsync, canConfirm, RxApp.MainThreadScheduler);

		var canSkip = this.WhenAnyValue(x => x.CanSkip);
		SkipCommand = ReactiveCommand.Create(() =>
		{
			IsVisible = false;
		}, canSkip);
	}
}
