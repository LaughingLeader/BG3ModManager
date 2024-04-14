namespace ModManager;

public readonly struct OpenFileBrowserDialogRequest
{
	public string? Title { get; init; }
	public string? Description { get; init; }
	public string? StartingPath { get; init; }
	public bool MultiSelect { get; init; }
	public FileTypeFilter[] FileTypes { get; init; }

	public OpenFileBrowserDialogRequest()
	{
		Title = "Open File...";
		FileTypes = [CommonFileTypes.All];
	}

	public OpenFileBrowserDialogRequest(string title, string startingPath, FileTypeFilter[]? fileTypes = null, bool multiSelect = false, string? description = null)
	{
		Title = title;
		Description = description;
		StartingPath = startingPath;
		FileTypes = fileTypes ?? [CommonFileTypes.All];
		MultiSelect = multiSelect;
	}
}