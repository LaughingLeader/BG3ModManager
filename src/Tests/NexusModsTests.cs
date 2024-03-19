using DivinityModManager.AppServices;
using DivinityModManager.Models.Mod;
using DivinityModManager.Models.Updates;
using DivinityModManager.Util;

using Newtonsoft.Json;

using ReactiveUI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace DivinityModManager.Tests
{
	public class NexusModsTests : BaseTest
	{
		private readonly NexusModsService _service;
		private readonly string _testModUUID = "789deb36-e3da-4a5e-b737-eb644cd1dd6a";
		private readonly List<DivinityModData> _mods;

		public NexusModsTests(ITestOutputHelper output) : base(output)
		{
			var apiKey = Environment.GetEnvironmentVariable("NEXUS_API_KEY");
			Assert.True(!String.IsNullOrEmpty(apiKey), "Set the NEXUS_API_KEY environment variable in order to run automated NexusMods tests");

			var assembly = Assembly.GetExecutingAssembly();
			var appName = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false)).Product;
			var version = assembly.GetName().Version.ToString();
			var productName = Regex.Replace(appName.Trim(), @"\s+", String.Empty);

			_service = new NexusModsService("Baldur's Gate 3 Mod Manager", "1.0.10.0-Testing")
			{
				ApiKey = apiKey
			};

			_mods = new List<DivinityModData>
			{
				CreateTestMod(_testModUUID, 3214)
			};
		}

		private static DivinityModData CreateTestMod(string uuid, long modId)
		{
			var mod = new DivinityModData() { UUID = uuid };
			mod.NexusModsData.ModId = modId;
			return mod;
		}

		[Theory]
		[InlineData(0)]
		[InlineData(250)]
		public async Task FetchModInfo(int cancelAfter)
		{
			var cts = new CancellationTokenSource();

			if (cancelAfter > 0)
			{
				RxApp.TaskpoolScheduler.Schedule(TimeSpan.FromMilliseconds(cancelAfter), () => cts.Cancel(false));
			}

			var results = await _service.FetchModInfoAsync(_mods, cts.Token);

			if (!cts.IsCancellationRequested)
			{
				Assert.True(results.Success, $"Failed to fetch info: {results.FailureMessage}");
				Assert.True(results.UpdatedMods.Count > 0, "No mods were updated?");
				Output.WriteLine($"NexusMods Info:\n{JsonConvert.SerializeObject(results.UpdatedMods.First().NexusModsData, Formatting.Indented)}");
			}
			else
			{
				Output.WriteLine("Task successfully canceled");
			}
		}

		[Fact]
		public async Task QueryAndDownloadModUpdate()
		{
			var cts = new CancellationTokenSource();

			var results = await _service.GetLatestDownloadsForModsAsync(_mods, cts.Token);

			Assert.True(results.TryGetValue(_testModUUID, out var downloadLink), "Failed to get download link");
			var url = downloadLink.DownloadLink.Uri.ToString();

			var downloadData = new ModDownloadData()
			{
				DownloadPath = url,
				DownloadPathType = ModDownloadPathType.URL,
				DownloadSourceType = ModSourceType.NEXUSMODS,
				Version = downloadLink.File.ModVersion,
				Date = DateUtils.UnixTimeStampToDateTime(downloadLink.File.UploadedTimestamp)
			};
			var outputDirectory = DivinityApp.GetAppDirectory("Temp");
			var downloadResult = await downloadData.DownloadAsync(null, outputDirectory, cts.Token);
			Assert.True(downloadResult.Success, "Failed to download file");
			var info = new FileInfo(downloadResult.OutputFilePath);
			Assert.True(info.Exists && info.Length >= 1390000, $"Output pak size ({info?.Length}) does not match expected size 1.39 MB");
			info.Delete();
		}
	}
}
