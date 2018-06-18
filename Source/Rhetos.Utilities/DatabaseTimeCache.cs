using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    internal static class DatabaseTimeCache
    {
        private static TimeSpan _latency = TimeSpan.MaxValue;
        private static TimeSpan _difference = TimeSpan.Zero;
        private static DateTime _obsoleteAfter = DateTime.MinValue;
        private static int _retries = 0;
        private static object _lock = new object();
        /// <summary>
        /// Retries are used to eliminate imprecision caused by initialization of the SQL connection (or a connection pool).
        /// The first response from a local SQL Server may have latency around 30ms, while the consecutive responses execute in 1ms or 0ms.
        /// </summary>
        private const int MaxRetries = 3;

        public static DateTime GetDatabaseTimeCached(Func<DateTime> getDatabaseTimeFromDatabase)
        {
            DateTime now = DateTime.Now;

            if (now > _obsoleteAfter || _retries < MaxRetries) // Same condition as below!
            {
                lock (_lock)
                {
                    if (now > _obsoleteAfter || _retries < MaxRetries)
                    {
                        DateTime start = now;
                        var databaseTime = getDatabaseTimeFromDatabase();
                        now = DateTime.Now; // Refreshing current time to avoid including initial SQL connection time.
                        var newLatency = now.Subtract(start);

                        if (now > _obsoleteAfter)
                            _retries = 0;
                        if (_retries == 0 || newLatency <= _latency)
                        {
                            _latency = newLatency;
                            _difference = databaseTime - now;
                            _obsoleteAfter = now.AddMinutes(1); // Short expiration time to minimize errors on local or database time updates, daylight savings and other.
                        }

                        _retries++;
                    }
                }
            }
            return now + _difference;
        }
    }
}
