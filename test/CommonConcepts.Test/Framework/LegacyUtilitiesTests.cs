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
using Rhetos.Utilities;

namespace CommonConcepts.Test.Framework
{
    [TestClass]
    public class LegacyUtilitiesTests
    {
#pragma warning disable CS0618 // Type or member is obsolete

        [TestMethod]
        public void StaticConfiguration()
        {
            string connectionString1 = new Configuration().GetString(ConnectionString.ConnectionStringConfigurationKey, null).Value;
            Assert.IsTrue(!string.IsNullOrEmpty(connectionString1));

            string connectionString2 = ConfigUtility.GetAppSetting(ConnectionString.ConnectionStringConfigurationKey);
            Assert.IsTrue(!string.IsNullOrEmpty(connectionString2));
        }

#pragma warning restore CS0618 // Type or member is obsolete
    }
}
