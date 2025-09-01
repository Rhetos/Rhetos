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
using CommonConcepts.Test.Helpers;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MaxLengthTest
    {
        [TestMethod]
        public void ShouldThrowUserExceptionOnInsert()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleMaxLength { StringLessThan10Chars = "More than 10 characters." };

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestLengthLimit.SimpleMaxLength.Insert(entity),
                    "StringLessThan10Chars", "maximum", "10");
            }
        }

        [TestMethod]
        public void ShouldInsertWithShortStringEntity()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestLengthLimit.SimpleMaxLength;",
                    });

                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleMaxLength { StringLessThan10Chars = "abc" };
                repository.TestLengthLimit.SimpleMaxLength.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldInsertEntity()
        {
            using (var scope = TestScope.Create())
            {
                scope.Resolve<ISqlExecuter>().ExecuteSql(new[]
                    {
                        "DELETE FROM TestLengthLimit.SimpleMaxLength;",
                    });

                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleMaxLength { LongStringLessThan10Chars = "abc" };
                repository.TestLengthLimit.SimpleMaxLength.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldThrowUserExceptionOnUpdate()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleMaxLength { LongStringLessThan10Chars = "123" };
                repository.TestLengthLimit.SimpleMaxLength.Insert(entity);

                entity.LongStringLessThan10Chars = "More than 10 characters.";
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestLengthLimit.SimpleMaxLength.Update(entity),
                    "StringLessThan10Chars", "maximum", "10");
            }
        }
    }
}
