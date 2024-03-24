using AutoUpdaterDotNET;

using ModManager.Extensions;
using ModManager.Models;
using ModManager.Models.App;
using ModManager.Models.Mod;
using ModManager.Models.NexusMods;
using ModManager.Models.Settings;
using ModManager.Models.Updates;
using ModManager.ModUpdater.Cache;
using ModManager.Util;
using ModManager.Views.Main;
using ModManager.Windows;
using ModManager.ViewModels.Main;

using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;

using Microsoft.Win32;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Reactive.Bindings.Extensions;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Compressors.BZip2;
using SharpCompress.Compressors.Xz;
using SharpCompress.Readers;
using SharpCompress.Writers;

using Splat;

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ZstdSharp;
using System.Collections.Immutable;
using ModManager.ViewModels;

namespace ModManager.Services;

public class ModImportService(MainWindowViewModel mainVM)
{
	public MainWindowViewModel ViewModel => mainVM;
	public Window Window => mainVM.Window;

	private static ModManagerSettings Settings => Services.Settings.ManagerSettings;
	private static DivinityPathwayData Pathways => Services.Pathways.Data;

	private static readonly List<string> _archiveFormats = [".7z", ".7zip", ".gzip", ".rar", ".tar", ".tar.gz", ".zip"];
	private static readonly List<string> _compressedFormats = [".bz2", ".xz", ".zst"];
	private static readonly string _archiveFormatsStr = String.Join(";", _archiveFormats.Select(x => "*" + x));
	private static readonly string _compressedFormatsStr = String.Join(";", _compressedFormats.Select(x => "*" + x));

	private static readonly ArchiveEncoding _archiveEncoding = new(Encoding.UTF8, Encoding.UTF8);
	private static readonly ReaderOptions _importReaderOptions = new() { ArchiveEncoding = _archiveEncoding };
	private static readonly WriterOptions _exportWriterOptions = new(CompressionType.Deflate) { ArchiveEncoding = _archiveEncoding };

	public static bool IsImportableFile(string ext)
	{
		return ext == ".pak" || _archiveFormats.Contains(ext) || _compressedFormats.Contains(ext);
	}

	public static string GetInitialStartingDirectory(string prioritizePath = "")
	{
		var directory = prioritizePath;

		if (!String.IsNullOrEmpty(prioritizePath) && FileUtils.TryGetDirectoryOrParent(prioritizePath, out var actualDir))
		{
			directory = actualDir;
		}
		else
		{
			if (!String.IsNullOrEmpty(Settings.LastImportDirectoryPath))
			{
				directory = Settings.LastImportDirectoryPath;
			}

			if (!Directory.Exists(directory) && !String.IsNullOrEmpty(Pathways.LastSaveFilePath) && FileUtils.TryGetDirectoryOrParent(Pathways.LastSaveFilePath, out var lastDir))
			{
				directory = lastDir;
			}
		}

		if (String.IsNullOrEmpty(directory) || !Directory.Exists(directory))
		{
			directory = DivinityApp.GetAppDirectory();
		}

		return directory;
	}

