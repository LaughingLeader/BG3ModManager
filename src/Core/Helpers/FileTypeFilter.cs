namespace ModManager;
public record struct FileTypeFilter(string Name, string[] Extensions)
{
	/// <summary>
	/// Combines <c>Name</c> with <c>Extensions</c>, joined together with semi-colons.
	/// <br/>
	/// <example>
	/// For example:
	/// <code>
	/// var name = new FileTypeFilter("Json file", ["*.json", "*.jsonc"]).GetDisplayName();
	/// </code>
	/// results in <c>"JSON file (*.json;*.jsonc)"</c>
	/// </example>
	/// </summary>
	/// <returns>A formatted display name.</returns>
	public readonly string GetDisplayName() => $"{Name} ({string.Join(";", Extensions)})";
}