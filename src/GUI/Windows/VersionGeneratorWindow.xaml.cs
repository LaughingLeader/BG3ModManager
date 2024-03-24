using ModManager.ViewModels;

using ReactiveUI;

using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ModManager.Windows;

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
