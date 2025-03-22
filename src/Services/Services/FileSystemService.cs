using System.IO;
using System.IO.Abstractions;

namespace ModManager.Services;

/// <inheritdoc />
public class FileSystemService(IFileSystem fileSystemService) : IFileSystemService
{
	private readonly IFileSystem _fileSystem = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));

	public IDirectory Directory => _fileSystem.Directory;
	public IDirectoryInfoFactory DirectoryInfo => _fileSystem.DirectoryInfo;
	public IDriveInfoFactory DriveInfo => _fileSystem.DriveInfo;
	public IFile File => _fileSystem.File;
	public IFileInfoFactory FileInfo => _fileSystem.FileInfo;
	public IFileStreamFactory FileStream => _fileSystem.FileStream;
	public IFileSystemWatcherFactory FileSystemWatcher => _fileSystem.FileSystemWatcher;
	public IPath Path => _fileSystem.Path;
	public IFileVersionInfoFactory FileVersionInfo => fileSystemService.FileVersionInfo;

	/// <inheritdoc />
	public virtual void EnsureDirectoryExists(string path)
	{
		ArgumentNullException.ThrowIfNullOrEmpty(path);
		if (_fileSystem.Path.IsPathRooted(path))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		}
	}

	/// <inheritdoc />
	public virtual string GetPathWithoutExtension(string path)
	{
		ArgumentNullException.ThrowIfNullOrEmpty(path);
		return Path.Join(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
	}

	/// <inheritdoc />
	public virtual string GetPathWithFinalSeparator(string path)
	{
		ArgumentNullException.ThrowIfNullOrEmpty(path);
		if (!path.EndsWith(Path.DirectorySeparatorChar))
		{
			path += Path.DirectorySeparatorChar;
		}
		return path;
	}

	/// <inheritdoc />
	public virtual string SanitizeFileName(string fileName, char replacementChar = '_')
	{
		ArgumentNullException.ThrowIfNullOrEmpty(fileName);
		foreach (var character in Path.GetInvalidFileNameChars())
		{
			fileName = fileName.Replace(character, replacementChar);
		}
		return fileName.Trim();
	}
}
