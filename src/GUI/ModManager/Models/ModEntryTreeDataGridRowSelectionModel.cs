using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Selection;

using ModManager.Models.Mod;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModManager.Models;
public class ModEntryTreeDataGridRowSelectionModel(ITreeDataGridSource<IModEntry> source) 
	: TreeDataGridRowSelectionModel<IModEntry>(source), 
	ITreeDataGridRowSelectionModel<IModEntry>, 
	ITreeDataGridSelectionInteraction
{
	bool ITreeDataGridSelectionInteraction.IsRowSelected(IRow rowModel)
	{
		if (rowModel.Model is IModEntry mod && mod.PreserveSelection) return true;
		if (rowModel is IModelIndexableRow indexable)
			return IsSelected(indexable.ModelIndexPath);
		return false;
	}

	bool ITreeDataGridSelectionInteraction.IsRowSelected(int rowIndex)
	{
		if (rowIndex >= 0 && rowIndex < source.Rows.Count)
		{
			if (source.Rows[rowIndex] is IModelIndexableRow indexable)
			{
				if (indexable.Model is IModEntry mod && mod.PreserveSelection) return true;
				return IsSelected(indexable.ModelIndexPath);
			}
		}

		return false;
	}
}
