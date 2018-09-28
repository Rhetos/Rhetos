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

using CommonConcepts.Test.Helpers;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Logging;
using Rhetos.Deployment;

namespace CommonConcepts.Test
{
    [TestClass]
    public class DefaultValueTest
    {
        [TestMethod]
        public void ValuesShouldBeSet()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var ctx = container.Resolve<Common.ExecutionContext>();

                var userName = ctx.UserInfo.UserName;
                var id = Guid.NewGuid();
                var idTestneReference = Guid.NewGuid();

                repository.DefaultValue.DefaultValueTestReference.Delete(repository.DefaultValue.DefaultValueTestReference.Load());

                repository.DefaultValue.DefaultValueTestReference.Insert(new[] {
                    new DefaultValue.DefaultValueTestReference {
                        ID = idTestneReference,
                        Name = "test1"
                    }
                });

                var testClass = new DefaultValue.DefaultValueTestClass
                {
                    ID = id
                };

                repository.DefaultValue.DefaultValueTestClass.Delete(repository.DefaultValue.DefaultValueTestClass.Load());
                repository.DefaultValue.DefaultValueTestClass.Insert(new[] { testClass });

                var inserted = repository.DefaultValue.DefaultValueTestClass.Load(new[] { id }).Single();

                Assert.AreEqual(49, inserted.IntegerValue);
                Assert.AreEqual(userName, inserted.ShortStringValue);
                Assert.AreEqual(idTestneReference, inserted.DefaultValueTestReferenceID);
                Assert.AreEqual((decimal)7.7, inserted.DecimalValue);
                Assert.AreEqual((decimal)7.7, inserted.MoneyValue);
                Assert.AreEqual(DateTime.Now.Date, inserted.DateValue);
                Assert.IsNotNull(inserted.DateTimeValue);
                Assert.AreEqual("tmp01", inserted.ShortStringValueWithAutoCode);
            }
        }
    }
}