using ModManager.ViewModels.Mods;

namespace ModManager.Views.Mods;

public partial class ModPickerView : ReactiveUserControl<ModPickerViewModel>
{
    public ModPickerView()
    {
        InitializeComponent();

#if DEBUG
		this.DesignSetup();
#endif

		this.WhenActivated(d =>
		{
			if (!Design.IsDesignMode) ViewModel ??= ViewModelLocator.ModPicker;

			if (ViewModel != null)
			{
				d(this.GetObservable(IsVisibleProperty).BindTo(ViewModel, x => x.IsVisible));

				//Throttle filtering here so we can be sure we're delaying when the user may be typing
				d(FilterTextBox.GetObservable(TextBox.TextProperty)
				.Skip(1)
				.Throttle(TimeSpan.FromMilliseconds(500))
				.ObserveOn(RxApp.MainThreadScheduler)
				.BindTo(ViewModel, x => x.FilterInputText));

				d(ViewModel.WhenAnyValue(x => x.FilterInputText).BindTo(this, x => x.FilterTextBox.Text));

				d(Observable.FromEventPattern<KeyEventArgs>(FilterTextBox, nameof(FilterTextBox.KeyDown)).Subscribe(e =>
				{
					var key = e.EventArgs.Key;
					if (key == Key.Return || key == Key.Enter || key == Key.Escape)
					{
						ModsListBox.Focus(NavigationMethod.Tab);
					}
				}));
			}
		});
	}
}