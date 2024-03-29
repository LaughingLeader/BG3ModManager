namespace ModManager;

public record struct OpenFolderBrowserDialogResults(bool Success, string File, string[] Files, bool IsSingleFile);