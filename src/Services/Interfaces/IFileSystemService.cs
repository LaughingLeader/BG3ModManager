using System.IO;
using System.IO.Abstractions;

namespace ModManager.Services;
/// <summary>
/// Extends IFileSystem with a few extra IO functions. IFileSystem provides wrappers around all IO methods.
/// Copyright Etienne Charland aka Hanuman MIT License
/// Source: https://github.com/mysteryx93/HanumanInstituteApps/tree/master
/// Tutorial: https://github.com/mysteryx93/Modern.Net-Tutorial/tree/main
/// </summary>
public interface IFileSystemService : IFileSystem
{
	/// <summary>
	/// Ensures the directory of specified path exists. If it doesn't exist, creates the directory.
	/// </summary>
	/// <param name="path">The absolute path to validate.</param>
	void EnsureDirectoryExists(string path);

	/// <summary>
	/// Deletes a file if it exists.
	/// </summary>
	/// <param name="path">The path of the file to delete.</param>
	void DeleteFileSilent(string path);

	/// <summary>
	/// Returns all files of specified extensions.
	/// </summary>
	/// <param name="path">The path in which to search.</param>
	/// <param name="extensions">A list of file extensions to return, each extension must include the dot.</param>
	/// <param name="searchOption">Specifies additional search options.</param>
	/// <returns>A list of files paths matching search conditions.</returns>
	IEnumerable<string> GetFilesByExtensions(string path, IEnumerable<string> extensions, SearchOption searchOption = SearchOption.TopDirectoryOnly);

	/// <summary>
	/// Returns specified path without its file extension.
	/// </summary>
	/// <param name="path">The path to truncate extension from.</param>
	/// <returns>A file path with no file extension.</returns>
	string GetPathWithoutExtension(string path);

	/// <summary>
	/// Returns the path ensuring it ends with a directory separator char.
	/// </summary>
	/// <param name="path">The path to end with a separator char.</param>
	/// <returns>A path that must end with a directory separator char.</returns>
	string GetPathWithFinalSeparator(string path);

	/// <summary>
	/// Replaces all illegal chars in specified file name with specified replacement character.
	/// </summary>
	/// <param name="fileName">The file name to sanitize.</param>
	/// <param name="replacementChar">The replacement character.</param>
	/// <returns></returns>
	string SanitizeFileName(string fileName, char replacementChar = '_');

	// /// <summary>
	// /// Send a file or path silently to the recycle bin. Suppress dialog, suppress errors, delete if too large.
	// /// </summary>
	// /// <param name="path">Location of directory or file to recycle.</param>
	// void MoveToRecycleBin(string path);
	//
	// /// <summary>
	// /// Sends a file or path to the recycle bin.
	// /// </summary>
	// /// <param name="displayWarning">Whether to display a warning if file is too large for the recycle bin.</param>
	// /// <param name="path">Location of directory or file to recycle.</param>
	// void MoveToRecycleBin(string path, bool displayWarning);
}
