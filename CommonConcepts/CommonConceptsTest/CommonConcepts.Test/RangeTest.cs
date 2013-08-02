/*
    Copyright (C) 2013 Omega software d.o.o.

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
using TestRange;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RangeTest
    {
        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnInsert()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new SimpleRange { FromValue = 1, ToValue = 0 };
                repository.TestRange.SimpleRange.Insert(new[] { entity });
            }
        }
        
        [TestMethod]
        public void ShouldNotThrowUserExceptionOnInsert()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new SimpleRange { FromValue = (decimal)1.1, ToValue = (decimal)2.0 };
                repository.TestRange.SimpleRange.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldInsertEntityWithoutRangeTo()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestRange.SimpleRange;",
                    });

                var repository = new Common.DomRepository(executionContext);
                var entity = new SimpleRange { FromValue = 1 };
                var entity2 = new SimpleRange { ToValue = 1 };
                repository.TestRange.SimpleRange.Insert(new[] { entity, entity2 });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnUpdate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new SimpleRange { FromValue = 1, ToValue = 5 };
                repository.TestRange.SimpleRange.Insert(new[] { entity });

                entity.ToValue = 0;
                repository.TestRange.SimpleRange.Update(new[] { entity });
            }
        }

        [TestMethod]
        public void ShoulInsertNormallyDate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new DateRangeWithoutDef { FromDate = DateTime.Today, ToDate = DateTime.Today.AddDays(2) };
                repository.TestRange.DateRangeWithoutDef.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShoulInsertNormallySameDates()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new DateRangeWithoutDef { FromDate = DateTime.Today, ToDate = DateTime.Today };
                repository.TestRange.DateRangeWithoutDef.Insert(new[] { entity });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnUpdateDate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new DateRangeWithoutDef { FromDate = DateTime.Today, ToDate = DateTime.Today.AddDays(2) };
                repository.TestRange.DateRangeWithoutDef.Insert(new[] { entity });

                entity.ToDate = DateTime.Today.AddDays(-2);
                repository.TestRange.DateRangeWithoutDef.Update(new[] { entity });
            }
        }

        [TestMethod]
        public void ShoulInsertNormallySameDateTime()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new DateTimeRangeWithoutDef { FromDate = DateTime.Today, ToDate = DateTime.Today };
                repository.TestRange.DateTimeRangeWithoutDef.Insert(new[] { entity });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnUpdateDateTime()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new DateTimeRangeWithoutDef { FromDate = DateTime.Today, ToDate = DateTime.Today.AddDays(2) };
                repository.TestRange.DateTimeRangeWithoutDef.Insert(new[] { entity });

                entity.ToDate = DateTime.Today.AddDays(-2);
                repository.TestRange.DateTimeRangeWithoutDef.Update(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldInsertNormallyJustOneDate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new DateRangeWithoutDef { FromDate = DateTime.Today };
                var entity2 = new DateRangeWithoutDef { ToDate = DateTime.Today };
                repository.TestRange.DateRangeWithoutDef.Insert(new[] { entity, entity2 });
            }
        }


        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThrowExceptionIfNotSetRequired()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new DateRangeWithRequired { ToDate = DateTime.Today };
                repository.TestRange.DateRangeWithRequired.Insert(new[] { entity });
            }
        }
    }
}
