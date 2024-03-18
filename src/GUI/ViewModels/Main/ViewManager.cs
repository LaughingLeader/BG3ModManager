using DivinityModManager.Views;
using DivinityModManager.Views.Main;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Splat;

namespace DivinityModManager.ViewModels.Main
{
	public class ViewManager : ReactiveObject
	{
		private readonly RoutingState Router;

		public ModOrderViewModel ModOrder { get; }
		public DeleteFilesViewModel DeleteFiles { get; }
		public ModUpdatesViewModel ModUpdates { get; }

		[ObservableAsProperty] public IRoutableViewModel CurrentView { get; }

		public void SwitchToModOrderView() => Router.Navigate.Execute(ModOrder);
		public void SwitchToDeleteView() => Router.Navigate.Execute(DeleteFiles);
		public void SwitchToModUpdates() => Router.Navigate.Execute(ModUpdates);


		public ViewManager(RoutingState router, MainWindowViewModel vm)
		{
			Router = router;

			ModOrder = new ModOrderViewModel(vm);
			DeleteFiles = new DeleteFilesViewModel(vm);
			ModUpdates = new ModUpdatesViewModel(vm);

			Locator.CurrentMutable.RegisterConstant(new ModOrderView() { ViewModel = ModOrder }, typeof(IViewFor<ModOrderViewModel>));
			Locator.CurrentMutable.RegisterLazySingleton(() => new DeleteFilesConfirmationView() { ViewModel = DeleteFiles }, typeof(IViewFor<DeleteFilesViewModel>));
			Locator.CurrentMutable.RegisterLazySingleton(() => new ModUpdatesLayout() { ViewModel = ModUpdates }, typeof(IViewFor<ModUpdatesViewModel>));

			Router.CurrentViewModel.ToPropertyEx(this, x => x.CurrentView, false, RxApp.MainThreadScheduler);
		}
	}
}
