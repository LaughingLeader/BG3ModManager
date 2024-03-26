using ModManager.Models.View;

using ReactiveUI;

using System.Windows.Media;

namespace ModManager.Views.StatsValidator;

public class StatsValidatorEntryViewBase : ReactiveUserControl<StatsValidatorErrorEntry> { }

public partial class StatsValidatorEntryView : StatsValidatorEntryViewBase
{
	public static Brush ErrorToForeground(bool isError) => isError ? Brushes.OrangeRed : Brushes.Yellow;

	public StatsValidatorEntryView()
	{
		InitializeComponent();

		this.OneWayBind(ViewModel, vm => vm.Message, view => view.MessageTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.IsError, view => view.MessageTextBlock.Foreground, ErrorToForeground);
	}
}
