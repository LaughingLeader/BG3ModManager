using ModManager.ViewModels;

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

	[GeneratedRegex("[^0-9]+")]
	private static partial Regex NumbersOnlyRe();

	private static readonly Regex _numberOnlyRegex = NumbersOnlyRe();

	public VersionGeneratorWindow()
	{
		InitializeComponent();

		ViewModel = ViewModelLocator.VersionGenerator;

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
