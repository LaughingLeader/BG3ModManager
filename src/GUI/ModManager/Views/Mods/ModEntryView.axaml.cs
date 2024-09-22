using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.VisualTree;

using ModManager.Models.Mod;

using System.Windows.Input;

namespace ModManager.Views.Mods;

public partial class ModEntryView : ReactiveUserControl<DivinityModData>
{
	public ModEntryView()
	{
		InitializeComponent();
	}
}