	public async Task<ImportOperationResults> ImportModFromFile(Dictionary<string, DivinityModData> builtinMods, ImportOperationResults taskResult, string filePath, CancellationToken token, bool toActiveList = false)
	{
		var ext = Path.GetExtension(filePath).ToLower();
		if (ext.Equals(".pak", StringComparison.OrdinalIgnoreCase))
		{
			var outputFilePath = Path.Join(Pathways.AppDataModsPath, Path.GetFileName(filePath));
			try
			{
				taskResult.TotalPaks++;

				await FileUtils.CopyFileAsync(filePath, outputFilePath, token);

				if (File.Exists(outputFilePath))
				{
					var mod = await DivinityModDataLoader.LoadModDataFromPakAsync(outputFilePath, builtinMods, token);
					if (mod != null)
					{
						taskResult.Mods.Add(mod);
						await Observable.Start(() =>
						{
							ViewModel.AddImportedMod(mod, toActiveList);
						}, RxApp.MainThreadScheduler);
					}
				}
			}
			catch (System.IO.IOException ex)
			{
				DivinityApp.Log($"File may be in use by another process:\n{ex}");
				DivinityApp.ShowAlert($"Failed to copy file '{Path.GetFileName(filePath)} - It may be locked by another process'", AlertType.Danger);
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error reading file ({filePath}):\n{ex}");
			}
		}
		else
		{
			var importOptions = new ImportParameters(filePath, Services.Pathways.Data.AppDataModsPath, token, taskResult)
			{
				BuiltinMods = builtinMods,
				OnlyMods = true,
				Extension = ext,
				ReportProgress = amount => ViewModel.IncreaseMainProgressValue(amount),
				ShowAlert = ViewModel.ShowAlert
			};

			if (_archiveFormats.Contains(ext, StringComparer.OrdinalIgnoreCase))
			{
				await ImportUtils.ImportArchiveAsync(importOptions);
			}
			else if (_compressedFormats.Contains(ext, StringComparer.OrdinalIgnoreCase))
			{
				await ImportUtils.ImportCompressedFileAsync(importOptions);
			}

			if (importOptions.Result.Mods.Count > 0)
			{
				await Observable.Start(() =>
				{
					foreach (var mod in importOptions.Result.Mods)
					{
						ViewModel.AddImportedMod(mod, toActiveList);
					}
				}, RxApp.MainThreadScheduler);
			}
		}

		return taskResult;
	}

