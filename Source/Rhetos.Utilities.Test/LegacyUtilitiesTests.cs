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

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class LegacyUtilitiesTests
    {
#pragma warning disable CS0618 // Type or member is obsolete

        [TestMethod]
        public void SqlUtilityWorksCorrectly()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider())
                .AddConfigurationManagerConfiguration()
                .Build();
            LegacyUtilities.Initialize(configuration);

            Assert.AreEqual("MsSql", SqlUtility.DatabaseLanguage);
            Assert.IsFalse(string.IsNullOrEmpty(SqlUtility.ConnectionString));
            Assert.AreEqual(31, SqlUtility.SqlCommandTimeout);
        }

        [TestMethod]
        public void PathsOnNullEnvironment()
        {
            var configuration = new ConfigurationBuilder(new ConsoleLogProvider()).Build();
            Paths.Initialize(configuration);

            TestUtility.ShouldFail<FrameworkException>(() => Console.WriteLine(Paths.RhetosServerRootPath),
                "Paths property 'RhetosServerRootPath' is not configured in 'unspecified' environment.");
            TestUtility.ShouldFail<FrameworkException>(() => Console.WriteLine(Paths.ResourcesFolder), "ResourcesFolder");
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
