namespace ModManager;

public record ShowAlertRequest(string Message, AlertType AlertType, int Timeout = 20, string? Title = "");