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
using TestMaxLength;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonConcepts.Test
{
    [TestClass]
    public class MaxLengthTest
    {
        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnInsert()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new Old1 { Name = "More than 5 characters." };
                repository.TestMaxLength.Old1.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldInsertEntity()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                executionContext.SqlExecuter.ExecuteSql(new[]
                    {
                        "DELETE FROM TestMaxLength.Old1;",
                    });

                var repository = new Common.DomRepository(executionContext);
                var entity = new Old1 { Name = "abc" };
                repository.TestMaxLength.Old1.Insert(new[] { entity });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnUpdate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new Old1 { Name = "More than 5 characters." };
                repository.TestMaxLength.Old1.Update(new[] { entity });
            }
        }
    }
}
