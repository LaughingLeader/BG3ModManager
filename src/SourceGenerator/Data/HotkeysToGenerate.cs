using System;
using System.Collections.Generic;
using System.Text;

namespace ModManager.SourceGenerator.Data;
public readonly record struct HotkeysToGenerate : IEquatable<HotkeysToGenerate>
{
	public readonly INamedTypeSymbol Symbol;
	public readonly List<HotkeyData> Hotkeys;
	public readonly string ClassName;

	public HotkeysToGenerate(INamedTypeSymbol symbol, List<HotkeyData> hotkeys)
	{
		Symbol = symbol;
		Hotkeys = hotkeys;
		ClassName = $"{Symbol.Name}.Keybindings.g.cs";
	}
}