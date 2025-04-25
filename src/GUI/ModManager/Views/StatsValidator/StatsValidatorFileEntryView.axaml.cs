using Avalonia.Media;

using ModManager.Models.View;

namespace ModManager.Views.StatsValidator;

public partial class StatsValidatorFileEntryView : ReactiveUserControl<StatsValidatorFileResults>
{
	public static IBrush ErrorToForeground(bool isError)
	{
		if (!isError)
		{
			if (Application.Current != null
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

#if DEBUG
		this.DesignSetup();
#endif

		this.OneWayBind(ViewModel, vm => vm.HasErrors, x => x.TextControl.Foreground, ErrorToForeground);
	}
}