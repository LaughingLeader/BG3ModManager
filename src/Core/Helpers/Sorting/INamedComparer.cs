using ModManager.Models.Interfaces;

namespace ModManager.Helpers.Sorting;
public class INamedComparer : IComparer<INamedEntry>
{
	private readonly NaturalStringSortComparer _base;

	public INamedComparer(StringComparison stringComparison) => _base = new(stringComparison);
	public INamedComparer(IComparer<string> stringComparer) => _base = new(stringComparer);

	/// <inheritdoc />
	public int Compare(INamedEntry? x, INamedEntry? y)
	{
		return _base.Compare(x?.Name, y?.Name);
	}
}
