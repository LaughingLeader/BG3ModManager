using System.Windows;

namespace ModManager.Util;

class RxExceptionHandler : IObserver<Exception>
{
	public void OnNext(Exception value)
	{
		var message = $"(OnNext) Exception encountered:\nType: {value.GetType().ToString()}\tMessage: {value.Message}\nSource: {value.Source}\nStackTrace: {value.StackTrace}";
		DivinityApp.Log(message);
		MessageBox.Show(message, "Error Encountered", MessageBoxButton.OK, MessageBoxImage.Error);
	}

	public void OnError(Exception value)
	{
		var message = $"(OnError) Exception encountered:\nType: {value.GetType().ToString()}\tMessage: {value.Message}\nSource: {value.Source}\nStackTrace: {value.StackTrace}";
		DivinityApp.Log(message);
	}

	public void OnCompleted()
	{
		//if (Debugger.IsAttached) Debugger.Break();
		//RxApp.MainThreadScheduler.Schedule(() => { throw new NotImplementedException(); });
	}
}
