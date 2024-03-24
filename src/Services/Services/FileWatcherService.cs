using ModManager.Services.IO;

namespace ModManager.Services;

public class FileWatcherService : IFileWatcherService
{
	public IFileWatcherWrapper WatchDirectory(string directory, string filter)
	{
		var watcherWrapper = new FileWatcherWrapper(filter, directory);
		return watcherWrapper;
	}
}