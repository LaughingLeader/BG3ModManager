namespace ModManager.Util;

public static class StreamUtils
{
	public static byte[] ReadToEnd(System.IO.Stream stream)
	{
		long originalPosition = 0;

		if (stream.CanSeek)
		{
			originalPosition = stream.Position;
			stream.Position = 0;
		}

		try
		{
			var readBuffer = new byte[4096];

			var totalBytesRead = 0;
			int bytesRead;

			while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
			{
				totalBytesRead += bytesRead;

				if (totalBytesRead == readBuffer.Length)
				{
					var nextByte = stream.ReadByte();
					if (nextByte != -1)
					{
						var temp = new byte[readBuffer.Length * 2];
						Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
						Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
						readBuffer = temp;
						totalBytesRead++;
					}
				}
			}

			var buffer = readBuffer;
			if (readBuffer.Length != totalBytesRead)
			{
				buffer = new byte[totalBytesRead];
				Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
			}
			return buffer;
		}
		finally
		{
			if (stream.CanSeek)
			{
				stream.Position = originalPosition;
			}
		}
	}

	public static int IndexOf(this byte[] arrayToSearchThrough, byte[] patternToFind)
	{
		if (patternToFind.Length > arrayToSearchThrough.Length)
			return -1;
		for (var i = 0; i < arrayToSearchThrough.Length - patternToFind.Length; i++)
		{
			var found = true;
			for (var j = 0; j < patternToFind.Length; j++)
			{
				if (arrayToSearchThrough[i + j] != patternToFind[j])
				{
					found = false;
					break;
				}
			}
			if (found)
			{
				return i;
			}
		}
		return -1;
	}
}
