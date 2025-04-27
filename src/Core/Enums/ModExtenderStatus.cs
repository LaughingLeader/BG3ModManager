namespace ModManager;

[Flags]
public enum ModExtenderStatus
{
	None,
	Supports,
	Fulfilled,
	DisabledFromConfig,
	MissingRequiredVersion,
	MissingAppData,
	MissingUpdater,
}