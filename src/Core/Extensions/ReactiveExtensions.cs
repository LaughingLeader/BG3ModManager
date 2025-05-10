using System.Linq.Expressions;

namespace ModManager;

public static class ReactiveExtensions
{
	/// <summary>
	/// ToPropertyEx with deferSubscription set to true, and the default scheduler set to RxApp.MainThreadScheduler.
	/// </summary>
	/// <typeparam name="TObj"></typeparam>
	/// <typeparam name="TRet"></typeparam>
	/// <param name="obs"></param>
	/// <param name="source"></param>
	/// <param name="property"></param>
	/// <param name="initialValue"></param>
	/// <returns></returns>
	public static ObservableAsPropertyHelper<TRet> ToUIProperty<TObj, TRet>(this IObservable<TRet> obs, TObj source, Expression<Func<TObj, TRet>> property, TRet initialValue = default) where TObj : ReactiveObject
	{
		return obs.ToPropertyEx(source, property, initialValue, true, RxApp.MainThreadScheduler);
	}

	/// <summary>
	/// ToPropertyEx with deferSubscription set to false, and the default scheduler set to RxApp.MainThreadScheduler.
	/// deferSubscription is false so the value is set immediately, which is important when used in other logic, such as collection filters.
	/// </summary>
	/// <typeparam name="TObj"></typeparam>
	/// <typeparam name="TRet"></typeparam>
	/// <param name="obs"></param>
	/// <param name="source"></param>
	/// <param name="property"></param>
	/// <param name="initialValue"></param>
	/// <returns></returns>
	public static ObservableAsPropertyHelper<TRet> ToUIPropertyImmediate<TObj, TRet>(this IObservable<TRet> obs, TObj source, Expression<Func<TObj, TRet>> property, TRet initialValue = default) where TObj : ReactiveObject
	{
		return obs.ToPropertyEx(source, property, initialValue, false, RxApp.MainThreadScheduler);
	}

	#region Debounce

	//Source: https://github.com/dotnet/reactive/issues/395#issuecomment-1252835057


	/// <summary>
	/// Ignores all items following another item before the 'delay' window ends 
	/// </summary>
	public static IObservable<T> ThrottleFirst<T>(this IObservable<T> source, TimeSpan delay, IScheduler? timeSource = null)
		=> new ThrottleFirstObservable<T>(source, delay, timeSource ?? Scheduler.Default);

	sealed class ThrottleFirstObservable<T> : IObservable<T>
	{
		private readonly IObservable<T> _source;
		private readonly IScheduler _timeSource;
		private readonly TimeSpan _timespan;

		internal ThrottleFirstObservable(IObservable<T> source, TimeSpan timespan, IScheduler timeSource)
		{
			_source = source;
			_timeSource = timeSource;
			_timespan = timespan;
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			var parent = new ThrottleFirstObserver<T>(observer, _timespan, _timeSource);
			_source.Subscribe(parent, parent.DisposeCancel.Token);
			return parent;
		}
	}

	sealed class ThrottleFirstObserver<T> : IDisposable, IObserver<T>
	{
		private readonly IObserver<T> _downstream;
		private readonly TimeSpan _delay;
		private readonly IScheduler _timeSource;

		private DateTimeOffset _nextItemTime = DateTimeOffset.MinValue;

		internal CancellationTokenSource DisposeCancel { get; } = new();

		internal ThrottleFirstObserver(IObserver<T> downStream, TimeSpan delay, IScheduler timeSource)
		{
			_downstream = downStream;
			_timeSource = timeSource;
			_delay = delay;
		}

		public void Dispose() => DisposeCancel.Cancel();
		public void OnCompleted() => _downstream.OnCompleted();
		public void OnError(Exception error) => _downstream.OnError(error);

		/// <summary>
		/// Always emit 1st value
		/// Wait 'delay' before emitting any new value
		/// Ignores all values in between
		/// </summary>
		public void OnNext(T value)
		{
			var now = _timeSource.Now;
			if (now >= _nextItemTime)
			{
				_nextItemTime = now.Add(_delay);
				_downstream.OnNext(value);
			}
		}
	}

	#endregion

	public static IObservable<bool> AllTrue(this IObservable<bool> first, IObservable<bool> second) => first.CombineLatest(second).AllTrue();
	public static IObservable<bool> AllTrue(this IObservable<bool> first, IObservable<bool> second, IObservable<bool> third) => first.CombineLatest(second, third).AllTrue();
	public static IObservable<bool> AllTrue(this IObservable<(bool First, bool Second)> obs) => obs.Select(x => x.First && x.Second);
	public static IObservable<bool> AllTrue(this IObservable<(bool First, bool Second, bool Third)> obs) => obs.Select(x => x.First && x.Second && x.Third);
	public static IObservable<bool> AnyTrue(this IObservable<(bool First, bool Second)> obs) => obs.Select(x => x.First || x.Second);
}
