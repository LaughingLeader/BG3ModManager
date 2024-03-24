using PowerArgs;

using Toolbox.ScriptExtender;

namespace Toolbox.Args;

[ArgExceptionBehavior(ArgExceptionPolicy.StandardExceptionHandling)]
public class MainArgs
{
	[ArgActionMethod, ArgDescription("Updates the script extender using the updater dll")]
	public static void UpdateScriptExtender(ScriptExtenderUpdaterArgs args)
	{
		if (!File.Exists(args.Updater) || !Directory.Exists(args.BinFolder))
		{
			throw new FileNotFoundException($"-u ({args.Updater}) and -b ({args.BinFolder}) args must be valid file paths.");
		}
		using (var updater = new Updater(args.Updater, args.BinFolder))
		{
			//updater.ShowConsoleWindow();
			//updater.SetGameVersion(Path.Join(args.BinFolder, "bg3.exe");
			Console.WriteLine($"Updating...");
			updater.Update();
		}
		Console.WriteLine("Update finished");
	}
}