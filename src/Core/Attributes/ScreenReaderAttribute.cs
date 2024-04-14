namespace ModManager;

public class ScreenReaderHelperAttribute : Attribute
{
	public string? Name { get; set; }
	public string? HelpText { get; set; }

	public ScreenReaderHelperAttribute(string name = "", string helpText = "")
	{
		Name = name;
		HelpText = HelpText;
	}
}
