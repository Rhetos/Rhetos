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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class DatabaseTimeCacheTest
    {
        [TestMethod]
        public void GetDatabaseTimeCached_DbAhead()
        {
            RunTests(
                baseSysTime: new DateTime(2001, 2, 3, 4, 0, 0),
                dbOffset: new TimeSpan(2, 30, 10),
                tests: new ListOfTuples<int, double, bool>
                {
                    // Format: minute, second, should read time from db.
                    { 59, 58.1, true }, // read from db 1
                    { 59, 58.2, true }, // read from db 2
                    { 59, 58.3, true }, // read from db 3
                    { 59, 58.4, false }, // after 3 reads from db, the cache is used
                    { 59, 59.9, false },
                    // reset cache on every 30 minutes of *system* clock
                    { 60, 0.1, true }, // read from db 1
                    { 60, 0.2, true }, // read from db 2
                    { 60, 0.3, true }, // read from db 3
                    { 60, 0.4, false }, // after 3 reads from db, the cache is used
                    { 60, 1, false },
                    { 60, 2, false },
                    { 60, 3, false },
                    { 60, 4, false },
                    
                    { 89, 49.9, false},
                    // reset cache on 30 minutes of *database* clock
                    { 89, 50.1, true }, // read from db 1
                    { 89, 50.2, true }, // read from db 2
                    { 89, 50.3, true }, // read from db 3
                    { 89, 50.4, false }, // after 3 reads from db, the cache is used

                    { 89, 59, false },
                    { 89, 59.6, false },
                    // reset cache on 30 minutes of *system* clock
                    { 90, 0.1, true }, // read from db 1
                    { 90, 1.2, true }, // read from db 2
                    { 90, 1.3, true }, // read from db 3
                    { 90, 1.4, false }, // after 3 reads from db, the cache is used
                
                    { 119, 49, false },
                    { 119, 49.6, false },
                    // reset cache on 30 minutes of *database* clock
                    { 119, 50.1, true }, // read from db 1
                    { 119, 51.2, true }, // read from db 2
                    { 119, 51.3, true }, // read from db 3
                    { 119, 51.4, false }, // after 3 reads from db, the cache is used
                });
        }

        [TestMethod]
        public void GetDatabaseTimeCached_Db0()
        {
            RunTests(
                baseSysTime: new DateTime(2001, 2, 3, 4, 0, 0),
                dbOffset: new TimeSpan(0, 0, 0),
                tests: new ListOfTuples<int, double, bool>
                {
                    // Format: minute, second, should read time from db.
                    { 59, 58.1, true }, // read from db 1
                    { 59, 58.2, true }, // read from db 2
                    { 59, 58.3, true }, // read from db 3
                    { 59, 58.4, false }, // after 3 reads from db, the cache is used
                    { 59, 59.4, false },
                    { 59, 59.9, false },
                    // reset cache on every 30 minutes of *system* clock
                    { 60, 0.1, true }, // read from db 1
                    { 60, 1.2, true }, // read from db 2
                    { 60, 9.3, true }, // read from db 3
                    { 60, 9.4, false }, // after 3 reads from db, the cache is used
                    { 60, 11, false },
                    { 60, 12, false },
                    { 60, 13, false },
                    { 60, 14, false },

                    { 89, 59, false },
                    { 89, 59.6, false },
                    // reset cache on 30 minutes of *system* clock
                    { 90, 1.1, true }, // read from db 1
                    { 90, 1.2, true }, // read from db 2
                    { 90, 1.3, true }, // read from db 3
                    { 90, 1.4, false }, // after 3 reads from db, the cache is used
                });
        }

        [TestMethod]
        public void GetDatabaseTimeCached_ClockMovedBack()
        {
            foreach (int daytimeCorrectionMinutes in new[] { 0, -60, 30, 60 })
                RunTests(
                    baseSysTime: new DateTime(2001, 2, 3, 4, 0, 0),
                    dbOffset: new TimeSpan(0, 0, 0),
                    tests: new ListOfTuples<int, double, bool>
                    {
                        // Format: minute, second, should read time from db.
                        { 60, 0.1, true }, // read from db 1
                        { 60, 0.2, true }, // read from db 2
                        { 60, 0.3, true }, // read from db 3
                        { 61, 10, false }, // after 3 reads from db, the cache is used
                        { 61, 15, false },
                        { 61 + daytimeCorrectionMinutes, 10.1, true }, // clocked moved 1 hour back
                        { 61 + daytimeCorrectionMinutes, 10.2, true }, // read 2
                        { 61 + daytimeCorrectionMinutes, 10.3, true }, // read 3
                        { 61 + daytimeCorrectionMinutes, 10.4, false },

                        // reset cache on every 30 minutes of *system* clock
                        { 89 + daytimeCorrectionMinutes, 59.9, false },
                        { 90 + daytimeCorrectionMinutes, 0.1, true },
                        { 90 + daytimeCorrectionMinutes, 0.2, true },
                        { 90 + daytimeCorrectionMinutes, 0.3, true },
                        { 90 + daytimeCorrectionMinutes, 0.4, false },
                    });
        }


        [TestMethod]
        public void GetDatabaseTimeCached_BestLatency()
        {
            var tests = new ListOfTuples<ListOfTuples<int, double>, int>
            {
                // Format: 3 x (db latency ms, db clock offset seconds) => expected clock offset
                { new ListOfTuples<int, double> { { 20,  11 }, { 30, 22 }, { 40, 33 } }, 11 },
                { new ListOfTuples<int, double> { { 40,  11 }, { 20, 22 }, { 30, 33 } }, 22 },
                { new ListOfTuples<int, double> { { 40,  11 }, { 30, 22 }, { 20, 33 } }, 33 },
            };

            for (int t = 0; t < tests.Count(); t++)
            {
                var test = tests[t];
                var dbUsage = new List<bool>();
                TimeSpan lastDbOffset = TimeSpan.MaxValue;

                DatabaseTimeCache.Reset();
                DateTime systemTime = new DateTime(2001, 2, 3, 4, 5, 6);

                foreach (var sqlClock in test.Item1.Concat(new[] { Tuple.Create<int, double>(70, 70) }))
                {
                    DateTime dbTime = DateTime.MinValue;
                    bool dbRead = false;
                    DateTime computed = DatabaseTimeCache.GetDatabaseTimeCached(
                        () =>
                        {
                            dbRead = true;
                            // There is no need to use Thred.Sleep(sqlClock.Item1) here, because db latency is simulated by increasing the systemTime value.
                            return dbTime = systemTime.AddSeconds(sqlClock.Item2);
                        },
                        () =>
                        {
                            return systemTime = systemTime.AddMilliseconds(sqlClock.Item1);
                        } );

                    dbUsage.Add(dbRead);
                    lastDbOffset = computed.Subtract(systemTime);
                    Console.WriteLine($"Test {t} {sqlClock.Item1} {sqlClock.Item2}: sys {systemTime.ToString("o")}, db {dbTime.ToString("o")} " +
                        $"=> {ReportDbOrCache(dbRead)} {computed.ToString("o")}");
                }

                Assert.AreEqual("read db, read db, read db, read cache", TestUtility.Dump(dbUsage, ReportDbOrCache),
                    $"Test {t}: Should use cache after 3rd query from database.");
                Assert.AreEqual(test.Item2, Convert.ToInt32(lastDbOffset.TotalSeconds),
                    $"Test {t}: When using datetime cache, the database offset should be the one taken with the smallest latency.");
            }
        }

        private void RunTests(DateTime baseSysTime, TimeSpan dbOffset, ListOfTuples<int, double, bool> tests)
        {
            DatabaseTimeCache.Reset();

            foreach (var test in tests)
            {
                DateTime systemTime = baseSysTime.AddMinutes(test.Item1).AddSeconds(test.Item2);
                DateTime dbTime = systemTime.Add(dbOffset);
                bool dbRead = false;
                DateTime computed = DatabaseTimeCache.GetDatabaseTimeCached(() => { dbRead = true; return dbTime; }, () => systemTime);

                string report = $"Test {test.Item1} {test.Item2}: sys {systemTime.ToString("o")}, db {dbTime.ToString("o")} " +
                    $"=> {ReportDbOrCache(dbRead)} {computed.ToString("o")}";
                Console.WriteLine(report);
                Assert.AreEqual(ReportDbOrCache(test.Item3), ReportDbOrCache(dbRead), report);
                Assert.AreEqual(dbTime, computed, report);
            }
        }

        private string ReportDbOrCache(bool readDb) => readDb ? "read db" : "read cache";

        private void Print(DateTime dateTime) => Console.WriteLine(dateTime.ToString("o"));
    }
}