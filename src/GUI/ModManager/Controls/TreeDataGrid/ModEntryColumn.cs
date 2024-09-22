using Avalonia.Controls.Models.TreeDataGrid;

using ModManager.Models.Mod;

using System.ComponentModel;

namespace ModManager.Controls.TreeDataGrid;

/// <summary>
/// A column in an <see cref="ITreeDataGridSource"/> which displays its values using a data
/// template.
/// </summary>
/// <typeparam name="TModel">The model type.</typeparam>
/// <typeparam name="TValue">The column data type.</typeparam>
public class ModEntryColumn : ColumnBase<IModEntry>, ITextSearchableColumn<IModEntry>
{
	public ModEntryColumn(object? header = null, GridLength? width = null, ColumnOptions<IModEntry>? options = null) : base(header, width, options ?? new())
	{

	}

	public override ICell CreateCell(IRow<IModEntry> row) => new ModEntryCell(row.Model);

	public override Comparison<IModEntry?>? GetComparison(ListSortDirection direction)
	{
		return direction switch
		{
			ListSortDirection.Ascending => Options.CompareAscending,
			ListSortDirection.Descending => Options.CompareDescending,
			_ => null,
		};
	}

	public bool IsTextSearchEnabled => true;
	public string? SelectValue(IModEntry model) => model.DisplayName;
}
