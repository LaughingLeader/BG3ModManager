using LSLib.LS;

using System.IO;

namespace ModManager.Util;

public static class DivinitySaveTools
{
	public static bool RenameSave(string pathToSave, string newName)
	{
		try
		{
			string baseOldName = Path.GetFileNameWithoutExtension(pathToSave);
			string baseNewName = Path.GetFileNameWithoutExtension(newName);
			string output = Path.ChangeExtension(Path.Join(Path.GetDirectoryName(pathToSave), newName), ".lsv");

			var reader = new PackageReader();
			using var package = reader.Read(pathToSave);
			var saveScreenshotImage = package.Files.FirstOrDefault(p => p.Name.EndsWith(".WebP"));
			if (saveScreenshotImage != null)
			{
				saveScreenshotImage.Name = saveScreenshotImage.Name.Replace(Path.GetFileNameWithoutExtension(saveScreenshotImage.Name), baseNewName);

				DivinityApp.Log($"Renamed internal screenshot '{saveScreenshotImage.Name}' in '{output}'.");
			}

			var conversionParams = ResourceConversionParameters.FromGameVersion(DivinityApp.GAME);

			var build = new PackageBuildData
			{
				Version = conversionParams.PAKVersion,
				Compression = CompressionMethod.Zlib,
				CompressionLevel = LSCompressionLevel.Default
			};

			using var writer = new PackageWriter(build, output);
			writer.Write();

			File.SetLastWriteTime(output, File.GetLastWriteTime(pathToSave));
			File.SetLastAccessTime(output, File.GetLastAccessTime(pathToSave));

			return true;

		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Failed to rename save: {ex}");
		}

		return false;
	}
}
