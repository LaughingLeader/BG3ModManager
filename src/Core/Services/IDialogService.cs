namespace ModManager;
public interface IDialogService
{
	string GetInitialStartingDirectory(string? prioritizePath = null);
	Task<OpenFileBrowserDialogResults> OpenFolderAsync(OpenFolderBrowserDialogRequest context);
	Task<OpenFileBrowserDialogResults> OpenFileAsync(OpenFileBrowserDialogRequest context);
	Task<OpenFileBrowserDialogResults> SaveFileAsync(OpenFileBrowserDialogRequest context);
}
