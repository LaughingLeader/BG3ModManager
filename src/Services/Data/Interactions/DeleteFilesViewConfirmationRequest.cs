namespace ModManager;

public record struct DeleteFilesViewConfirmationRequest(int Total, bool PermanentlyDelete, CancellationToken Token);