using ModManager.Models.Mod;

using NexusModsNET.DataModels.GraphQL.Types;

using ReactiveUI;

using System.Reactive;

namespace ModManager.Services;

/// <inheritdoc/>
public class InteractionsService : IInteractionsService
{
	/// <inheritdoc/>
	public Interaction<bool, bool> ToggleModFileNameDisplay { get; }
	/// <inheritdoc/>
	public Interaction<DeleteFilesViewConfirmationRequest, bool> ConfirmModDeletion { get; }
	/// <inheritdoc/>
	public Interaction<DeleteModsRequest, bool> DeleteMods { get; }
	/// <inheritdoc/>
	public Interaction<Unit, bool> DeleteSelectedMods { get; }
	/// <inheritdoc/>
	public Interaction<ModData, bool> OpenModProperties { get; }
	/// <inheritdoc/>
	public Interaction<NexusGraphCollectionRevision, bool> OpenDownloadCollectionView { get; }
	/// <inheritdoc/>
	public Interaction<OpenFileBrowserDialogRequest, OpenFileBrowserDialogResults> OpenFileBrowserDialog { get; }
	/// <inheritdoc/>
	public Interaction<OpenFolderBrowserDialogRequest, OpenFileBrowserDialogResults> OpenFolderBrowserDialog { get; }
	/// <inheritdoc/>
	public Interaction<ShowAlertRequest, bool> ShowAlert { get; }
	/// <inheritdoc/>
	public Interaction<ShowMessageBoxRequest, MessageBoxResult> ShowMessageBox { get; }
	/// <inheritdoc/>
	public Interaction<ShowModPickerRequest, ModPickerResult> PickMods { get; }
	/// <inheritdoc/>
	public Interaction<ValidateModStatsRequest, bool> ValidateModStats { get; }
	/// <inheritdoc/>
	public Interaction<ValidateModStatsResults, bool> OpenValidateStatsResults { get; }
	/// <inheritdoc/>
	public Interaction<ViewModFilesRequest, bool> ViewModFiles { get; }

	public InteractionsService()
	{
		ConfirmModDeletion = new();
		DeleteMods = new();
		DeleteSelectedMods = new();
		OpenDownloadCollectionView = new();
		OpenFileBrowserDialog = new();
		OpenFolderBrowserDialog = new();
		OpenModProperties = new();
		OpenValidateStatsResults = new();
		ShowAlert = new();
		ShowMessageBox = new();
		PickMods = new();
		ToggleModFileNameDisplay = new();
		ValidateModStats = new();
		ViewModFiles = new();
	}
}
