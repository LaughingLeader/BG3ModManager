﻿namespace ModManager.ViewModels.Main;

public class ViewManager : ReactiveObject
{
	private readonly RoutingState Router;

	[ObservableAsProperty] public IRoutableViewModel CurrentView { get; }

	public void SwitchToModOrderView() => Router.Navigate.Execute(ViewModelLocator.ModOrder);
	public void SwitchToDeleteView() => Router.Navigate.Execute(ViewModelLocator.DeleteFiles);
	public void SwitchToModUpdates() => Router.Navigate.Execute(ViewModelLocator.ModUpdates);


	public ViewManager(RoutingState router, MainWindowViewModel vm)
	{
		Router = router;
		Router.CurrentViewModel.ToPropertyEx(this, x => x.CurrentView, false, RxApp.MainThreadScheduler);
	}
}