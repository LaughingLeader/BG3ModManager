using ModManager.Models.Mod;

using NexusModsNET.DataModels.GraphQL.Types;

namespace ModManager;

public interface IInteractionsService
{
	/// <summary>
	/// Confirm deletion of mods.
	/// </summary>
	Interaction<DeleteFilesViewConfirmationRequest, bool> ConfirmModDeletion { get; }

	/// <summary>
	/// Toggle mod display between display names and file names.
	/// </summary>
	Interaction<bool, bool> ToggleModFileNameDisplay { get; }

	/// <summary>
	/// Open the mod deletion view.
	/// </summary>
	Interaction<DeleteModsRequest, bool> DeleteMods { get; }

	/// <summary>
	/// Requests the ViewModel to delete all selected mods.
	/// </summary>
	Interaction<Unit, bool> DeleteSelectedMods { get; }

	/// <summary>
	/// Open the mod properties view.
	/// </summary>
	Interaction<ModData, bool> OpenModProperties { get; }

	/// <summary>
	/// Open a view for downloading a Nexus Mods collection.
	/// </summary>
	Interaction<NexusGraphCollectionRevision, bool> OpenDownloadCollectionView { get; }

	/// <summary>
	/// Request a file browser dialog window.
	/// </summary>
	Interaction<OpenFileBrowserDialogRequest, OpenFileBrowserDialogResults> OpenFileBrowserDialog { get; }

	/// <summary>
	/// Request a folder browser dialog window.
	/// </summary>
	Interaction<OpenFolderBrowserDialogRequest, OpenFileBrowserDialogResults> OpenFolderBrowserDialog { get; }

	/// <summary>
	/// Show an alert in the main view.
	/// </summary>
	Interaction<ShowAlertRequest, bool> ShowAlert { get; }

	/// <summary>
	/// Show a message box.
	/// </summary>
	Interaction<ShowMessageBoxRequest, bool> ShowMessageBox { get; }

	/// <summary>
	/// Validate stats for given mods using LSLib.
	/// </summary>
	Interaction<ValidateModStatsRequest, bool> ValidateModStats { get; }

	/// <summary>
	/// Open the stats validator view with the given results.
	/// </summary>
	Interaction<ValidateModStatsResults, bool> OpenValidateStatsResults { get; }
}
