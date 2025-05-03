using System.IO.Abstractions;

namespace ModManager.Services;
/// <summary>
/// <para>Extends IFileSystem with additional helpers.</para>
/// IFileSystem: <inheritdoc cref="IFileSystem"/>
/// </summary>
public interface IFileSystemService : IFileSystem
{
	/// <summary>
	/// Creates the directory of a path if it doesn't exist.
	/// </summary>
	/// <param name="path">A given file path.</param>
	/// <exception cref="ArgumentNullException">Thrown if the path is null.</exception>
	/// <exception cref="ArgumentException">Thrown if the path is empty.</exception>
	void EnsureDirectoryExists(string path);

	/// <summary>
	/// Returns a file path without its file extension. This returns the full path, intead of just the file name.
	/// </summary>
	/// <param name="path">A given file path.</param>
	/// <exception cref="ArgumentNullException">Thrown if the path is null.</exception>
	/// <exception cref="ArgumentException">Thrown if the path is empty.</exception>
	/// <returns>A file path with no file extension.</returns>
	string GetPathWithoutExtension(string path);

	/// <summary>
	/// Ensures the path ends with the filesystem's directory separator character.
	/// </summary>
	/// <param name="path">A given file path.</param>
	/// <exception cref="ArgumentNullException">Thrown if the path is null.</exception>
	/// <exception cref="ArgumentException">Thrown if the path is empty.</exception>
	string GetPathWithFinalSeparator(string path);

	/// <summary>
	/// Replaces all invalid filesystem characters with a replacement character.
	/// </summary>
	/// <param name="fileName">The file name to sanitize.</param>
	/// <param name="replacementChar">The replacement character.</param>
	/// <returns>The sanitized file name.</returns>
	/// <exception cref="ArgumentNullException">Thrown if the path is null.</exception>
	/// <exception cref="ArgumentException">Thrown if the path is empty.</exception>
	string SanitizeFileName(string fileName, char replacementChar = '_');

	/// <summary>
	/// Expands environment variables and makes the path relative to the app directory if not rooted.
	/// </summary>
	/// <param name="path">The file path.</param>
	/// <returns>The expanded file path.</returns>
	string GetRealPath(string path);
}
