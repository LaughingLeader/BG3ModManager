using System;
using System.Threading;
using System.Threading.Tasks;

namespace NexusAPI
{
    public sealed class Throttle
    {
        private readonly TimeSpan _maxPeriod;
        private readonly SemaphoreSlim _throttleActions, _throttlePeriods;

        public Throttle(int maxActions, TimeSpan maxPeriod)
        {
            this._throttleActions = new SemaphoreSlim(maxActions, maxActions);
            this._throttlePeriods = new SemaphoreSlim(maxActions, maxActions);
            this._maxPeriod = maxPeriod;
        }

        public Task<T> Queue<T>(Func<T> action)
        {
            return this._throttleActions.WaitAsync().ContinueWith(t =>
            {
                try
                {
                    this._throttlePeriods.Wait();

                    Task.Delay(this._maxPeriod).ContinueWith(tt =>
                    {
                        this._throttlePeriods.Release(1);
                    });

                    return action();
                }
                finally
                {
                    this._throttleActions.Release(1);
                }
            });
        }
    }
}