using System.IO;

namespace ModManager.Util;

public class TempFile : IDisposable
{
	private readonly FileStream _stream;
	private readonly string _path;
	private readonly string _sourcePath;

	private readonly int _bufferSize;

	public FileStream Stream => _stream;
	public string FilePath => _path;
	public string SourceFilePath => _sourcePath;

	//128 KB since we're using asynchronous streams, default is 4 KB
	private TempFile(string sourcePath, int bufferSize = 128000)
	{
		_bufferSize = bufferSize;
		var tempDir = DivinityApp.GetAppDirectory("Temp");
		Directory.CreateDirectory(tempDir);
		_path = Path.Join(tempDir, Path.GetFileName(sourcePath));
		_sourcePath = sourcePath;
		_stream = File.Create(_path, _bufferSize, FileOptions.Asynchronous | FileOptions.DeleteOnClose);
	}

	public static async Task<TempFile> CreateAsync(string sourcePath, CancellationToken token)
	{
		var temp = new TempFile(sourcePath);
		await temp.CopyAsync(token);
		return temp;
	}

	public static async Task<TempFile> CreateAsync(string sourcePath, Stream sourceStream, CancellationToken token)
	{
		var temp = new TempFile(sourcePath);
		await temp.CopyAsync(sourceStream, token);
		return temp;
	}

	private async Task CopyAsync(CancellationToken token)
	{
		using var sourceStream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
		await sourceStream.CopyToAsync(_stream, _bufferSize, token);
	}

	private async Task CopyAsync(Stream sourceStream, CancellationToken token)
	{
		await sourceStream.CopyToAsync(_stream, _bufferSize, token);
	}

	public void Dispose()
	{
		_stream?.Dispose();
	}
}
