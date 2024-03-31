namespace ModManager;
public static class SplatExtensions
{
	public static void RegisterSingletonView<TViewModel, TView>(this IMutableDependencyResolver resolver) where TViewModel : ReactiveObject where TView : IViewFor<TViewModel>
	{
		resolver.RegisterLazySingleton<IViewFor<TViewModel>>(() => Locator.Current.GetService<TView>());
	}

	public static void RegisterView<TViewModel, TView>(this IMutableDependencyResolver resolver) where TViewModel : ReactiveObject where TView : IViewFor<TViewModel>, new()
	{
		resolver.Register(() => new TView());
	}
}