

namespace ModManager;

public class ViewLocatorErrorView : TextBlock, IViewFor
{
	public object ViewModel { get; set; }
}


public class ViewLocator : IViewLocator
{
	private static readonly Type _viewForType = typeof(IViewFor<>);

	public IViewFor ResolveView<T>(T viewModel, string contract = null)
	{
		try
		{
			var viewType = _viewForType.MakeGenericType(viewModel.GetType());
			var registered = Locator.Current.GetService(viewType, contract);
			if (registered is IViewFor view)
			{
				return view;
			}
		}
		catch (Exception ex)
		{
			DivinityApp.Log($"Error fetching view: {ex}");
		}
		return new ViewLocatorErrorView() { Text = $"Failed to find view for {viewModel}" };
	}

	static ViewLocator()
	{
		var resolver = Locator.CurrentMutable;

		/*resolver.RegisterSingletonView<DeleteFilesViewModel, DeleteFilesConfirmationView>();
		resolver.RegisterSingletonView<ModOrderViewModel, ModOrderView>(resolver);
		resolver.RegisterSingletonView<ModUpdatesViewModel, ModUpdatesLayout>(resolver);

		resolver.RegisterView<DownloadActivityBarViewModel, DownloadActivityBar>(resolver);
		resolver.RegisterView<StatsValidatorFileResults, StatsValidatorFileEntryView>(resolver);
		resolver.RegisterView<StatsValidatorErrorEntry, StatsValidatorEntryView>(resolver);
		resolver.RegisterView<StatsValidatorLineText, StatsValidatorLineView>(resolver);*/
	}
}