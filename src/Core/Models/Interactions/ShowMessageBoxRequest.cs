namespace ModManager;

public record struct ShowMessageBoxRequest(string Title, string Message, InteractionMessageBoxType MessageBoxType, string? StartingInputText = null);