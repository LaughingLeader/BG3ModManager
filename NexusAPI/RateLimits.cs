using System;

namespace NexusAPI
{
    public static class RateLimits
    {
        // x-rl-daily-limit
        public static int DailyLimit;
        // x-rl-daily-remaining
        public static int DailyRemaining;
        // x-rl-daily-reset
        public static DateTimeOffset DailyReset;
        // x-rl-hourly-limit
        public static int HourlyLimit;
        // x-rl-hourly-remaining
        public static int HourlyRemaining;
        // x-rl-hourly-offset
        public static DateTimeOffset HourlyReset;

        public static bool IsBlocked()
        {
            return DailyRemaining <= 0 && HourlyRemaining <= 0;
        }

        public static TimeSpan GetTimeUntilRenewal()
        {
            if (!IsBlocked())
                return TimeSpan.Zero;

            var reset = HourlyReset <= DailyReset ? HourlyReset : DailyReset;
            return reset - DateTimeOffset.UtcNow;
        }
    }
}