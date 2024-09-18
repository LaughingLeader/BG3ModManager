namespace ModManager;

public readonly struct OpenFolderBrowserDialogRequest
{
	public string? Title { get; init; }
	public string? Description { get; init; }
	public string? StartingPath { get; init; }
	public bool MultiSelect { get; init; }
	public object? TargetWindow { get; init; }

	public OpenFolderBrowserDialogRequest()
	{
		Title = "Open Folder...";
	}

	public OpenFolderBrowserDialogRequest(string title, string startingPath, string? description = null, object? window = null)
	{
		Title = title;
		Description = description;
		StartingPath = startingPath;
		TargetWindow = window;
	}
}