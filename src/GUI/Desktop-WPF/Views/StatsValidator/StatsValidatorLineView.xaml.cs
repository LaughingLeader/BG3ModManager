using ModManager.Models.View;

namespace ModManager.Views.StatsValidator;

public class StatsValidatorLineViewBase : ReactiveUserControl<StatsValidatorLineText> { }

public partial class StatsValidatorLineView : StatsValidatorLineViewBase
{
	public StatsValidatorLineView()
	{
		InitializeComponent();

		this.OneWayBind(ViewModel, vm => vm.Text, view => view.LineTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.Start, view => view.LineTextBlock.HighlightStart);
		this.OneWayBind(ViewModel, vm => vm.End, view => view.LineTextBlock.HighlightEnd);
		this.OneWayBind(ViewModel, vm => vm.IsError, view => view.LineTextBlock.HighlightForeground, StatsValidatorEntryView.ErrorToForeground);
	}
}
