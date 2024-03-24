using ModManager.Models.View;
using ModManager.Util;
using ModManager.ViewModels;
using ModManager.Views.StatsValidator;

using ReactiveUI;

using Splat;

using System.Reactive;
using System.Reactive.Linq;

namespace ModManager.Windows;

public class StatsValidatorWindowBase : HideWindowBase<StatsValidatorWindowViewModel> { }

public partial class StatsValidatorWindow : StatsValidatorWindowBase
{
	private async Task<Unit> OpenWindow(IInteractionContext<ValidateModStatsResults, bool> context)
	{
		await Observable.Start(() =>
		{
			ViewModel.Load(context.Input);
			App.WM.StatsValidator.Toggle(true);
		}, RxApp.MainThreadScheduler);
		context.SetOutput(true);
		return Unit.Default;
	}

	public StatsValidatorWindow()
	{
		InitializeComponent();

		ViewModel = new StatsValidatorWindowViewModel();

		DivinityInteractions.OpenValidateStatsResults.RegisterHandler(input => Observable.Start(() => OpenWindow(input), RxApp.MainThreadScheduler));

		this.OneWayBind(ViewModel, vm => vm.ModName, view => view.TitleTextBlock.Text, name => $"{name} Results");
		this.OneWayBind(ViewModel, vm => vm.OutputText, view => view.ResultsTextBlock.Text);
		this.OneWayBind(ViewModel, vm => vm.Entries, view => view.EntriesTreeView.ItemsSource);

		this.OneWayBind(ViewModel, vm => vm.LockScreenVisibility, view => view.LockScreen.Visibility);

		this.BindCommand(ViewModel, vm => vm.ValidateCommand, view => view.ValidateButton, vm => vm.Mod);
		this.BindCommand(ViewModel, vm => vm.CancelValidateCommand, view => view.CancelButton);
	}
}
