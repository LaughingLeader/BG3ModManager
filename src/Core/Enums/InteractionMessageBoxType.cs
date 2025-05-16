namespace ModManager;

[Flags]
public enum InteractionMessageBoxType
{
	None = 0,
	Warning = 1,
	Error = 2,
	Information = 3,
	YesNo = 8,
	Input = 16
}
