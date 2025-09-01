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
using Rhetos;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using System;
using System.Linq;
using TestMaxValue;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MaxValueTest
    {
        [TestMethod]
        public void ShouldThrowUserExceptionOnInsertInteger()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleInteger { Value = 3 };
                TestUtility.ShouldFail<UserException>(
                    () => repository.TestMaxValue.SimpleInteger.Insert(new[] { entity }));
            }
        }

        [TestMethod]
        public void NormallyInsertInteger()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleInteger { Value = 1 };
                repository.TestMaxValue.SimpleInteger.Insert(new[] { entity });
            }
        }
        
        [TestMethod]
        public void ShouldThrowUserExceptionOnUpdate()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleInteger { Value = 1 };
                repository.TestMaxValue.SimpleInteger.Insert(new[] { entity });

                entity.Value = 10;
                TestUtility.ShouldFail<UserException>(
                    () => repository.TestMaxValue.SimpleInteger.Update(new[] { entity }));
            }
        }

        [TestMethod]
        public void ShouldThrowUserExceptionOnInsertDecimal()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleDecimal { Value = (decimal)3.33 };
                TestUtility.ShouldFail<UserException>(
                    () => repository.TestMaxValue.SimpleDecimal.Insert(new[] { entity }));
            }
        }

        [TestMethod]
        public void NormallyInsertDecimal()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleDecimal { Value = (decimal)2.30 };
                repository.TestMaxValue.SimpleDecimal.Insert(new[] { entity });
            }
        }


        [TestMethod]
        public void ShouldThrowUserExceptionOnInsertMoney()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleMoney { Value = (decimal)3.33 };
                TestUtility.ShouldFail<UserException>(
                    () => repository.TestMaxValue.SimpleMoney.Insert(new[] { entity }));
            }
        }

        [TestMethod]
        public void NormallyInsertMoney()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleMoney { Value = (decimal)2.30 };
                repository.TestMaxValue.SimpleMoney.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldThrowUserExceptionOnInsertDate()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleDate { Value = new DateTime(2013, 7, 7) };
                TestUtility.ShouldFail<UserException>(
                    () => repository.TestMaxValue.SimpleDate.Insert(new[] { entity }));
            }
        }

        [TestMethod]
        public void NormallyInsertDate()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleDate { Value = new DateTime(2013, 7, 3) };
                repository.TestMaxValue.SimpleDate.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldThrowUserExceptionOnInsertDateTime()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleDateTime { Value = new DateTime(2013, 7, 5, 12, 34, 59) };
                TestUtility.ShouldFail<UserException>(
                    () => repository.TestMaxValue.SimpleDateTime.Insert(new[] { entity }));
            }
        }

        [TestMethod]
        public void NormallyInsertDateTime()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleDateTime { Value = new DateTime(2013, 7, 5, 12, 33, 1) };
                repository.TestMaxValue.SimpleDateTime.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void NullValue()
        {
            using (var scope = TestScope.Create())
            {
                var repository = scope.Resolve<Common.DomRepository>();
                var entity = new SimpleInteger { ID = Guid.NewGuid(), Value = null };
                repository.TestMaxValue.SimpleInteger.Insert(new[] { entity });
                Assert.IsNull(repository.TestMaxValue.SimpleInteger.Load(new[] { entity.ID }).Single().Value);
            }
        }
    }
}
