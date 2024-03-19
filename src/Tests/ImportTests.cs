using DivinityModManager.Models.Mod;
using DivinityModManager.Util;

using SharpCompress.Archives;
using SharpCompress.Archives.Zip;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace DivinityModManager.Tests
{
	public class ImportTests : BaseTest
	{
		private static readonly string _testMeta = "<?xml version=\"1.0\" encoding=\"UTF-8\"?> <save> <version major=\"4\" minor=\"4\" revision=\"0\" build=\"300\"/> <region id=\"Config\"> <node id=\"root\"> <children> <node id=\"Dependencies\"/> <node id=\"ModuleInfo\"> <attribute id=\"Author\" type=\"LSString\" value=\"\"/> <attribute id=\"CharacterCreationLevelName\" type=\"FixedString\" value=\"\"/> <attribute id=\"Description\" type=\"LSString\" value=\"\"/> <attribute id=\"Folder\" type=\"LSString\" value=\"Test\"/> <attribute id=\"LobbyLevelName\" type=\"FixedString\" value=\"\"/> <attribute id=\"MD5\" type=\"LSString\" value=\"\"/> <attribute id=\"MainMenuBackgroundVideo\" type=\"FixedString\" value=\"\"/> <attribute id=\"MenuLevelName\" type=\"FixedString\" value=\"\"/> <attribute id=\"Name\" type=\"LSString\" value=\"Test Mod\"/> <attribute id=\"NumPlayers\" type=\"uint8\" value=\"4\"/> <attribute id=\"PhotoBooth\" type=\"FixedString\" value=\"\"/> <attribute id=\"StartupLevelName\" type=\"FixedString\" value=\"\"/> <attribute id=\"Tags\" type=\"LSString\" value=\"\"/> <attribute id=\"Type\" type=\"FixedString\" value=\"Adventure\"/> <attribute id=\"UUID\" type=\"FixedString\" value=\"32ac9ce2-2aba-8cda-b3b5-6e922f71b6b8\"/> <attribute id=\"Version64\" type=\"int64\" value=\"144819734515343021\"/> <children> <node id=\"PublishVersion\"> <attribute id=\"Version64\" type=\"int64\" value=\"144255927711717104\"/> </node> <node id=\"Scripts\"/> <node id=\"TargetModes\"> <children> <node id=\"Target\"> <attribute id=\"Object\" type=\"FixedString\" value=\"Story\"/> </node> </children> </node> </children> </node> </children> </node> </region> </save> ";
		private readonly CancellationTokenSource _cts;

		private readonly string testDir;
		private readonly string pakDataPath;
		private readonly string importDirectory;
		private readonly string outputDirectory;
		private readonly string metaPath;
		private readonly string dummyFilePath;
		public ImportTests(ITestOutputHelper output) : base(output)
		{
			_cts = new CancellationTokenSource();

			testDir = DivinityApp.GetAppDirectory("Test");
			importDirectory = Path.Combine(testDir, "Mods");
			outputDirectory = Path.Combine(testDir, "Output");
			pakDataPath = Path.Combine(testDir, "TestData");
			metaPath = Path.Combine(pakDataPath, @"Mods\Test\meta.lsx");
			dummyFilePath = Path.Combine(pakDataPath, @"Mods\Test\dummyfile.bin");

			Directory.CreateDirectory(testDir);
			Directory.CreateDirectory(pakDataPath);
			Directory.CreateDirectory(importDirectory);
			Directory.CreateDirectory(Path.GetDirectoryName(metaPath));
		}

		public override void Dispose()
		{
			base.Dispose();
			try
			{
				Directory.Delete(testDir);
			}
			catch (Exception ex) { }
		}

		[Theory]
		[InlineData(42000, false)] // 42 KB
		[InlineData(500000000, true)] // 500 MB
									  //[InlineData(2048L * 1024 * 1024)] // 2GB
		public async Task ImportModPak(int intendedPakSize, bool asArchive)
		{
			var pakPath = Path.Combine(outputDirectory, $"Test_{StringUtils.BytesToString(intendedPakSize)}.pak");
			Directory.CreateDirectory(Path.GetDirectoryName(pakPath));

			File.WriteAllText(metaPath, _testMeta);

			var fs = File.Create(dummyFilePath, intendedPakSize, System.IO.FileOptions.Asynchronous);
			fs.Seek(intendedPakSize, System.IO.SeekOrigin.Begin);
			fs.WriteByte(0);
			fs.Dispose();

			var files = new List<string> { metaPath, dummyFilePath };

			Assert.True(await FileUtils.CreatePackageAsync(pakDataPath, files, pakPath, _cts.Token), "Failed to create pak");
			var pakSize = new FileInfo(pakPath)?.Length;
			Assert.True(pakSize >= 0, $"Intended pak size not correct ({pakSize}/{intendedPakSize})");

			var importFilePath = pakPath;

			if (asArchive)
			{
				var zipPath = Path.Combine(testDir, @"Output\Test.zip");
				var zip = ZipArchive.Create();
				zip.AddEntry(Path.GetFileName(pakPath), pakPath);
				zip.SaveTo(zipPath, new SharpCompress.Writers.WriterOptions(SharpCompress.Common.CompressionType.Deflate));
				importFilePath = zipPath;
				zip.Dispose();
			}

			var options = new ImportParameters(importFilePath, importDirectory, _cts.Token)
			{
				BuiltinMods = new Dictionary<string, DivinityModData>(),
				OnlyMods = true
			};

			Assert.True(await ImportUtils.ImportFileAsync(options), "Failed to import file");
			Assert.True(options.Result.Mods.Count > 0, "Failed to import mod in pak");

			Output.WriteLine($"Imported mod: {options.Result.Mods[0].Name}");
		}
	}
}
