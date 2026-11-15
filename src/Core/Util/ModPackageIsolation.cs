using System.Diagnostics;

namespace DivinityModManager.Util;

// Apply a complete package move plan before the existing settings writer; undo failures.
public static class ModPackageIsolation
{
	private static readonly SemaphoreSlim Gate = new(1, 1);
	public static string GetInactiveDirectory(string modsDirectory) => Path.GetFullPath(modsDirectory).TrimEnd(Path.DirectorySeparatorChar) + ".disabled";
	public static IEnumerable<string> GetPackageDirectories(string modsDirectory) => new[] { modsDirectory, GetInactiveDirectory(modsDirectory) }.Where(Directory.Exists);

	public static async Task<bool> ExportAsync(IReadOnlyDictionary<string, string> moves,
		Func<Task<bool>> exportSettings, Func<bool> isGameRunning = null)
	{
		static bool NeedsMove(KeyValuePair<string, string> pair) => !pair.Key.Equals(pair.Value, StringComparison.OrdinalIgnoreCase);
		await Gate.WaitAsync();
		var completed = new List<KeyValuePair<string, string>>();
		var exported = false;
		try
		{
			foreach (var move in moves)
				if (!File.Exists(move.Key) || (NeedsMove(move) && (File.Exists(move.Value) || Directory.Exists(move.Value))))
					throw new IOException($"Package is missing or its destination already exists: '{move.Key}' -> '{move.Value}'.");
			var processes = new[] { "bg3", "bg3_dx11" }.SelectMany(Process.GetProcessesByName).ToArray();
			try
			{
				if (moves.Any(NeedsMove) && (isGameRunning?.Invoke() ?? processes.Length > 0))
					throw new IOException("Close Baldur's Gate 3 before exporting package changes.");
			}
			finally { foreach (var process in processes) process.Dispose(); }
			foreach (var move in moves)
			{
				if (!NeedsMove(move)) continue;
				Directory.CreateDirectory(Path.GetDirectoryName(move.Value));
				File.Move(move.Key, move.Value);
				completed.Add(move);
			}
			exported = await exportSettings();
			return exported;
		}
		finally
		{
			var errors = new List<Exception>();
			if (!exported)
				foreach (var move in completed.AsEnumerable().Reverse())
					try { File.Move(move.Value, move.Key); }
					catch (Exception ex) { errors.Add(ex); }
			Gate.Release();
			if (errors.Count > 0) throw new AggregateException("Package rollback was incomplete; restore the reported files manually.", errors);
		}
	}
}
