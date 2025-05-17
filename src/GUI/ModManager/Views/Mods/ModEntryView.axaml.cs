using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

using ModManager.Models.Mod;

namespace ModManager.Views.Mods;

public partial class ModEntryView : ReactiveUserControl<ModEntry>
{
	public ModEntryView()
	{
		InitializeComponent();
	}
}