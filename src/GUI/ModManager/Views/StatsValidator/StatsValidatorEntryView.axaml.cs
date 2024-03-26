using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;

using ModManager.Models.View;

namespace ModManager;

public partial class StatsValidatorEntryView : ReactiveUserControl<StatsValidatorErrorEntry>
{
	public static IBrush ErrorToForeground(bool isError) => isError ? Brushes.OrangeRed : Brushes.Yellow;

	public StatsValidatorEntryView()
    {
        InitializeComponent();

		this.OneWayBind(ViewModel, vm => vm.IsError, view => view.TextControl.Foreground, ErrorToForeground);
	}
}