using ModManager.ViewModels;

using ReactiveUI;

using System.Reactive;
using System.Reactive.Linq;

namespace ModManager.Windows;

public class StatsValidatorWindowBase : HideWindowBase<StatsValidatorWindowViewModel> { }

public partial class StatsValidatorWindow : StatsValidatorWindowBase
{
	public StatsValidatorWindow()
	{
		InitializeComponent();

		ViewModel = ViewModelLocator.StatsValidator;

		DivinityInteractions.OpenValidateStatsResults.RegisterHandler(context =>
		{
			context.SetOutput(true);

			RxApp.MainThreadScheduler.Schedule(() =>
			{
				ViewModel.Load(context.Input);
				App.WM.StatsValidator.Toggle(true);
			});
		});

		this.OneWayBind(ViewModel, vm => vm.ModName, view => view.TitleTextBlock.Text, name => $"{name} Results");
		this.OneWayBind(ViewModel, vm => vm.Entries, view => view.EntriesTreeView.ItemsSource);

		this.OneWayBind(ViewModel, vm => vm.LockScreenVisibility, view => view.LockScreen.Visibility);

		this.OneWayBind(ViewModel, vm => vm.OutputText, view => view.ResultsTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.TimeTakenText, view => view.TimeTakenTextControl.Text);

		this.BindCommand(ViewModel, vm => vm.ValidateCommand, view => view.ValidateButton, vm => vm.Mod);
		this.BindCommand(ViewModel, vm => vm.CancelValidateCommand, view => view.CancelButton);
	}
}
