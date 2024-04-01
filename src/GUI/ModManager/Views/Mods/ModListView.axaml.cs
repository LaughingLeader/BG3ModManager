using Avalonia.Input;

using ModManager.ViewModels.Mods;

namespace ModManager.Views.Mods;
public partial class ModListView : ReactiveUserControl<ModListViewModel>
{
	public ModListView()
	{
		InitializeComponent();

		this.WhenActivated(d =>
		{
			d(Observable.FromEventPattern<KeyEventArgs>(FilterTextBox, nameof(FilterTextBox.KeyDown)).Subscribe(e =>
			{
				var key = e.EventArgs.Key;
				if (key == Key.Return || key == Key.Enter || key == Key.Escape)
				{
					ModsTreeDataGrid.Focus(NavigationMethod.Pointer);
				}
			}));
		});
	}
}
