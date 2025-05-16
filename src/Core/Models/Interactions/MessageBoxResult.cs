namespace ModManager;

public readonly struct MessageBoxResult(bool result, string? input)
{
	public readonly bool Result = result;
	public readonly string? Input = input;

	public static bool operator true(MessageBoxResult x) => x.Result == true;
	public static bool operator false(MessageBoxResult x) => x.Result == false;
}