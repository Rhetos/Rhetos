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
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class BinaryTest
    {
        [TestMethod]
        public void ShouldUploadBinary()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestBinary.E;" });
                var repository = scope.Resolve<Common.DomRepository>();

                var rnd = System.Security.Cryptography.RandomNumberGenerator.Create();
                var blob = new Byte[10];
                rnd.GetBytes(blob);
                

                var entity = new TestBinary.E() { ID = Guid.NewGuid(), Blob = blob };
                repository.TestBinary.E.Insert(new[] { entity });

                var loaded = repository.TestBinary.E.Query().Where(item => item.ID == entity.ID).Single().Blob;
                Assert.IsTrue(Enumerable.SequenceEqual(blob, loaded));
            }
        }

        [TestMethod]
        public void LargeBinary()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[] { "DELETE FROM TestBinary.E;" });
                var repository = scope.Resolve<Common.DomRepository>();

                var rnd = System.Security.Cryptography.RandomNumberGenerator.Create();
                var blob = new Byte[1000000];
                rnd.GetBytes(blob);

                var entity = new TestBinary.E() { ID = Guid.NewGuid(), Blob = blob };
                repository.TestBinary.E.Insert(new[] { entity });

                var loaded = repository.TestBinary.E.Query().Where(item => item.ID == entity.ID).Single().Blob;
                Assert.IsTrue(Enumerable.SequenceEqual(blob, loaded));
            }
        }
    }
}