	public void ImportOrderFromArchive()
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".zip",
			Filter = $"Archive file (*.7z,*.rar;*.zip)|{_archiveFormatsStr}|All files (*.*)|*.*",
			Title = "Import Order & Mods from Archive...",
			ValidateNames = true,
			ReadOnlyChecked = true,
			Multiselect = false,
			InitialDirectory = GetInitialStartingDirectory(Settings.LastImportDirectoryPath)
		};

		if (dialog.ShowDialog(Window) == true)
		{
			var savedDirectory = Path.GetDirectoryName(dialog.FileName);
			if (Settings.LastImportDirectoryPath != savedDirectory)
			{
				Settings.LastImportDirectoryPath = savedDirectory;
				Pathways.LastSaveFilePath = savedDirectory;
				Services.Settings.TrySaveAll(out _);
			}
			//if(!Path.GetExtension(dialog.FileName).Equals(".zip", StringComparison.OrdinalIgnoreCase))
			//{
			//	view.AlertBar.SetDangerAlert($"Currently only .zip format archives are supported.", -1);
			//	return;
			//}
			ViewModel.MainProgressTitle = $"Importing mods from '{dialog.FileName}'.";
			ViewModel.MainProgressWorkText = "";
			ViewModel.MainProgressValue = 0d;
			ViewModel.MainProgressIsActive = true;
			var result = new ImportOperationResults()
			{
				TotalFiles = 1
			};
			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
				ViewModel.MainProgressToken = new CancellationTokenSource();

				var importOptions = new ImportParameters(dialog.FileName, Pathways.AppDataModsPath, ViewModel.MainProgressToken.Token, result)
				{
					BuiltinMods = builtinMods,
					OnlyMods = false,
					ReportProgress = amount => ViewModel.IncreaseMainProgressValue(amount),
					ShowAlert = ViewModel.ShowAlert
				};

				await ImportUtils.ImportArchiveAsync(importOptions);

				if (importOptions.Result.Mods.Count > 0)
				{
					await Observable.Start(() =>
					{
						foreach (var mod in importOptions.Result.Mods)
						{
							ViewModel.AddImportedMod(mod, false);
						}
					}, RxApp.MainThreadScheduler);
				}

				if (result.Mods.Count > 0 && result.Mods.Any(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START))
				{
					await Services.Get<IModUpdaterService>().NexusMods.Update(result.Mods, ViewModel.MainProgressToken.Token);
				}
				await ctrl.Yield();
				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					ViewModel.OnMainProgressComplete();

					if (result.Errors.Count > 0)
					{
						var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
						var errorOutputPath = DivinityApp.GetAppDirectory("_Logs", $"ImportOrderFromArchive_{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}_Errors.log");
						var logsDir = Path.GetDirectoryName(errorOutputPath);
						if (!Directory.Exists(logsDir))
						{
							Directory.CreateDirectory(logsDir);
						}
						File.WriteAllText(errorOutputPath, String.Join("\n", result.Errors.Select(x => $"File: {x.File}\nError:\n{x.Exception}")));
					}

					var messages = new List<string>();
					var total = result.Orders.Count + result.Mods.Count;

					if (total > 0)
					{
						if (result.Orders.Count > 0)
						{
							messages.Add($"{result.Orders.Count} order(s)");

							foreach (var order in result.Orders)
							{
								if (order.Name == "Current")
								{
									if (ViewModel.Views.ModOrder.SelectedModOrder?.IsModSettings == true)
									{
										ViewModel.Views.ModOrder.SelectedModOrder.SetFrom(order);
										ViewModel.Views.ModOrder.LoadModOrder();
									}
									else
									{
										var currentOrder = ViewModel.Views.ModOrder.ModOrderList.FirstOrDefault(x => x.IsModSettings);
										if (currentOrder != null)
										{
											ViewModel.Views.ModOrder.SelectedModOrder.SetFrom(currentOrder);
										}
									}
								}
								else
								{
									ViewModel.Views.ModOrder.AddNewModOrder(order);
								}
							}
						}
						if (result.Mods.Count > 0)
						{
							messages.Add($"{result.Mods.Count} mod(s)");
						}
						var msg = String.Join(", ", messages);
						DivinityApp.ShowAlert($"Imported {msg}", AlertType.Success, 20);
					}
					else
					{
						DivinityApp.ShowAlert($"Successfully extracted archive, but no mods or load orders were found", AlertType.Warning, 20);
					}
				});
				return Disposable.Empty;
			});
		}
	}

	private async Task<ModuleInfo> TryGetMetaFromZipAsync(string filePath, CancellationToken token)
	{
		TempFile tempFile = null;
		try
		{
			using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
			await fileStream.ReadAsync(new byte[fileStream.Length], 0, (int)fileStream.Length);
			fileStream.Position = 0;

			var modManager = Services.Mods;

			using var archive = ArchiveFactory.Open(fileStream, _importReaderOptions);
			foreach (var file in archive.Entries)
			{
				if (token.IsCancellationRequested) return null;
				if (!file.IsDirectory)
				{
					if (file.Key.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
					{
						using var entryStream = file.OpenEntryStream();
						tempFile = await TempFile.CreateAsync(String.Join("\\", filePath, file.Key), entryStream, token);
						var meta = DivinityModDataLoader.TryGetMetaFromPakFileStream(tempFile.Stream, filePath, token);
						if (meta == null)
						{
							var pakName = Path.GetFileNameWithoutExtension(file.Key);
							if (modManager.ModExists(pakName))
							{
								return new ModuleInfo
								{
									UUID = pakName,
								};
							}
						}
						else
						{
							return meta;
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error reading zip:\n{ex}");
		}
		finally
		{
			tempFile?.Dispose();
		}

		return null;
	}

	private async Task<ModuleInfo> TryGetMetaFromCompressedFileAsync(string filePath, string extension, CancellationToken token)
	{
		ModuleInfo result = null;
		using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
		{
			await fileStream.ReadAsync(new byte[fileStream.Length], 0, (int)fileStream.Length);
			fileStream.Position = 0;

			System.IO.Stream decompressionStream = null;
			TempFile tempFile = null;

			try
			{
				switch (extension)
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
					tempFile = await TempFile.CreateAsync(filePath, decompressionStream, token);
					result = DivinityModDataLoader.TryGetMetaFromPakFileStream(tempFile.Stream, filePath, token);
					if (result == null)
					{
						var pakName = Path.GetFileNameWithoutExtension(filePath);
						if (Services.Mods.ModExists(pakName))
						{
							result = new ModuleInfo
							{
								UUID = pakName,
							};
						}
					}
				}
			}
			finally
			{
				decompressionStream?.Dispose();
				tempFile?.Dispose();
			}
		}
		return result;
	}

	private async Task<bool> FetchNexusModsIdFromFilesAsync(List<string> files, ImportOperationResults results, CancellationToken token)
	{
		foreach (var filePath in files)
		{
			try
			{
				if (token.IsCancellationRequested) break;

				var ext = Path.GetExtension(filePath).ToLower();

				var isArchive = _archiveFormats.Contains(ext, StringComparer.OrdinalIgnoreCase);
				var isCompressedFile = !isArchive && _compressedFormats.Contains(ext, StringComparer.OrdinalIgnoreCase);

				if (isArchive || isCompressedFile)
				{
					var info = NexusModFileVersionData.FromFilePath(filePath);
					if (info.Success)
					{
						ModuleInfo meta = null;
						if (isArchive)
						{
							meta = await TryGetMetaFromZipAsync(filePath, token);
						}
						else if (isCompressedFile)
						{
							meta = await TryGetMetaFromCompressedFileAsync(filePath, ext, token);
						}

						if (meta != null && Services.Mods.TryGetMod(meta.UUID, out var mod))
						{
							mod.NexusModsData.SetModVersion(info);
							results.Mods.Add(mod);
						}
					}
				}
			}
			catch (Exception ex)
			{
				DivinityApp.Log($"Error fetching info:\n{ex}");
				results.AddError(filePath, ex);
			}
		}

		if (results.Success)
		{
			DivinityApp.Log($"Updated NexusMods mod ids for ({results.Mods.Count}) mod(s).");
			await Services.Updater.NexusMods.Update(results.Mods, token);
			await Services.Updater.NexusMods.SaveCacheAsync(false, ViewModel.Version, token);
		}
		return results.Success;
	}

	public void ImportMods(IEnumerable<string> files, bool toActiveList = false)
	{
		if (!ViewModel.MainProgressIsActive)
		{
			ViewModel.MainProgressTitle = "Importing mods.";
			ViewModel.MainProgressWorkText = "";
			ViewModel.MainProgressValue = 0d;
			ViewModel.MainProgressIsActive = true;

			var result = new ImportOperationResults()
			{
				TotalFiles = files.Count()
			};

			RxApp.TaskpoolScheduler.ScheduleAsync(async (ctrl, t) =>
			{
				var builtinMods = DivinityApp.IgnoredMods.SafeToDictionary(x => x.Folder, x => x);
				ViewModel.MainProgressToken = new CancellationTokenSource();
				foreach (var f in files)
				{
					await ImportModFromFile(builtinMods, result, f, ViewModel.MainProgressToken.Token, toActiveList);
				}

				if (Services.Updater.NexusMods.IsEnabled && result.Mods.Count > 0 && result.Mods.Any(x => x.NexusModsData.ModId >= DivinityApp.NEXUSMODS_MOD_ID_START))
				{
					await Services.Updater.NexusMods.Update(result.Mods, ViewModel.MainProgressToken.Token);
					await Services.Updater.NexusMods.SaveCacheAsync(false, ViewModel.Version, ViewModel.MainProgressToken.Token);
				}

				await ctrl.Yield();
				RxApp.MainThreadScheduler.Schedule(_ =>
				{
					ViewModel.OnMainProgressComplete();

					if (result.Errors.Count > 0)
					{
						var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
						var errorOutputPath = DivinityApp.GetAppDirectory("_Logs", $"ImportMods_{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}_Errors.log");
						var logsDir = Path.GetDirectoryName(errorOutputPath);
						if (!Directory.Exists(logsDir))
						{
							Directory.CreateDirectory(logsDir);
						}
						File.WriteAllText(errorOutputPath, String.Join("\n", result.Errors.Select(x => $"File: {x.File}\nError:\n{x.Exception}")));
					}

					var total = result.Mods.Count;
					if (result.Success)
					{
						if (result.Mods.Count > 1)
						{
							DivinityApp.ShowAlert($"Successfully imported {total} mods", AlertType.Success, 20);
						}
						else if (total == 1)
						{
							var modFileName = result.Mods.First().FileName;
							var fileNames = String.Join(", ", files.Select(x => Path.GetFileName(x)));
							DivinityApp.ShowAlert($"Successfully imported '{modFileName}' from '{fileNames}'", AlertType.Success, 20);
						}
						else
						{
							DivinityApp.ShowAlert("Skipped importing mod - No .pak file found", AlertType.Success, 20);
						}
					}
					else
					{
						if (total == 0)
						{
							DivinityApp.ShowAlert("No mods imported. Does the file contain a .pak?", AlertType.Warning, 60);
						}
						else
						{
							DivinityApp.ShowAlert($"Only imported {total}/{result.TotalPaks} mods - Check the log", AlertType.Danger, 60);
						}
					}
				});
				return Disposable.Empty;
			});
		}
	}

	public void OpenModImportDialog()
	{
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".zip",
			Filter = $"All formats (*.pak;{_archiveFormatsStr};{_compressedFormatsStr})|*.pak;{_archiveFormatsStr};{_compressedFormatsStr}|Mod package (*.pak)|*.pak|Archive file ({_archiveFormatsStr})|{_archiveFormatsStr}|Compressed file ({_compressedFormatsStr})|{_compressedFormatsStr}|All files (*.*)|*.*",
			Title = "Import Mods from Archive...",
			ValidateNames = true,
			ReadOnlyChecked = true,
			Multiselect = true,
			InitialDirectory = GetInitialStartingDirectory(Settings.LastImportDirectoryPath)
		};

		if (dialog.ShowDialog(Window) == true)
		{
			var savedDirectory = Path.GetDirectoryName(dialog.FileName);
			if (Settings.LastImportDirectoryPath != savedDirectory)
			{
				Settings.LastImportDirectoryPath = savedDirectory;
				Services.Pathways.Data.LastSaveFilePath = savedDirectory;
				Services.Settings.TrySaveAll(out _);
			}

			ImportMods(dialog.FileNames);
		}
	}

	public void OpenModIdsImportDialog()
	{
		//Filter = $"All formats (*.pak;{_archiveFormatsStr};{_compressedFormatsStr})|*.pak;{_archiveFormatsStr};{_compressedFormatsStr}|Mod package (*.pak)|*.pak|Archive file ({_archiveFormatsStr})|{_archiveFormatsStr}|Compressed file ({_compressedFormatsStr})|{_compressedFormatsStr}|All files (*.*)|*.*",
		var dialog = new OpenFileDialog
		{
			CheckFileExists = true,
			CheckPathExists = true,
			DefaultExt = ".zip",
			Filter = $"All formats ({_archiveFormatsStr};{_compressedFormatsStr})|{_archiveFormatsStr};{_compressedFormatsStr}|Archive file ({_archiveFormatsStr})|{_archiveFormatsStr}|Compressed file ({_compressedFormatsStr})|{_compressedFormatsStr}|All files (*.*)|*.*",
			Title = "Import NexusMods ModId(s) from Archive(s)...",
			ValidateNames = true,
			ReadOnlyChecked = true,
			Multiselect = true,
			InitialDirectory = GetInitialStartingDirectory(Settings.LastImportDirectoryPath)
		};

		if (dialog.ShowDialog(Window) == true)
		{
			var savedDirectory = Path.GetDirectoryName(dialog.FileName);
			if (Settings.LastImportDirectoryPath != savedDirectory)
			{
				Settings.LastImportDirectoryPath = savedDirectory;
				Services.Pathways.Data.LastSaveFilePath = savedDirectory;
				Services.Settings.TrySaveAll(out _);
			}

			var files = dialog.FileNames.ToList();

			if (!ViewModel.MainProgressIsActive)
			{
				ViewModel.MainProgressTitle = "Parsing files for NexusMods ModIds...";
				ViewModel.MainProgressWorkText = "";
				ViewModel.MainProgressValue = 0d;
				ViewModel.MainProgressIsActive = true;
				var result = new ImportOperationResults()
				{
					TotalFiles = files.Count
				};

				RxApp.TaskpoolScheduler.ScheduleAsync(async (sch, t) =>
				{
					ViewModel.MainProgressToken = new CancellationTokenSource();

					await FetchNexusModsIdFromFilesAsync(files, result, ViewModel.MainProgressToken.Token);

					RxApp.MainThreadScheduler.Schedule(_ =>
					{
						ViewModel.OnMainProgressComplete();

						if (result.Errors.Count > 0)
						{
							var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
							var errorOutputPath = DivinityApp.GetAppDirectory("_Logs", $"ImportNexusModsModIds_{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}_Errors.log");
							var logsDir = Path.GetDirectoryName(errorOutputPath);
							if (!Directory.Exists(logsDir))
							{
								Directory.CreateDirectory(logsDir);
							}
							File.WriteAllText(errorOutputPath, String.Join("\n", result.Errors.Select(x => $"File: {x.File}\nError:\n{x.Exception}")));
						}

						var total = result.Mods.Count;
						if (result.Success)
						{
							if (result.Mods.Count > 1)
							{
								DivinityApp.ShowAlert($"Successfully imported NexusMods ids for {total} mods", AlertType.Success, 20);
							}
							else if (total == 1)
							{
								DivinityApp.ShowAlert($"Successfully imported the NexusMods id for '{result.Mods.First().Name}'", AlertType.Success, 20);
							}
							else
							{
								DivinityApp.ShowAlert("No NexusMods ids found", AlertType.Success, 20);
							}
						}
						else
						{
							if (total == 0)
							{
								DivinityApp.ShowAlert("No NexusMods ids found. Does the .zip name contain an id, with a .pak file inside?", AlertType.Warning, 60);
							}
							else if (result.Errors.Count > 0)
							{
								DivinityApp.ShowAlert($"Encountered some errors fetching ids - Check the log", AlertType.Danger, 60);
							}
						}
					});
					return Disposable.Empty;
				});
			}
		}
	}

	#region Exporting

	private static Task WriteZipAsync(IWriter writer, string entryName, string source, CancellationToken token)
	{
		if (token.IsCancellationRequested)
		{
			return Task.FromCanceled(token);
		}

		var task = Task.Run(async () =>
		{
			// execute actual operation in child task
			var childTask = Task.Factory.StartNew(() =>
			{
				try
				{
					writer.Write(entryName, source);
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

	public async Task<bool> ExportLoadOrderToArchiveAsync(DivinityProfileData selectedProfile, DivinityLoadOrder selectedModOrder, string outputPath, CancellationToken token)
	{
		var success = false;
		if (selectedProfile != null && selectedModOrder != null)
		{
			var sysFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern.Replace("/", "-");
			var gameDataFolder = Path.GetFullPath(Settings.GameDataPath);
			var tempDir = DivinityApp.GetAppDirectory("Temp");
			Directory.CreateDirectory(tempDir);

			if (String.IsNullOrEmpty(outputPath))
			{
				var baseOrderName = selectedModOrder.Name;
				if (selectedModOrder.IsModSettings)
				{
					baseOrderName = $"{selectedProfile.Name}_{selectedModOrder.Name}";
				}
				var outputDir = DivinityApp.GetAppDirectory("Export");
				Directory.CreateDirectory(outputDir);
				outputPath = Path.Join(outputDir, $"{baseOrderName}-{DateTime.Now.ToString(sysFormat + "_HH-mm-ss")}.zip");
			}

			var modManager = Services.Mods;

			var modPaks = new List<DivinityModData>(modManager.AllMods.Where(x => selectedModOrder.Order.Any(o => o.UUID == x.UUID)));
			modPaks.AddRange(modManager.ForceLoadedMods.Where(x => !x.IsForceLoadedMergedMod));

			var incrementProgress = 1d / modPaks.Count;

			try
			{
				using var stream = File.OpenWrite(outputPath);
				using var zipWriter = WriterFactory.Open(stream, ArchiveType.Zip, _exportWriterOptions);

				var orderFileName = DivinityModDataLoader.MakeSafeFilename(Path.Join(selectedModOrder.Name + ".json"), '_');
				var contents = JsonConvert.SerializeObject(selectedModOrder, Newtonsoft.Json.Formatting.Indented);

				using var ms = new System.IO.MemoryStream();
				using var swriter = new System.IO.StreamWriter(ms);

				await swriter.WriteAsync(contents);
				swriter.Flush();
				ms.Position = 0;
				zipWriter.Write(orderFileName, ms);

				foreach (var mod in modPaks)
				{
					if (token.IsCancellationRequested) return false;
					if (!mod.IsEditorMod)
					{
						var fileName = Path.GetFileName(mod.FilePath);
						await WriteZipAsync(zipWriter, fileName, mod.FilePath, token);
					}
					else
					{
						var outputPackage = Path.ChangeExtension(Path.Join(tempDir, mod.Folder), "pak");
						//Imported Classic Projects
						if (!mod.Folder.Contains(mod.UUID))
						{
							outputPackage = Path.ChangeExtension(Path.Join(tempDir, mod.Folder + "_" + mod.UUID), "pak");
						}

						var sourceFolders = new List<string>();

						var modsFolder = Path.Join(gameDataFolder, $"Mods/{mod.Folder}");
						var publicFolder = Path.Join(gameDataFolder, $"Public/{mod.Folder}");

						if (Directory.Exists(modsFolder)) sourceFolders.Add(modsFolder);
						if (Directory.Exists(publicFolder)) sourceFolders.Add(publicFolder);

						DivinityApp.Log($"Creating package for editor mod '{mod.Name}' - '{outputPackage}'.");

						if (await FileUtils.CreatePackageAsync(gameDataFolder, sourceFolders, outputPackage, token, FileUtils.IgnoredPackageFiles))
						{
							var fileName = Path.GetFileName(outputPackage);
							await WriteZipAsync(zipWriter, fileName, outputPackage, token);
							File.Delete(outputPackage);
						}
					}

					await ViewModel.IncreaseMainProgressValueAsync(incrementProgress);
				}

				RxApp.MainThreadScheduler.Schedule(() =>
				{
					DivinityApp.ShowAlert($"Exported load order to '{outputPath}'", AlertType.Success, 15);
					var dir = Path.GetFullPath(Path.GetDirectoryName(outputPath));
					if (Directory.Exists(dir))
					{
						FileUtils.TryOpenPath(dir);
					}
				});

				success = true;
			}
			catch (Exception ex)
			{
				RxApp.MainThreadScheduler.Schedule(() =>
				{
					string msg = $"Error writing load order archive '{outputPath}': {ex}";
					DivinityApp.Log(msg);
					DivinityApp.ShowAlert(msg, AlertType.Danger);
				});
			}

			Directory.Delete(tempDir);
		}
		else
		{
			RxApp.MainThreadScheduler.Schedule(() =>
			{
				DivinityApp.ShowAlert("SelectedProfile or SelectedModOrder is null! Failed to export mod order", AlertType.Danger);
			});
		}

		return success;
	}

	#endregion
}
