using System.Diagnostics;

namespace ModManager.Util;

public class LogTraceListener : TextWriterTraceListener
{
	public LogTraceListener(string fileName, string name) : base(fileName, name)
	{

	}

	public override void Write(string message)
	{
		base.Write(StringUtils.ReplaceSpecialPathways(message));
	}

	public override void WriteLine(string message)
	{
		base.WriteLine(StringUtils.ReplaceSpecialPathways(message));
	}
}
