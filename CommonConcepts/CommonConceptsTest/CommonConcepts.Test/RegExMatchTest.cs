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
using TestRegex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RegExMatchTest
    {
        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnInsert()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new Simple { StringFrom200To249 = "." };
                repository.TestRegex.Simple.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void ShouldNotThrowUserExceptionOnInsert()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new Simple { StringFrom200To249 = "205" };
                repository.TestRegex.Simple.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void EmptyValuesAreAllowed()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new Simple { StringFrom200To249 = null };
                repository.TestRegex.Simple.Insert(new[] { entity });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void EmptyValuesAreNotAllowedIfRequiredSet()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new SimpleRequired { StringFrom200To249 = null };
                repository.TestRegex.SimpleRequired.Insert(new[] { entity });
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Rhetos.UserException))]
        public void ShouldThowUserExceptionOnUpdate()
        {
            using (var executionContext = new CommonTestExecutionContext())
            {
                var repository = new Common.DomRepository(executionContext);
                var entity = new Simple { StringFrom200To249 = "205" };
                repository.TestRegex.Simple.Insert(new[] { entity });

                entity.StringFrom200To249 = "259";
                repository.TestRegex.Simple.Update(new[] { entity });
            }
        }
    }
}
