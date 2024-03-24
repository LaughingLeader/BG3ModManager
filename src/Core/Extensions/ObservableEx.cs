using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace ModManager;
public static class ObservableEx
{
	//public static IObservable<Unit> StartAsync(Func<CancellationToken, Task> actionAsync, IScheduler scheduler)

	/// <summary>
	/// Workaround for the fact that StartAsync is blocking.
	/// See here:
	/// https://github.com/dotnet/reactive/issues/457
	/// </summary>
	public static IObservable<Unit> CreateAndStartAsync(Func<CancellationToken, Task> func, IScheduler scheduler)
	{
		return Observable.Defer(() => Observable.StartAsync(func, scheduler)).SubscribeOn(scheduler);
	}
}
