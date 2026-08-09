using System.Text;
using DivinityModManager.Util;
using Xunit;

namespace BG3ModManager.Tests;

public class ScriptExtenderUpdaterDetectorTests
{
	[Fact]
	public void ProductMetadataContainingScriptExtenderIsAccepted()
	{
		using var unrelatedPayload = new MemoryStream("unrelated dll payload"u8.ToArray());

		bool detected = ScriptExtenderUpdaterDetector.IsScriptExtenderUpdater(
			"BG3 Script Extender Updater",
			unrelatedPayload);

		Assert.True(detected);
	}

	[Fact]
	public void NullProductNameFallsBackToUpdaterByteSignature()
	{
		byte[] updaterPayload = Encoding.ASCII.GetBytes(
			"binary-prefix\0BG3 Script Extender Error\0binary-suffix");
		using var stream = new MemoryStream(updaterPayload);

		bool detected = ScriptExtenderUpdaterDetector.IsScriptExtenderUpdater(null, stream);

		Assert.True(detected);
	}

	[Fact]
	public void NullProductNameRejectsUnrelatedDWritePayload()
	{
		using var unrelatedPayload = new MemoryStream("ordinary proxy dll"u8.ToArray());

		bool detected = ScriptExtenderUpdaterDetector.IsScriptExtenderUpdater(
			null,
			unrelatedPayload);

		Assert.False(detected);
	}
}
