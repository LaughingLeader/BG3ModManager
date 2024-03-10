using DivinityModManager.Views;
using DivinityModManager.Views.Main;

using ReactiveUI;

using Splat;

namespace DivinityModManager.ViewModels.Main
{
	public class ViewManager
    {
		private readonly RoutingState Router;

		public ModOrderViewModel ModOrder { get; }
		public DeleteFilesViewData DeleteFiles { get; }
		public ModUpdatesViewData ModUpdates { get; }

		public void SwitchToModOrderView() => Router.Navigate.Execute(ModOrder);
		public void SwitchToDeleteView() => Router.Navigate.Execute(DeleteFiles);
		public void SwitchToModUpdates() => Router.Navigate.Execute(ModUpdates);

		public ViewManager(RoutingState router, MainWindowViewModel vm)
		{
			Router = router;

			ModOrder = new ModOrderViewModel(vm);
			DeleteFiles = new DeleteFilesViewData(vm);
			ModUpdates = new ModUpdatesViewData(vm);

			Locator.CurrentMutable.RegisterConstant(new ModOrderView(), typeof(IViewFor<ModOrderViewModel>));
			Locator.CurrentMutable.RegisterLazySingleton(() => new DeleteFilesConfirmationView(), typeof(IViewFor<DeleteFilesViewData>));
			Locator.CurrentMutable.RegisterLazySingleton(() => new ModUpdatesLayout(), typeof(IViewFor<DeleteFilesViewData>));
		}
	}
}
