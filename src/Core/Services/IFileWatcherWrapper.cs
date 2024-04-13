namespace ModManager.Services;
public interface IFileWatcherWrapper
{
	string DefaultDirectory { get; }

	string DirectoryPath { get; }

	bool IsEnabled { get; }

	IObservable<FileSystemEventArgs> FileChanged { get; }
	IObservable<FileSystemEventArgs> FileCreated { get; }
	IObservable<FileSystemEventArgs> FileDeleted { get; }

	void SetDirectory(string path);
	void PauseWatcher(bool paused, double pauseFor = -1);
}