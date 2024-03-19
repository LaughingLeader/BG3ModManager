using DivinityModManager.Models;
using DivinityModManager.Models.App;
using DivinityModManager.Models.Mod;
using DivinityModManager.Models.NexusMods;

using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers;
using SharpCompress.Writers;

using System.IO;
using System.Reactive.Linq;
using System.Text;

using ZstdSharp;

namespace DivinityModManager.Util;

public struct ImportedJsonFile
{
	public string FileName;
	public string Text;
}

public class ImportParameters
{
	public string FilePath;

	private string _ext;
	public string Extension
	{
		get
		{
			if (_ext == null) _ext = Path.GetExtension(FilePath).ToLower();
			return _ext;
		}
		set => _ext = value?.ToLower();
	}
	public string OutputDirectory;
	public bool OnlyMods;
	public readonly CancellationToken Token;
	public Action<double> ReportProgress;
	public Action<string, AlertType, int> ShowAlert;
	public ImportOperationResults Result;
	public Dictionary<string, DivinityModData> BuiltinMods;
	public List<ImportedJsonFile> ImportedJsonFiles;

	public ImportParameters(string filePath, string outputDirectory, CancellationToken token, ImportOperationResults result = null)
	{
		Result = result ?? new ImportOperationResults();
		ImportedJsonFiles = new List<ImportedJsonFile>();

		FilePath = filePath;
		OutputDirectory = outputDirectory;
		Token = token;
	}
}

public static class ImportUtils
{
	private const int ARCHIVE_BUFFER = 128000;

	private static readonly ArchiveEncoding _archiveEncoding = new(Encoding.UTF8, Encoding.UTF8);
	private static readonly ReaderOptions _importReaderOptions = new() { ArchiveEncoding = _archiveEncoding };
	private static readonly WriterOptions _exportWriterOptions = new(CompressionType.Deflate) { ArchiveEncoding = _archiveEncoding };

	private static readonly List<string> _archiveFormats = new() { ".7z", ".7zip", ".gzip", ".rar", ".tar", ".tar.gz", ".zip" };
	private static readonly List<string> _compressedFormats = new() { ".bz2", ".xz", ".zst" };
	private static readonly string _archiveFormatsStr = String.Join(";", _archiveFormats.Select(x => "*" + x));
	private static readonly string _compressedFormatsStr = String.Join(";", _compressedFormats.Select(x => "*" + x));

