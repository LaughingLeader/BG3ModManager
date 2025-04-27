namespace ModManager;

public readonly struct OpenFileBrowserDialogRequest
{
	public string? Title { get; init; }
	public string? Description { get; init; }
	public string? SuggestedDirectory { get; init; }
	public string? SuggestedName { get; init; }
	public bool MultiSelect { get; init; }
	public FileTypeFilter[] FileTypes { get; init; }
	public object? TargetWindow { get; init; }

	public OpenFileBrowserDialogRequest()
	{
		Title = "Open File...";
		FileTypes = [CommonFileTypes.All];
	}

	public OpenFileBrowserDialogRequest(string title, string startingDirectory, FileTypeFilter[]? fileTypes = null,
		string? fileName = null,
		bool multiSelect = false,
		string? description = null,
		object? window = null
		)
	{
		Title = title;
		Description = description;
		SuggestedDirectory = startingDirectory;
		SuggestedName = fileName;
		FileTypes = fileTypes ?? [CommonFileTypes.All];
		MultiSelect = multiSelect;
		TargetWindow = window;
	}
}