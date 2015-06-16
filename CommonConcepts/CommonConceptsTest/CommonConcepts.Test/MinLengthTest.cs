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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using TestLengthLimit;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Utilities;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MinLengthTest
    {
        [TestMethod]
        public void ShouldThowUserExceptionOnInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleMinLength { StringMoreThan2Chars = "." };

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestLengthLimit.SimpleMinLength.Insert(entity),
                    "StringMoreThan2Chars", "minimum", "3");
            }
        }
        
        [TestMethod]
        public void ShouldNotThrowUserExceptionOnInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleMinLength { StringMoreThan2Chars = ".aaa" };
                repository.TestLengthLimit.SimpleMinLength.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldInsertEntity()
        {
            using (var container = new RhetosTestContainer())
            {
                container.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestLengthLimit.SimpleMaxLength;",
                    });

                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleMinLength { StringMoreThan2Chars = "abc" };
                repository.TestLengthLimit.SimpleMinLength.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldThowUserExceptionOnUpdate()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleMinLength { StringMoreThan2Chars = "1234" };
                repository.TestLengthLimit.SimpleMinLength.Insert(entity);
                entity.StringMoreThan2Chars = "1";

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestLengthLimit.SimpleMinLength.Update(entity),
                    "StringMoreThan2Chars", "minimum", "3");
            }
        }
    }
}
