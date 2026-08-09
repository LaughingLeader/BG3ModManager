using System.Text;

#nullable enable

namespace DivinityModManager.Util;

public static class ScriptExtenderUpdaterDetector
{
	private const int MaximumSignatureScanBytes = 64 * 1024 * 1024;
	private static readonly byte[] UpdaterSignature = Encoding.ASCII.GetBytes("BG3 Script Extender Error");

	public static bool IsScriptExtenderUpdater(string? productName, Stream updaterBinary)
	{
		if (!String.IsNullOrEmpty(productName))
		{
			return productName.IndexOf("Script Extender", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		ArgumentNullException.ThrowIfNull(updaterBinary);

		int matchedBytes = 0;
		int scannedBytes = 0;
		var buffer = new byte[8192];
		while (scannedBytes < MaximumSignatureScanBytes)
		{
			int bytesToRead = Math.Min(buffer.Length, MaximumSignatureScanBytes - scannedBytes);
			int bytesRead = updaterBinary.Read(buffer, 0, bytesToRead);
			if (bytesRead == 0)
			{
				return false;
			}

			scannedBytes += bytesRead;
			for (int index = 0; index < bytesRead; index++)
			{
				byte currentByte = buffer[index];
				if (currentByte == UpdaterSignature[matchedBytes])
				{
					matchedBytes++;
					if (matchedBytes == UpdaterSignature.Length)
					{
						return true;
					}
				}
				else
				{
					matchedBytes = currentByte == UpdaterSignature[0] ? 1 : 0;
				}
			}
		}

		return false;
	}
}
