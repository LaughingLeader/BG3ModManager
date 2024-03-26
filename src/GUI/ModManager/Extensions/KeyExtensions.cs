using Avalonia.Input;

namespace ModManager;

public static class KeyExtensions
{
	private static readonly Dictionary<Key, string> KeyToName = new()
	{
		{Key.Add, "+"},
		{Key.D0, "0"},
		{Key.D1, "1"},
		{Key.D2, "2"},
		{Key.D3, "3"},
		{Key.D4, "4"},
		{Key.D5, "5"},
		{Key.D6, "6"},
		{Key.D7, "7"},
		{Key.D8, "8"},
		{Key.D9, "9"},
		{Key.Decimal, "."},
		{Key.Divide, " / "},
		{Key.Multiply, "*"},
		{Key.Oem1, ";"},
		{Key.Oem5, "\\"},
		{Key.Oem6, "]"},
		{Key.Oem7, "'"},
		{Key.OemBackslash, "\\"},
		{Key.OemComma, ","},
		{Key.OemMinus, "-"},
		{Key.OemOpenBrackets, "["},
		{Key.OemPeriod, "."},
		{Key.OemPlus, "="},
		{Key.OemQuestion, "/"},
		{Key.OemTilde, "`"},
		{Key.Subtract, "-"}
	};

	public static string GetKeyName(this Key key)
	{
		if (KeyToName.TryGetValue(key, out var name))
		{
			return name;
		}
		return key.ToString();
	}
}
