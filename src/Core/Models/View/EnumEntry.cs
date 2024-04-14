namespace ModManager.Models.View;

public class EnumEntry : ReactiveObject
{
	[Reactive] public string? Description { get; set; }
	[Reactive] public object Value { get; set; }

	public EnumEntry(string description, object value)
	{
		Description = description;
		Value = value;
	}
}
