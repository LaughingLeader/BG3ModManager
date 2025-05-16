using Avalonia.Media;

using System.Diagnostics;

namespace ModManager.Controls;
public class GeneratedUserControl<TViewModel> : ReactiveUserControl<TViewModel> where TViewModel : class
{
	public GeneratedUserControl()
	{
#if DEBUG
		this.DesignSetup();
#endif
	}

	/*protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
	{
		if(change.Property == DataContextProperty)
		{
			Trace.WriteLine($"DataContext: [{DataContext}]=>[{change.NewValue}]");
			Trace.WriteLine($"ViewModel: [{ViewModel}]");
			return;
		}
		else if(change.Property == ViewModelProperty)
		{
			SetCurrentValue(DataContextProperty, change.NewValue);
			return;
		}
		base.OnPropertyChanged(change);
	}*/
}