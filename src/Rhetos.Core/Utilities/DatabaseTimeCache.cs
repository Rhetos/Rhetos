/*
    Copyright (C) 2014 Omega software d.o.o.

    This file is part of Rhetos.

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as
    published by the Free Software Foundation, either version 3 of the
    License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;

namespace Rhetos.Utilities
{
    public static class DatabaseTimeCache
    {
        /// <summary>
        /// Retries are used to eliminate imprecision caused by initialization of the SQL connection (or a connection pool).
        /// The first response from a local SQL Server may have latency around 30ms, while the consecutive responses execute in 1ms or 0ms.
        /// </summary>
        private const int MaxRetries = 3;
        private const int ResetCacheOnMinutes = 30; // To support clock adjustments and daylight saving on the database server. Must be set to a divisor of 60.

        private static readonly object _lock = new();
        private static TimeSpan _latency = TimeSpan.MaxValue;
        private static TimeSpan _difference = TimeSpan.Zero;
        private static DateTime _obsoleteAfter = DateTime.MinValue;
        private static DateTime _lastTime = DateTime.MinValue;
        private static int _retries = 0;

        public static DateTime GetDatabaseTimeCached(Func<DateTime> getDatabaseTimeFromDatabase, Func<DateTime> getSystemTime)
        {
            DateTime now = getSystemTime();

            if (IsCacheInvalid(now))
            {
                lock (_lock)
                {
                    if (IsCacheInvalid(now))
                    {
                        DateTime start = now;
                        var databaseTime = getDatabaseTimeFromDatabase();
                        now = getSystemTime(); // Refreshing current time to avoid including initial SQL connection time.
                        var newLatency = now.Subtract(start);

                        if (now > _obsoleteAfter || now < _lastTime)
                            _retries = 0; // Reset latency optimization.
                        if (_retries == 0 || newLatency <= _latency)
                        {
                            _latency = newLatency;
                            _difference = databaseTime - now;
                            _obsoleteAfter = Min(NextCacheResetTime(now), NextCacheResetTime(databaseTime) - _difference);
                            _lastTime = now;
                        }
                        _retries++;
                    }
                }
            }

            _lastTime = now;
            return now + _difference;
        }

        private static bool IsCacheInvalid(DateTime now)
        {
            return now > _obsoleteAfter || now < _lastTime || _retries < MaxRetries;
        }

        /// <summary>
        /// For testing purpose.
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _latency = TimeSpan.MaxValue;
                _difference = TimeSpan.Zero;
                _obsoleteAfter = DateTime.MinValue;
                _lastTime = DateTime.MinValue;
                _retries = 0;
            }
        }

        /// <summary>
        /// For testing purpose.
        /// </summary>
        public static string Report()
        {
            lock (_lock)
            {
                return new
                {
                    _latency,
                    _difference,
                    _obsoleteAfter,
                    _lastTime,
                    _retries
                }.ToString();
            }
        }

        private static DateTime NextCacheResetTime(DateTime now)
        {
            return now.Date.AddSeconds(
                Math.Floor(now.TimeOfDay.TotalSeconds / (60 * ResetCacheOnMinutes) + 1)
                    * 60 * ResetCacheOnMinutes);
        }

        private static DateTime Min(DateTime dateTime1, DateTime dateTime2)
        {
            return dateTime1 < dateTime2 ? dateTime1 : dateTime2;
        }
    }
}