	public static async Task<bool> ImportArchiveAsync(ImportParameters options)
	{
		double taskStepAmount = 1.0 / 4;
		var success = false;
		try
		{
			using var fileStream = new FileStream(options.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
			if (fileStream != null)
			{
				var info = NexusModFileVersionData.FromFilePath(options.FilePath);

				await fileStream.ReadAsync(new byte[fileStream.Length], 0, (int)fileStream.Length);
				fileStream.Position = 0;
				options.ReportProgress?.Invoke(taskStepAmount);
				using var archive = ArchiveFactory.Open(fileStream, _importReaderOptions);
				foreach (var file in archive.Entries)
				{
					if (options.Token.IsCancellationRequested) return false;
					if (!file.IsDirectory)
					{
						if (file.Key.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
						{
							var outputName = Path.GetFileName(file.Key);
							var outputFilePath = Path.Join(options.OutputDirectory, outputName);
							options.Result.TotalPaks++;
							using (var entryStream = file.OpenEntryStream())
							{
								using var fs = File.Create(outputFilePath, ARCHIVE_BUFFER, FileOptions.Asynchronous);
								try
								{
									await entryStream.CopyToAsync(fs, ARCHIVE_BUFFER, options.Token);
									success = true;
								}
								catch (Exception ex)
								{
									options.Result.AddError(outputFilePath, ex);
									DivinityApp.Log($"Error copying file '{file.Key}' from archive to '{outputFilePath}':\n{ex}");
								}
							}

							if (success)
							{
								var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, options.BuiltinMods, options.Token);
								if (mod != null)
								{
									options.Result.Mods.Add(mod);
									mod.NexusModsData.SetModVersion(info);
								}
							}
						}
						else if (file.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
						{
							using var entryStream = file.OpenEntryStream();
							try
							{
								int length = (int)file.Size;
								var result = new byte[length];
								await entryStream.ReadAsync(result, 0, length);
								string text = Encoding.UTF8.GetString(result);
								if (!String.IsNullOrWhiteSpace(text))
								{
									options.ImportedJsonFiles.Add(new ImportedJsonFile { FileName = Path.GetFileNameWithoutExtension(file.Key), Text = text });
								}
							}
							catch (Exception ex)
							{
								options.Result.AddError(file.Key, ex);
								DivinityApp.Log($"Error reading json file '{file.Key}' from archive:\n{ex}");
							}
						}
					}
				}

				options.ReportProgress?.Invoke(taskStepAmount);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error extracting package: {ex}");
			options.Result.AddError(options.FilePath, ex);
			options.ShowAlert?.Invoke($"Error extracting archive (check the log): {ex.Message}", AlertType.Danger, 0);
		}
		options.ReportProgress?.Invoke(taskStepAmount);

		if (!options.OnlyMods && options.ImportedJsonFiles.Count > 0)
		{
			foreach (var entry in options.ImportedJsonFiles)
			{
				DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(entry.Text);
				if (order != null)
				{
					options.Result.Orders.Add(order);
					order.Name = entry.FileName;
					DivinityApp.Log($"Imported mod order from archive: {String.Join(@"\n\t", order.Order.Select(x => x.Name))}");
				}
			}
		}
		options.ReportProgress?.Invoke(taskStepAmount);
		await Task.Yield();
		return success;
	}

	public static async Task<bool> ImportCompressedFileAsync(ImportParameters options)
	{
		FileStream fileStream = null;
		double taskStepAmount = 1.0 / 4;
		var success = false;
		try
		{
			fileStream = new FileStream(options.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
			if (fileStream != null)
			{
				var info = NexusModFileVersionData.FromFilePath(options.FilePath);

				await fileStream.ReadAsync(new byte[fileStream.Length], 0, (int)fileStream.Length);
				fileStream.Position = 0;
				options.ReportProgress?.Invoke(taskStepAmount);
				System.IO.Stream decompressionStream = null;
				TempFile tempFile = null;

				try
				{
					switch (options.Extension)
					{
						case ".bz2":
							decompressionStream = new BZip2Stream(fileStream, SharpCompress.Compressors.CompressionMode.Decompress, true);
							break;
						case ".xz":
							decompressionStream = new XZStream(fileStream);
							break;
						case ".zst":
							decompressionStream = new DecompressionStream(fileStream);
							break;
					}
					if (decompressionStream != null)
					{
						DivinityApp.Log($"Checking if compressed file ({options.FilePath} => {options.Extension}) is a pak.");
						var outputName = Path.GetFileNameWithoutExtension(options.FilePath);
						if (!outputName.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) outputName += ".pak";
						var outputFilePath = Path.Join(options.OutputDirectory, outputName);

						tempFile = await TempFile.CreateAsync(options.FilePath, decompressionStream, options.Token);

						try
						{
							var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(tempFile.Stream, outputFilePath, options.BuiltinMods, options.Token);
							if (mod != null)
							{
								try
								{
									mod.LastModified = File.GetLastWriteTime(options.FilePath);
									mod.LastUpdated = mod.LastModified;
								}
								catch (Exception ex)
								{
									DivinityApp.Log($"Error getting pak last modified date for '{ex}': {ex}");
								}

								if (!outputName.Contains(mod.Name))
								{
									var nameFromMeta = $"{mod.Folder}.pak";
									outputFilePath = Path.Join(options.OutputDirectory, nameFromMeta);
									mod.FilePath = outputFilePath;
								}
								using (var fs = File.Create(outputFilePath, ARCHIVE_BUFFER, FileOptions.Asynchronous))
								{
									try
									{
										await tempFile.Stream.CopyToAsync(fs, ARCHIVE_BUFFER, options.Token);
										success = true;
									}
									catch (Exception ex)
									{
										options.Result.AddError(outputFilePath, ex);
										DivinityApp.Log($"Error copying file '{outputName}' from archive to '{outputFilePath}':\n{ex}");
									}
								}

								if (success)
								{
									options.Result.TotalPaks++;
									options.Result.Mods.Add(mod);
									mod.NexusModsData.SetModVersion(info);
								}
							}
						}
						catch (Exception ex)
						{
							DivinityApp.Log($"Error reading decompressed file '{options.FilePath}' as pak:\n{ex}");
						}
					}
				}
				catch (Exception ex)
				{
					DivinityApp.Log($"Error reading file '{options.FilePath}':\n{ex}");
				}
				finally
				{
					decompressionStream?.Dispose();
					tempFile?.Dispose();
				}

				options.ReportProgress?.Invoke(taskStepAmount);
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error extracting package: {ex}");
			options.Result.AddError(options.FilePath, ex);
			options.ShowAlert?.Invoke($"Error extracting archive (check the log): {ex.Message}", AlertType.Danger, 0);
		}
		finally
		{
			fileStream?.Close();
			options.ReportProgress?.Invoke(taskStepAmount);

			if (!options.OnlyMods && options.ImportedJsonFiles.Count > 0)
			{
				foreach (var entry in options.ImportedJsonFiles)
				{
					DivinityLoadOrder order = DivinityJsonUtils.SafeDeserialize<DivinityLoadOrder>(entry.Text);
					if (order != null)
					{
						options.Result.Orders.Add(order);
						order.Name = entry.FileName;
						DivinityApp.Log($"Imported mod order from archive: {String.Join(@"\n\t", order.Order.Select(x => x.Name))}");
					}
				}
			}
			options.ReportProgress?.Invoke(taskStepAmount);
		}
		await Task.Yield();
		return success;
	}

	public static async Task<bool> ImportPakAsync(ImportParameters options)
	{
		var outputFilePath = Path.Join(options.OutputDirectory, Path.GetFileName(options.FilePath));
		try
		{
			options.Result.TotalPaks++;

			await FileUtils.CopyFileAsync(options.FilePath, outputFilePath, options.Token);

			if (File.Exists(outputFilePath))
			{
				var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, options.BuiltinMods, options.Token);
				if (mod != null)
				{
					options.Result.Mods.Add(mod);
					return true;
				}
			}
		}
		catch (System.IO.IOException ex)
		{
			DivinityApp.Log($"File may be in use by another process:\n{ex}");
			options.ShowAlert?.Invoke($"Failed to copy file '{options.FilePath} - It may be locked by another process'", AlertType.Danger, 0);
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error reading file ({options.FilePath}):\n{ex}");
		}
		return false;
	}

	/// <summary>
	/// Figures out if the file can be imported via the file extension, and calling ImportPakAsync/ImportArchiveAsync/ImportCompressedFileAsync.
	/// </summary>
	public static async Task<bool> ImportFileAsync(ImportParameters options)
	{
		if (options.Extension.Equals(".pak"))
		{
			return await ImportPakAsync(options);
		}
		else if (_archiveFormats.Contains(options.Extension))
		{
			return await ImportArchiveAsync(options);
		}
		else if (_compressedFormats.Contains(options.Extension))
		{
			return await ImportCompressedFileAsync(options);
		}
		options.Result.AddError(options.FilePath, new NotImplementedException($"[ImportFileAsync] File type not handled: {options.Extension}"));
		return false;
	}
}
