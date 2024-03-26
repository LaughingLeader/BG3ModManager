using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.ReactiveUI;

using FluentAvalonia.Styling;
using ModManager.Models.View;

namespace ModManager;

public partial class StatsValidatorFileEntryView : ReactiveUserControl<StatsValidatorFileResults>
{
	public static IBrush ErrorToForeground(bool isError)
	{
		if (!isError)
		{
			if(Application.Current != null 
				&& Application.Current.TryFindResource("TextFillColorSecondaryBrush", out var value) 
				&& value is IBrush brush)
			{
				return brush;
			}
			return Brushes.White;
		}
		return Brushes.OrangeRed;
	}

	public StatsValidatorFileEntryView()
    {
        InitializeComponent();

		this.OneWayBind(ViewModel, vm => vm.HasErrors, x => x.TextControl.Foreground, ErrorToForeground);
    }
}