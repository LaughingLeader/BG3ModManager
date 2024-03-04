﻿using DivinityModManager.Controls;
using DivinityModManager.Models;
using DivinityModManager.Windows;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DivinityModManager.Windows
{
	public class VersionGeneratorViewModel : ReactiveObject
	{
		[Reactive] public DivinityModVersion2 Version { get; set; }
		[Reactive] public string Text { get; set; }

		public ICommand CopyCommand { get; private set; }
		public ICommand ResetCommand { get; private set; }
		public ReactiveCommand<KeyboardFocusChangedEventArgs, Unit> UpdateVersionFromTextCommand { get; private set; }

		public VersionGeneratorViewModel(AlertBar alert)
		{
			Version = new DivinityModVersion2(36028797018963968);

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

	public class VersionGeneratorWindowBase : HideWindowBase<VersionGeneratorViewModel> { }

	/// <summary>
	/// Interaction logic for VersionGenerator.xaml
	/// </summary>
	public partial class VersionGeneratorWindow : VersionGeneratorWindowBase
	{
		private static readonly Regex _numberOnlyRegex = new("[^0-9]+");

		public VersionGeneratorWindow()
		{
			InitializeComponent();

			ViewModel = new VersionGeneratorViewModel(AlertBar);

			this.WhenActivated(d =>
			{
				d(this.Bind(ViewModel, vm => vm.Text, v => v.VersionNumberTextBox.Text));
				d(this.Bind(ViewModel, vm => vm.Version.Major, v => v.MajorUpDown.Value));
				d(this.Bind(ViewModel, vm => vm.Version.Minor, v => v.MinorUpDown.Value));
				d(this.Bind(ViewModel, vm => vm.Version.Revision, v => v.RevisionUpDown.Value));
				d(this.Bind(ViewModel, vm => vm.Version.Build, v => v.BuildUpDown.Value));
				d(this.BindCommand(ViewModel, vm => vm.CopyCommand, v => v.CopyButton));
				d(this.BindCommand(ViewModel, vm => vm.ResetCommand, v => v.ResetButton));

				var tbEvents = this.VersionNumberTextBox.Events();
				d(tbEvents.LostKeyboardFocus.ObserveOn(RxApp.MainThreadScheduler).InvokeCommand(ViewModel.UpdateVersionFromTextCommand));
				d(tbEvents.PreviewTextInput.ObserveOn(RxApp.MainThreadScheduler).Subscribe((e) =>
				{
					e.Handled = _numberOnlyRegex.IsMatch(e.Text);
				}));
			});
		}
	}
}
