using ModManager.Models.Interfaces;

namespace ModManager.Helpers.Sorting;

/// <inheritdoc />
public class NaturalFileSortComparer : IComparer<IFileModel>
{
	private readonly NaturalStringSortComparer _base;

	public NaturalFileSortComparer(StringComparison stringComparison) => _base = new(stringComparison);
	public NaturalFileSortComparer(IComparer<string> stringComparer) => _base = new(stringComparer);

	/// <inheritdoc />
	public int Compare(IFileModel? file1, IFileModel? file2)
	{
		return _base.Compare(file1?.FilePath, file2?.FilePath);
	}
}