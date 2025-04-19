using ModManager.Models;

using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata.Ecma335;

namespace ModManager.ViewModels;

public class VersionGeneratorViewModel : ReactiveObject, IClosableViewModel, IRoutableViewModel
{
	#region IClosableViewModel/IRoutableViewModel
	public string UrlPathSegment => "versiongenerator";
	public IScreen HostScreen { get; }
	[Reactive] public bool IsVisible { get; set; }
	public RxCommandUnit CloseCommand { get; }
	#endregion

	[Reactive] public LarianVersion Version { get; set; }

	[Range(long.MinValue, long.MaxValue, ErrorMessage = "Numbers only")]
	[Reactive] public string? Text { get; set; }

	public RxCommandUnit CopyCommand { get; }
	public RxCommandUnit ResetCommand { get; }
	public RxCommandUnit UpdateVersionFromTextCommand { get; }
	public ReactiveCommand<ShowAlertRequest, ShowAlertRequest> ShowAlertCommand { get; }

	public VersionGeneratorViewModel(IGlobalCommandsService globalCommands, IScreen? host = null)
	{
		HostScreen = host ?? Locator.Current.GetService<IScreen>()!;
		CloseCommand = this.CreateCloseCommand();

		Version = new LarianVersion(36028797018963968);

		ShowAlertCommand = ReactiveCommand.Create<ShowAlertRequest, ShowAlertRequest>(request => request);

		CopyCommand = ReactiveCommand.Create(() =>
		{
			globalCommands.CopyToClipboardCommand.Execute(Version.VersionInt.ToString()).Subscribe();
			ShowAlertCommand.Execute(new ShowAlertRequest($"Copied {Version.VersionInt} to the clipboard.", AlertType.Success, 5)).Subscribe();
		});

		ResetCommand = ReactiveCommand.Create(() =>
		{
			Version.VersionInt = 36028797018963968;
			Text = "36028797018963968";
			ShowAlertCommand.Execute(new ShowAlertRequest("Reset version number.", AlertType.Info, 2)).Subscribe();
		});

		UpdateVersionFromTextCommand = ReactiveCommand.Create(() =>
		{
			if (ulong.TryParse(Text, out var version))
			{
				Version.ParseInt(version);
			}
			else
			{
				Version.ParseInt(36028797018963968);
			}
			return Unit.Default;
		});

		Version.WhenAnyValue(x => x.VersionInt).Throttle(TimeSpan.FromMilliseconds(50)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(v =>
		{
			Text = v.ToString();
		});

		Version.WhenAnyValue(x => x.Major, x => x.Minor, x => x.Revision, x => x.Build).Throttle(TimeSpan.FromMilliseconds(50)).ObserveOn(RxApp.MainThreadScheduler).Subscribe(v =>
		{
			Version.VersionInt = Version.ToInt();
		});
	}
}
