using ModManager.Controls;
using ModManager.Models;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace ModManager.ViewModels;

public class VersionGeneratorViewModel : ReactiveObject
{
	[Reactive] public LarianVersion Version { get; set; }
	[Reactive] public string Text { get; set; }

	public ICommand CopyCommand { get; private set; }
	public ICommand ResetCommand { get; private set; }
	public ReactiveCommand<KeyboardFocusChangedEventArgs, Unit> UpdateVersionFromTextCommand { get; private set; }

	public VersionGeneratorViewModel(AlertBar alert)
	{
		Version = new LarianVersion(36028797018963968);

		CopyCommand = ReactiveCommand.Create(() =>
		{
			Clipboard.SetText(Version.VersionInt.ToString());
			alert.SetSuccessAlert($"Copied {Version.VersionInt} to the clipboard.");
		});

		ResetCommand = ReactiveCommand.Create(() =>
		{
			Version.VersionInt = 36028797018963968;
			alert.SetWarningAlert($"Reset version number.");
		});

		UpdateVersionFromTextCommand = ReactiveCommand.Create<KeyboardFocusChangedEventArgs, Unit>(e =>
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
