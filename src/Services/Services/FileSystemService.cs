using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using HanumanInstitute.Validators;

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

	/// <inheritdoc />
	public virtual void EnsureDirectoryExists(string path)
	{
		if (_fileSystem.Path.IsPathRooted(path))
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		}
	}

	/// <inheritdoc />
	public virtual void DeleteFileSilent(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				File.Delete(path);
			}
		}
		catch (IOException) { }
	}

	/// <inheritdoc />
	public virtual IEnumerable<string> GetFilesByExtensions(string path, IEnumerable<string> extensions, SearchOption searchOption = SearchOption.TopDirectoryOnly)
	{
		if (path == null) { throw new ArgumentNullException(nameof(path)); }
		if (string.IsNullOrWhiteSpace(path)) { throw new ArgumentException("The passed value may not be empty or whithespace.", nameof(path)); }

		try
		{
			return Directory.EnumerateFiles(path, "*", searchOption).Where(f => extensions.Any(s => f.EndsWith(s, StringComparison.InvariantCultureIgnoreCase)));
		}
		catch (DirectoryNotFoundException) { }
		catch (UnauthorizedAccessException) { }
		catch (PathTooLongException) { }

		return Array.Empty<string>();
	}

	/// <inheritdoc />
	public virtual string GetPathWithoutExtension(string path)
	{
		path.CheckNotNullOrEmpty(nameof(path));
		return Path.Join(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path));
	}

	/// <inheritdoc />
	public virtual string GetPathWithFinalSeparator(string path)
	{
		path.CheckNotNullOrEmpty(nameof(path));
		if (!path.EndsWith(Path.DirectorySeparatorChar))
		{
			path += Path.DirectorySeparatorChar;
		}
		return path;
	}

	/// <inheritdoc />
	public virtual string SanitizeFileName(string fileName, char replacementChar = '_')
	{
		var blackList = new HashSet<char>(System.IO.Path.GetInvalidFileNameChars()) { '"' }; // '"' not invalid in Linux, but causes problems
		var output = fileName.ToCharArray();
		for (int i = 0, ln = output.Length; i < ln; i++)
		{
			if (blackList.Contains(output[i]))
			{
				output[i] = replacementChar;
			}
		}
		return new string(output);
	}
}
