namespace ModManager;

public readonly struct OpenFolderBrowserDialogRequest
{
	public string? Title { get; init; }
	public string? Description { get; init; }
	public string? SuggestedDirectory { get; init; }
	public string? SuggestedName { get; init; }
	public bool MultiSelect { get; init; }
	public object? TargetWindow { get; init; }

	public OpenFolderBrowserDialogRequest()
	{
		Title = "Open Folder...";
	}

	public OpenFolderBrowserDialogRequest(string title, string startingPath, string? fileName = null, string? description = null, object? window = null)
	{
		Title = title;
		Description = description;
		SuggestedDirectory = startingPath;
		SuggestedName = fileName;
		TargetWindow = window;
	}
}