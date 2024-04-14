﻿using ModManager.Util;

using System.IO.Compression;

namespace ModManager.Models.Updates;

public enum ModDownloadPathType
{
	FILE,
	URL
}

public struct ModDownloadResult
{
	public bool Success;
	public string? OutputFilePath;
}

public class ModDownloadData : ReactiveObject
{
	[Reactive] public string? DownloadPath { get; set; }
	[Reactive] public ModDownloadPathType DownloadPathType { get; set; }
	[Reactive] public ModSourceType DownloadSourceType { get; set; }
	[Reactive] public DateTimeOffset? Date { get; set; }
	[Reactive] public string? Version { get; set; }
	[Reactive] public string? Description { get; set; }
	[Reactive] public bool IsIndirectDownload { get; set; }

	private static bool FileNamesMatch(string localFilePath, string newFilePath) => Path.GetFileName(localFilePath).Equals(Path.GetFileName(newFilePath), StringComparison.OrdinalIgnoreCase);

	private static void MoveOldPakToRecycleBin(string previousFilePath, string newFilePath)
	{
		if (!String.IsNullOrEmpty(previousFilePath) && FileNamesMatch(previousFilePath, newFilePath))
		{
			RecycleBinHelper.DeleteFile(previousFilePath, false, false);
		}
	}

	private static System.IO.Stream MakeFileStream(string path) => new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, true);

	public async Task<ModDownloadResult> DownloadAsync(string previousFilePath, string outputDirectory, CancellationToken token)
	{
		var result = new ModDownloadResult();
		try
		{
			Directory.CreateDirectory(outputDirectory);
			DivinityApp.Log($"Downloading {DownloadPath} - DownloadPathType({DownloadPathType}) DownloadSourceType({DownloadSourceType})");
			if (DownloadPathType == ModDownloadPathType.FILE)
			{
				var outputFilePath = Path.Join(outputDirectory, DownloadPath);
				//This covers when an update changes the pak name
				MoveOldPakToRecycleBin(previousFilePath, outputFilePath);
				await FileUtils.CopyFileAsync(DownloadPath, outputFilePath, token);
				result.Success = true;
				result.OutputFilePath = outputFilePath;
				return result;
			}
			else if (DownloadPathType == ModDownloadPathType.URL)
			{
				if (IsIndirectDownload)
				{
					//Nexus non-premium users need to go to the website and get a nxm:// link to have download authorization.
					FileUtils.TryOpenPath(DownloadPath);
					result.Success = true;
					result.OutputFilePath = DownloadPath;
					return result;
				}
				else
				{
					using var webStream = await WebHelper.DownloadFileAsStreamAsync(DownloadPath, token);
					if (webStream == null) return result;

					if (DownloadPath.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
					{
						var outputFilePath = Path.Join(outputDirectory, Path.GetFileName(DownloadPath));
						MoveOldPakToRecycleBin(previousFilePath, outputFilePath);
						using var outputFile = MakeFileStream(outputFilePath);
						await webStream.CopyToAsync(outputFile, 128000, token);
						result.Success = true;
						result.OutputFilePath = outputFilePath;
					}
					else
					{
						var archive = new ZipArchive(webStream);
						foreach (var entry in archive.Entries)
						{
							if (entry.Name.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
							{
								using var entryStream = entry.Open();
								var outputFilePath = Path.Join(outputDirectory, Path.GetFileName(entry.Name));
								MoveOldPakToRecycleBin(previousFilePath, outputFilePath);
								using var outputFile = MakeFileStream(outputFilePath);
								await entryStream.CopyToAsync(outputFile, 128000, token);
								result.Success = true;
								result.OutputFilePath = outputFilePath;
							}
						}
					}
				}
				return result;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error downloading update ({DownloadPath}): {ex}");
		}
		return result;
	}
}
