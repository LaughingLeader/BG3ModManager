﻿using ModManager.Models.Mod;

using NexusModsNET.DataModels.GraphQL.Types;

using ReactiveUI;

namespace ModManager.Interfaces;
public interface IInteractionService
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
	/// Open the mod properties view.
	/// </summary>
	Interaction<DivinityModData, bool> OpenModProperties { get; }

	/// <summary>
	/// Open a view for downloading a Nexus Mods collection.
	/// </summary>
	Interaction<NexusGraphCollectionRevision, bool> OpenDownloadCollectionView { get; }

	/// <summary>
	/// Request a folder browser dialog window.
	/// </summary>
	Interaction<OpenFolderBrowserDialogRequest, OpenFolderBrowserDialogResults> OpenFolderBrowserDialog { get; }

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
