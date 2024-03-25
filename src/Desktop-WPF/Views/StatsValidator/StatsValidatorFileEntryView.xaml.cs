using ModManager.Models.View;

using ReactiveUI;

using System.Windows;
using System.Windows.Media;

namespace ModManager.Views.StatsValidator;

public class StatsValidatorFileEntryViewBase : ReactiveUserControl<StatsValidatorFileResults> { }

public partial class StatsValidatorFileEntryView : StatsValidatorFileEntryViewBase
{
	public static Brush ErrorToForeground(bool isError)
	{
		if (!isError)
		{
			return Application.Current.TryFindResource(AdonisUI.Brushes.ForegroundBrush) as Brush;
		}
		return Brushes.OrangeRed;
	}

	public StatsValidatorFileEntryView()
	{
		InitializeComponent();

		this.OneWayBind(ViewModel, vm => vm.DisplayName, view => view.NameTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.ToolTip, view => view.NameTextBlock.ToolTip);
		this.OneWayBind(ViewModel, vm => vm.HasErrors, view => view.NameTextBlock.Foreground, ErrorToForeground);
	}
}
