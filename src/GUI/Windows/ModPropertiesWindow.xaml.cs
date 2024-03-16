using DivinityModManager.ViewModels;

using DynamicData;

using ReactiveUI;

namespace DivinityModManager.Windows;

public class ModPropertiesWindowBase : HideWindowBase<ModConfigPropertiesViewModel> { }


/// <summary>
/// Interaction logic for SingleModConfigWindow.xaml
/// </summary>
public partial class ModPropertiesWindow : ModPropertiesWindowBase
{
	private void ConfirmAndClose()
	{
		ViewModel.Apply();
		Hide();
	}

	private void CancelAndClose()
	{
		ViewModel.OnClose();
		Hide();
	}

	private readonly object LargeFileIcon;
	private readonly object LargeFolderIcon;

	private object GetModTypeIcon(bool isEditorMod) => isEditorMod ? LargeFolderIcon : LargeFileIcon;

	public ModPropertiesWindow()
	{
		InitializeComponent();

		ViewModel = new ModConfigPropertiesViewModel()
		{
			OKCommand = ReactiveCommand.Create(ConfirmAndClose),
			CancelCommand = ReactiveCommand.Create(CancelAndClose)
		};

		LargeFileIcon = FindResource("LargeFileIcon");
		LargeFolderIcon = FindResource("LargeFolderIcon");

		/*ConfigAutoGrid.Loaded += (o, e) =>
		{
			ConfigAutoGrid.Rows = String.Join(",", Enumerable.Repeat("auto", ConfigAutoGrid.RowCount));
		};*/

		ModNexusModsIDUpDown.Minimum = DivinityApp.NEXUSMODS_MOD_ID_START;

		this.Activated += (o, e) => ViewModel.IsActive = true;
		this.Deactivated += (o, e) => ViewModel.IsActive = false;

		this.WhenActivated(d =>
		{
			this.OneWayBind(ViewModel, vm => vm.Title, v => v.Title);

			this.OneWayBind(ViewModel, vm => vm.Mod.FileName, v => v.ModFileNameText.Text);
			this.OneWayBind(ViewModel, vm => vm.Mod.Name, v => v.ModNameText.Text);
			this.OneWayBind(ViewModel, vm => vm.Mod.Description, v => v.ModDescriptionText.Text);
			this.OneWayBind(ViewModel, vm => vm.ModFilePath, v => v.ModPathText.Text);

			this.Bind(ViewModel, vm => vm.NexusModsId, v => v.ModNexusModsIDUpDown.Value);
			this.Bind(ViewModel, vm => vm.GitHub, v => v.ModGitHubText.Text);
			this.OneWayBind(ViewModel, vm => vm.GitHubPlaceholderLabelVisibility, v => v.GitHubPlaceholderLabel.Visibility);

			this.Bind(ViewModel, vm => vm.Notes, v => v.ModNotesTextBox.Text);

			this.OneWayBind(ViewModel, vm => vm.ModType, v => v.ModTypeText.Text);
			this.OneWayBind(ViewModel, vm => vm.ModSizeText, v => v.ModSizeText.Text);

			this.OneWayBind(ViewModel, vm => vm.Mod.IsEditorMod, v => v.ModTypeIconControl.Content, GetModTypeIcon);

			this.BindCommand(ViewModel, vm => vm.ApplyCommand, v => v.ApplyButton);
			this.BindCommand(ViewModel, vm => vm.OKCommand, v => v.OKButton);
			this.BindCommand(ViewModel, vm => vm.CancelCommand, v => v.CancelButton);

			HelpStackPanel.ToolTip = "Set the NexusMods ID to allow auto-updating (provided a NexusMods API key is set)\n\nSetting a valid GitHub Author/Repository will also allow auto-updating from GitHub";
		});
	}
}
