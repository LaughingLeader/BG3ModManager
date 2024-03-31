namespace ModManager.ViewModels;
public static class ViewModelExtensions
{
	public static RxCommandUnit CreateCloseCommand(this IClosableViewModel viewModel, IObservable<bool>? canExecute = null, Action? invokeAction = null)
	{
		canExecute ??= viewModel.WhenAnyValue(x => x.IsVisible);
		return ReactiveCommand.Create(() => { viewModel.IsVisible = false; invokeAction?.Invoke(); }, canExecute);
	}
}
