namespace ModManager;

public record OpenFolderBrowserDialogRequest(string Description, string StartingPath, 
	bool MultiSelect = false, string Title = "");