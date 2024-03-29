namespace ModManager;

public record struct ShowAlertRequest(string Message, AlertType AlertType, int Timeout);