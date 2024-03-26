using LSLib.LS.Stats;

using ModManager.Interfaces;
using ModManager.Models.Mod;

using NexusModsNET.DataModels.GraphQL.Types;

using ReactiveUI;

using System.Windows;

namespace ModManager.Services;

/// <inheritdoc/>
public class InteractionService : IInteractionService
{
	/// <inheritdoc/>
	public Interaction<bool, bool> ToggleModFileNameDisplay { get; }
	/// <inheritdoc/>
	public Interaction<DeleteFilesViewConfirmationRequest, bool> ConfirmModDeletion { get; }
	/// <inheritdoc/>
	public Interaction<DeleteModsRequest, bool> DeleteMods { get; }
	/// <inheritdoc/>
	public Interaction<DivinityModData, bool> OpenModProperties { get; }
	/// <inheritdoc/>
	public Interaction<NexusGraphCollectionRevision, bool> OpenDownloadCollectionView { get; }
	/// <inheritdoc/>
	public Interaction<OpenFolderBrowserDialogRequest, OpenFolderBrowserDialogResults> OpenFolderBrowserDialog { get; }
	/// <inheritdoc/>
	public Interaction<ShowAlertRequest, bool> ShowAlert { get; }
	/// <inheritdoc/>
	public Interaction<ShowMessageBoxRequest, bool> ShowMessageBox { get; }
	/// <inheritdoc/>
	public Interaction<ValidateModStatsRequest, bool> ValidateModStats { get; }
	/// <inheritdoc/>
	public Interaction<ValidateModStatsResults, bool> OpenValidateStatsResults { get; }

	public InteractionService()
	{
		ConfirmModDeletion = new();
		OpenModProperties = new();
		OpenDownloadCollectionView = new();
		ValidateModStats = new();
		OpenValidateStatsResults = new();
		DeleteMods = new();
		ShowAlert = new();
		ToggleModFileNameDisplay = new();
		ShowMessageBox = new();
		OpenFolderBrowserDialog = new();
	}
}
