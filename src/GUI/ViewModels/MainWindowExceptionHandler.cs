using ReactiveUI;

using System.Diagnostics;
using System.Reactive.Concurrency;

namespace ModManager.ViewModels;

public class MainWindowExceptionHandler : IObserver<Exception>
{
	private readonly MainWindowViewModel _viewModel;

	public MainWindowExceptionHandler(MainWindowViewModel vm)
	{
		_viewModel = vm;
	}

	public void OnNext(Exception value)
	{
		DivinityApp.Log($"Error: [{value.Source}]({value.GetType()}): {value.Message}\n{value.StackTrace}");
		//if (Debugger.IsAttached) Debugger.Break();
		//RxApp.MainThreadScheduler.Schedule(() => { throw value; });
	}

	public void OnError(Exception error)
	{
		DivinityApp.Log($"Error: [{error.Source}]({error.GetType()}): {error.Message}\n{error.StackTrace}");
		if (Debugger.IsAttached) Debugger.Break();

		RxApp.MainThreadScheduler.Schedule(() =>
		{
			if (_viewModel.MainProgressIsActive)
			{
				_viewModel.MainProgressIsActive = false;
			}
			_viewModel.View.AlertBar.SetDangerAlert(error.Message);
			//throw error;
		});
	}

	public void OnCompleted()
	{
		if (Debugger.IsAttached) Debugger.Break();
		//RxApp.MainThreadScheduler.Schedule(() => { throw new NotImplementedException(); });
	}
}
