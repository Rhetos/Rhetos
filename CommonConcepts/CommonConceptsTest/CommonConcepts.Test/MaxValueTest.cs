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
using TestMaxValue;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MaxValueTest
    {
        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnInsertInteger()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleInteger { Value = 3 };
                repository.TestMaxValue.SimpleInteger.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void NormallyInsertInteger()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleInteger { Value = 1 };
                repository.TestMaxValue.SimpleInteger.Insert(new[] { entity });
            }
        }
        
        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnUpdate()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleInteger { Value = 1 };
                repository.TestMaxValue.SimpleInteger.Insert(new[] { entity });

                entity.Value = 10;
                repository.TestMaxValue.SimpleInteger.Update(new[] { entity });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnInsertDecimal()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleDecimal { Value = (decimal)3.33 };
                repository.TestMaxValue.SimpleDecimal.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void NormallyInsertDecimal()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleDecimal { Value = (decimal)2.30 };
                repository.TestMaxValue.SimpleDecimal.Insert(new[] { entity });
            }
        }


        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnInsertMoney()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleMoney { Value = (decimal)3.33 };
                repository.TestMaxValue.SimpleMoney.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void NormallyInsertMoney()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleMoney { Value = (decimal)2.30 };
                repository.TestMaxValue.SimpleMoney.Insert(new[] { entity });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnInsertDate()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleDate { Value = new DateTime(2013, 7, 7) };
                repository.TestMaxValue.SimpleDate.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void NormallyInsertDate()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleDate { Value = new DateTime(2013, 7, 3) };
                repository.TestMaxValue.SimpleDate.Insert(new[] { entity });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnInsertDateTime()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleDateTime { Value = new DateTime(2013, 7, 5, 12, 34, 59) };
                repository.TestMaxValue.SimpleDateTime.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void NormallyInsertDateTime()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleDateTime { Value = new DateTime(2013, 7, 5, 12, 33, 1) };
                repository.TestMaxValue.SimpleDateTime.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void NullValue()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleInteger { ID = Guid.NewGuid(), Value = null };
                repository.TestMaxValue.SimpleInteger.Insert(new[] { entity });
                Assert.IsNull(repository.TestMaxValue.SimpleInteger.Load(new[] { entity.ID }).Single().Value);
            }
        }
    }
}
