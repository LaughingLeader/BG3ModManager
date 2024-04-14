using System;
using System.Collections.Generic;
using System.Text;

namespace ModManager.Utils;
internal class CodeBuilder(char spaceChar = '\t')
{
	private readonly StringBuilder _sb = new();
	private readonly char _space = spaceChar;
	private int _currentIndent = 0;

	public void StartScope(string scopeToken = "{")
	{
		if(scopeToken != string.Empty) AppendLine(scopeToken);
		_currentIndent++;
	}

	public void EndScope(string scopeToken = "}")
	{
		_currentIndent--;
		if (scopeToken != string.Empty) AppendLine(scopeToken);
	}

	public void AppendLine(string text)
	{
		_sb.Append(_space, _currentIndent);
		_sb.AppendLine(text);
	}

	public override string ToString()
	{
		return _sb.ToString();
	}
}
