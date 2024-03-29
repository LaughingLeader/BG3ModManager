namespace ModManager;

public record OpenFileBrowserDialogRequest(string Description, string StartingPath,
	bool MultiSelect = false, string Title = "", FileTypeFilter[]? FileTypes = null, bool IsSaving = false);