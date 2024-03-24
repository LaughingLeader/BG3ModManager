using ModManager.Models.Mod;

using LSLib.LS.Stats;

using NexusModsNET.DataModels.GraphQL.Types;

using ReactiveUI;

using System.Windows;

namespace ModManager;

public record struct DeleteFilesViewConfirmationData(int Total, bool PermanentlyDelete, CancellationToken Token);
public record struct ValidateModStatsRequest(List<DivinityModData> Mods, CancellationToken Token);
public record struct ValidateModStatsResults(List<DivinityModData> Mods, List<StatLoadingError> Errors, Dictionary<string, string[]> FileText);
public record DeleteModsRequestData(List<DivinityModData> TargetMods, bool IsDeletingDuplicates = false, IEnumerable<DivinityModData> LoadedMods = null);
public record struct ShowAlertData(string Message, AlertType AlertType, int Timeout);
public record struct ShowMessageBoxData(string Message, string Title, MessageBoxButton Button, MessageBoxImage Image, MessageBoxResult DefaultResult);
public record struct OpenFolderBrowserDialogRequest(string Description, string StartingPath, bool MultiSelect = false, string Title = "");
public record struct OpenFolderBrowserDialogResults(bool Success, string File, string[] Files, bool IsSingleFile);

public static class DivinityInteractions
{
	public static readonly Interaction<DeleteFilesViewConfirmationData, bool> ConfirmModDeletion = new();
	public static readonly Interaction<DivinityModData, bool> OpenModProperties = new();
	public static readonly Interaction<NexusGraphCollectionRevision, bool> OpenDownloadCollectionView = new();
	public static readonly Interaction<ValidateModStatsRequest, bool> RequestValidateModStats = new();
	public static readonly Interaction<ValidateModStatsResults, bool> OpenValidateStatsResults = new();
	public static readonly Interaction<DeleteModsRequestData, bool> DeleteMods = new();
	public static readonly Interaction<ShowAlertData, bool> ShowAlert = new();
	public static readonly Interaction<bool, bool> ToggleModFileNameDisplay = new();
	public static readonly Interaction<ShowMessageBoxData, MessageBoxResult> ShowMessageBox = new();
	public static readonly Interaction<OpenFolderBrowserDialogRequest, OpenFolderBrowserDialogResults> OpenFolderBrowserDialog = new();
}
