namespace ModManager;

public record struct ShowMessageBoxRequest(string Message, string Title, InteractionMessageBoxType MessageBoxType);