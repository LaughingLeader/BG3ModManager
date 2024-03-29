namespace ModManager;

public record struct OpenFileBrowserDialogResults(bool Success, string? File, string[]? Files, bool IsSingleFile);