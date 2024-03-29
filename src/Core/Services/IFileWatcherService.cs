using ModManager.Services;

namespace ModManager;

public interface IFileWatcherService
{
	IFileWatcherWrapper WatchDirectory(string directory, string filter);
}