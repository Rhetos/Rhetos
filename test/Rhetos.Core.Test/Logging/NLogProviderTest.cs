﻿/*
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
using Rhetos.Utilities;
using System;
using System.Threading.Tasks;

namespace Rhetos.Logging.Test
{
    [TestClass]
    public class NLogProviderTest
    {
        [TestMethod]
        [Timeout(1000)]
        public void GetLoggerTest_Performance()
        {
            NLogProvider logProvider = new NLogProvider(new LoggingOptions());
            for (int i = 0; i < 100*1000; i++)
            {
                var logger = logProvider.GetLogger("abc");
                Assert.IsNotNull(logger);
            }
        }

        [TestMethod]
        public void GetLoggerTest_ThreadSafe()
        {
            NLogProvider logProvider = new NLogProvider(new LoggingOptions());
            Parallel.For(1, 20, x => Assert.IsNotNull(logProvider.GetLogger("abc")));
        }

        [TestMethod]
        public void LoggerName()
        {
            NLogProvider logProvider = new NLogProvider(new LoggingOptions());
            string loggerName = $"Space Dot.{Guid.NewGuid().ToString()}";
            Console.WriteLine(loggerName);
            var logger = logProvider.GetLogger(loggerName);
            Assert.AreEqual(loggerName, logger.Name);
        }
    }
}
