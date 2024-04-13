namespace ModManager;
public interface IDialogService
{
	Task<OpenFileBrowserDialogResults> OpenFolderAsync(OpenFolderBrowserDialogRequest context);
	Task<OpenFileBrowserDialogResults> OpenFileAsync(OpenFileBrowserDialogRequest context);
	Task<OpenFileBrowserDialogResults> SaveFileAsync(OpenFileBrowserDialogRequest context);
}
