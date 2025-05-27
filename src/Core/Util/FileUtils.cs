using LSLib.LS;

using ModManager.Extensions;
using ModManager.Services;

using System.Diagnostics;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace ModManager.Util;

public static class FileUtils
{
	private static readonly IFileSystemService _fs;
	static FileUtils()
	{
		_fs = Locator.Current.GetService<IFileSystemService>()!;
	}

	public static readonly EnumerationOptions RecursiveOptions = new()
	{
		RecurseSubdirectories = true,
		IgnoreInaccessible = true,
		MatchCasing = MatchCasing.CaseInsensitive
	};

	public static readonly EnumerationOptions GameDataOptions = new()
	{
		RecurseSubdirectories = true,
		IgnoreInaccessible = true,
		MaxRecursionDepth = 1,
		MatchCasing = MatchCasing.CaseInsensitive
	};

	public static readonly EnumerationOptions FlatSearchOptions = new()
	{
		RecurseSubdirectories = false,
		IgnoreInaccessible = true,
		MatchCasing = MatchCasing.CaseInsensitive
	};

	/// <summary>
	/// Gets the drive type of the given path.
	/// </summary>
	/// <param name="path">The path.</param>
	/// <returns>DriveType of path</returns>
	public static DriveType GetPathDriveType(string path)
	{
		//OK, so UNC paths aren't 'drives', but this is still handy
		if (path.StartsWith(@"\\")) return DriveType.Network;
		var info = _fs.DriveInfo.GetDrives().Where(i => path.StartsWith(i.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
		if (info == null) return DriveType.Unknown;
		return info.DriveType;
	}

	/// <summary>
	/// Check if a directory is the base of another
	/// </summary>
	/// <param name="root">Candidate root</param>
	/// <param name="child">Child folder</param>
	public static bool IsSubdirectoryOf(IDirectoryInfo root, IDirectoryInfo child)
	{
		var directoryPath = EndsWithSeparator(new Uri(child.FullName).AbsolutePath);
		var rootPath = EndsWithSeparator(new Uri(root.FullName).AbsolutePath);
		return directoryPath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Check if a directory is the base of another
	/// </summary>
	/// <param name="root">Candidate root</param>
	/// <param name="child">Child folder</param>
	public static bool IsSubdirectoryOf(string root, string child)
	{
		return IsSubdirectoryOf(_fs.DirectoryInfo.New(root), _fs.DirectoryInfo.New(child));
	}

	private static string EndsWithSeparator(string absolutePath)
	{
		return absolutePath?.TrimEnd('/', '\\') + "/";
	}

	/// <summary>
	/// Gets a unique file name if the file already exists.
	/// Source: https://stackoverflow.com/a/13050041
	/// </summary>
	public static string GetUniqueFilename(string fullPath)
	{
		if (!_fs.Path.IsPathRooted(fullPath))
			fullPath = _fs.Path.GetFullPath(fullPath);
		if (_fs.File.Exists(fullPath))
		{
			var filename = _fs.Path.GetFileName(fullPath);
			var path = fullPath[..^filename.Length];
			var filenameWOExt = _fs.Path.GetFileNameWithoutExtension(fullPath);
			var ext = _fs.Path.GetExtension(fullPath);
			var n = 1;
			do
			{
				fullPath = _fs.Path.Join(path, string.Format("{0} ({1}){2}", filenameWOExt, (n++), ext));
			}
			while (_fs.File.Exists(fullPath));
		}
		return fullPath;
	}


	public static readonly List<string> IgnoredPackageFiles = [
		"ReConHistory.txt",
		"dialoglog.txt",
		"errors.txt",
		"log.txt",
		"personallog.txt",
		"story_orphanqueries_found.txt",
		"goals.div",
		"goals.raw",
		"story.div",
		"story_ac.dat",
		"story_definitions.div",
		"story.div.osi",
		".ailog",
		".log",
		".debugInfo",
		".dmp",
	];

	private static bool IgnoreFile(string targetFilePath, string ignoredFileName)
	{
		if (_fs.Path.GetFileName(targetFilePath).Equals(ignoredFileName, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		else if (ignoredFileName.Substring(0) == "." && _fs.Path.GetExtension(targetFilePath).Equals(ignoredFileName, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return false;
	}
	#region Package Creation Async
	public static async Task<bool> CreatePackageAsync(string rootPath, List<string> inputPaths, string outputPath, CancellationToken token, List<string>? ignoredFiles = null)
	{
		try
		{
			ignoredFiles ??= IgnoredPackageFiles;

			if (token.IsCancellationRequested) return false;

			if (!rootPath.EndsWith(_fs.Path.DirectorySeparatorChar.ToString()))
			{
				rootPath += _fs.Path.DirectorySeparatorChar;
			}

			var conversionParams = ResourceConversionParameters.FromGameVersion(DivinityApp.GAME);

			var build = new PackageBuildData
			{
				Version = conversionParams.PAKVersion,
				Compression = CompressionMethod.LZ4,
				CompressionLevel = LSCompressionLevel.Default,
				Priority = 0,
			};

			foreach (var f in inputPaths)
			{
				if (token.IsCancellationRequested) break;
				AddFilesToPackage(f, build, rootPath, ignoredFiles, token);
			}

			DivinityApp.Log($"Writing package '{outputPath}'.");
			using var writer = PackageWriterFactory.Create(build, outputPath);
			writer.Write();
			return true;
		}
		catch (Exception ex)
		{
			if (!token.IsCancellationRequested)
			{
				DivinityApp.Log($"Error creating package: {ex}");
			}
			else
			{
				DivinityApp.Log($"Cancelled creating package: {ex}");
			}
			return false;
		}
	}

	private static void AddFilesToPackage(string filePath, PackageBuildData build, string rootPath, List<string> ignoredFiles, CancellationToken token)
	{
		if (!rootPath.EndsWith(_fs.Path.DirectorySeparatorChar.ToString()))
		{
			rootPath += _fs.Path.DirectorySeparatorChar;
		}

		if (_fs.Directory.Exists(filePath))
		{
			if (!filePath.EndsWith(_fs.Path.DirectorySeparatorChar.ToString()))
			{
				filePath += _fs.Path.DirectorySeparatorChar;
			}

			var files = EnumerateFiles(filePath, RecursiveOptions, (f) => !ignoredFiles.Any(x => IgnoreFile(f, x)))
				.ToDictionary(k => k.Replace(rootPath, string.Empty), v => v);

			foreach (var file in files)
			{
				if (token.IsCancellationRequested) break;
				var fileInfo = PackageBuildInputFile.CreateFromFilesystem(file.Value, file.Key);
				build.Files.Add(fileInfo);
			}
		}
		else if (_fs.File.Exists(filePath))
		{
			var name = _fs.Path.GetRelativePath(rootPath, filePath);
			var fileInfo = PackageBuildInputFile.CreateFromFilesystem(filePath, name);
			build.Files.Add(fileInfo);
		}
	}

	private static Task WritePackageAsync(PackageWriter writer, string outputPath, CancellationToken token)
	{
		var task = Task.Run(async () =>
		{
			// execute actual operation in child task
			var childTask = Task.Factory.StartNew(() =>
			{
				try
				{
					writer.Write();
				}
				catch (Exception)
				{
					// ignored because an exception on a cancellation request 
					// cannot be avoided if the stream gets disposed afterwards 
				}
			}, TaskCreationOptions.AttachedToParent);

			var awaiter = childTask.GetAwaiter();
			while (!awaiter.IsCompleted)
			{
				await Task.Delay(0, token);
			}
		}, token);

		return task;
	}
	#endregion

	public static bool ExtractPackages(IEnumerable<string> pakPaths, string outputDirectory)
	{
		var success = 0;
		var count = pakPaths.Count();
		foreach (var path in pakPaths)
		{
			try
			{
				//Put each pak into its own folder
				var destination = _fs.Path.Join(outputDirectory, _fs.Path.GetFileNameWithoutExtension(path));

				//Unless the foldername == the pak name and we're only extracting one pak
				if (count == 1 && _fs.Path.GetDirectoryName(outputDirectory).Equals(_fs.Path.GetFileNameWithoutExtension(path)))
				{
					destination = outputDirectory;
				}
				var packager = new Packager();
				packager.UncompressPackage(path, destination, null);
				success++;
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error extracting package: {ex}");
			}
		}
		return success >= count;
	}

	public static bool ExtractPackage(string pakPath, string outputDirectory)
	{
		try
		{
			var packager = new Packager();
			packager.UncompressPackage(pakPath, outputDirectory, null);
			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error extracting package: {ex}");
			return false;
		}
	}

	public static async Task<bool> ExtractPackageAsync(string pakPath, string outputDirectory, CancellationToken token, Func<PackagedFileInfo, bool>? filter = null)
	{
		var task = await Task.Run(async () =>
		{
			// execute actual operation in child task
			var childTask = Task.Factory.StartNew(() =>
			{
				try
				{
					var packager = new Packager();
					packager.UncompressPackage(pakPath, outputDirectory, filter);
					return true;
				}
				catch (Exception) { return false; }
			}, TaskCreationOptions.AttachedToParent);

			var awaiter = childTask.GetAwaiter();
			while (!awaiter.IsCompleted)
			{
				await Task.Delay(0, token);
			}
			return childTask.Result;
		}, token);

		return task;
	}

	public static bool WriteTextFile(string path, string contents)
	{
		try
		{
			_fs.File.WriteAllText(path, contents);
			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error writing file: {ex}");
			return false;
		}
	}

	public static async Task<bool> WriteTextFileAsync(string path, string contents, CancellationToken token)
	{
		try
		{
			await _fs.File.WriteAllTextAsync(path, contents, token);
			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error writing file: {ex}");
			return false;
		}
	}

	public static async Task<byte[]?> LoadFileAsBytesAsync(string path, CancellationToken token)
	{
		try
		{
			var result = await _fs.File.ReadAllBytesAsync(path, token);
			return result;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error writing file: {ex}");
		}
		return null;
	}

	public static async Task<bool> CopyFileAsync(string copyFromPath, string copyToPath, CancellationToken token)
	{
		try
		{
			await using var sourceFile = new FileStream(copyFromPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
			await using var outputFile = new FileStream(copyToPath, FileMode.Create, FileAccess.Read, FileShare.Read, 128000, FileOptions.Asynchronous);
			await sourceFile.CopyToAsync(outputFile, 128000, token); // 81920 default
			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error copying file: {ex}");
		}
		return false;
	}

	public static async Task<bool> CopyDirectoryAsync(string copyFromPath, string copyToPath, CancellationToken token)
	{
		try
		{
			copyFromPath = copyFromPath.NormalizeDirectorySep()!;
			copyToPath = copyToPath.NormalizeDirectorySep()!;

			var tasks = new List<Task>();
			//Recreate subdirectories
			foreach(var subDir in _fs.Directory.EnumerateDirectories(copyFromPath, "*", RecursiveOptions))
			{
				var outputDir = _fs.Path.Join(copyToPath, _fs.Path.GetRelativePath(copyFromPath, subDir));
				_fs.Directory.CreateDirectory(outputDir);
			}
			foreach (var filePath in _fs.Directory.EnumerateFiles(copyFromPath, "*", RecursiveOptions))
			{
				var outputFile = _fs.Path.Join(copyToPath, _fs.Path.GetRelativePath(copyFromPath, filePath));
				tasks.Add(CopyFileAsync(filePath, outputFile, token));
			}
			await Task.WhenAll(tasks).WaitAsync(token);
			return true;
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error copying directory: {ex}");
		}
		return false;
	}

	public static bool TryGetDirectoryOrParent(string path, out string parentDir)
	{
		parentDir = "";
		try
		{
			if (_fs.Directory.Exists(path))
			{
				parentDir = path;
				return true;
			}
			var dir = Directory.GetParent(path);
			if (dir != null)
			{
				parentDir = dir.FullName;
				return true;
			}
		}
		catch (Exception ex) { }
		return false;
	}

	private static readonly EnumerationOptions _defaultOpts = new() { AttributesToSkip = FileAttributes.Hidden };

	public static IEnumerable<string> EnumerateFiles(string path, EnumerationOptions? opts = null, Func<string, bool>? inclusionFilter = null)
	{
		opts ??= _defaultOpts;
		if (inclusionFilter != null)
		{
			return Directory.EnumerateFiles(path, "*", opts).Where(inclusionFilter);
		}
		return Directory.EnumerateFiles(path, "*", opts);
	}

	public static IEnumerable<string> EnumerateDirectories(string path, EnumerationOptions? opts = null, Func<string, bool>? inclusionFilter = null)
	{
		opts ??= _defaultOpts;
		if (inclusionFilter != null)
		{
			return Directory.EnumerateDirectories(path, "*", opts).Where(inclusionFilter);
		}
		return Directory.EnumerateDirectories(path, "*", opts);
	}

	private static readonly FileSystemRights _readAccessRights = FileSystemRights.Read | FileSystemRights.Synchronize;

	public static bool HasFileReadPermission(params string[] paths)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			foreach (var path in paths)
			{
				try
				{
					if (path.IsExistingFile() && new FileInfo(path) is FileInfo info)
					{
						var security = info.GetAccessControl();
						var usersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
						var rules = security.GetAccessRules(true, true, usersSid.GetType()).OfType<FileSystemAccessRule>();
						if (!rules.Any(r => r.FileSystemRights == _readAccessRights || r.FileSystemRights == FileSystemRights.FullControl))
						{
							DivinityApp.Log($"Lacking permission for file '{path}'");
							return false;
						}
					}
				}
				catch (UnauthorizedAccessException ex)
				{
					DivinityApp.Log($"Lacking permission for file '{path}':\n{ex}");
					return false;
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error checking permissions for '{path}':\n{ex}");
				}
			}
		}
		return true;
	}

	public static bool HasDirectoryReadPermission(params string[] paths)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			foreach (var path in paths)
			{
				try
				{
					if (path.IsExistingDirectory() && new DirectoryInfo(path) is DirectoryInfo info)
					{
						var security = info.GetAccessControl();
						var usersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
						var rules = security.GetAccessRules(true, true, usersSid.GetType()).OfType<FileSystemAccessRule>();
						if (!rules.Any(r => r.FileSystemRights == _readAccessRights || r.FileSystemRights == FileSystemRights.FullControl))
						{
							DivinityApp.Log($"Lacking permission for directory '{path}'. Rights({string.Join(";", rules.Select(x => x.FileSystemRights))})");
							return false;
						}
					}
				}
				catch (UnauthorizedAccessException ex)
				{
					DivinityApp.Log($"Lacking permission for directory '{path}':\n{ex}");
					return false;
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error checking permissions for '{path}':\n{ex}");
				}
			}
		}
		return true;
	}
}
