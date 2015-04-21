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
using TestRegex;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using Rhetos.Configuration.Autofac;
using Rhetos.Dom.DefaultConcepts;

namespace CommonConcepts.Test
{
    [TestClass]
    public class RegExMatchTest
    {
        [TestMethod]
        public void ShouldThowUserExceptionOnInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { StringFrom200To249 = "." };
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Insert(new[] { entity }),
                    "StringFrom200To249 must be between 200 and 249", "Property:StringFrom200To249");
            }
        }

        [TestMethod]
        public void ShouldNotThrowUserExceptionOnInsert()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { StringFrom200To249 = "205" };
                repository.TestRegex.Simple.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void EmptyValuesAreAllowed()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { StringFrom200To249 = null };
                repository.TestRegex.Simple.Insert(new[] { entity });
            }
        }

        [TestMethod]
        public void EmptyValuesAreNotAllowedIfRequiredSet()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new SimpleRequired { StringFrom200To249 = null };
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.SimpleRequired.Insert(new[] { entity }),
                    "Required", "Property:StringFrom200To249");
            }
        }

        [TestMethod]
        public void ShouldThowUserExceptionOnUpdate()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { StringFrom200To249 = "205" };
                repository.TestRegex.Simple.Insert(new[] { entity });

                entity.StringFrom200To249 = "259";
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Update(new[] { entity }),
                    "StringFrom200To249 must be between 200 and 249", "Property:StringFrom200To249");
            }
        }

        [TestMethod]
        public void ExactMatch()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { StringFrom200To249 = "a 205 a" };

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Insert(new[] { entity }),
                    "StringFrom200To249 must be between 200 and 249", "Property:StringFrom200To249");
            }
        }

        [TestMethod]
        public void ExactMatch2()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { StringFrom200To249 = " 205" };

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Insert(new[] { entity }),
                    "StringFrom200To249 must be between 200 and 249", "Property:StringFrom200To249");
            }
        }

        [TestMethod]
        public void ExactMatch3()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { StringFrom200To249 = "205 " };

                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Insert(new[] { entity }),
                    "StringFrom200To249 must be between 200 and 249", "Property:StringFrom200To249");
            }
        }

        [TestMethod]
        public void UnicodeTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { UnicodeTest = "čćČĆテスト" };
                repository.TestRegex.Simple.Insert(new[] { entity });

                entity.UnicodeTest = "x";
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Update(new[] { entity }),
                    "must match", "Property:UnicodeTest");
            }
        }

        [TestMethod]
        public void WhitespaceTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { WhitespaceTest = "a\r\nb\tc" };
                repository.TestRegex.Simple.Insert(new[] { entity });

                entity.WhitespaceTest = "x";
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Update(new[] { entity }),
                    "must match", "Property:WhitespaceTest");
            }
        }

        [TestMethod]
        public void SpecialCharTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { SpecialCharTest = @"a!@#$%^&*()_+-=[]\{}|;':"",./<>?" };
                repository.TestRegex.Simple.Insert(new[] { entity });

                entity.SpecialCharTest = "x";
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Update(new[] { entity }),
                    "must match", "Property:SpecialCharTest");
            }
        }

        [TestMethod]
        public void DefaultErrorMessageTest()
        {
            using (var container = new RhetosTestContainer())
            {
                var repository = container.Resolve<Common.DomRepository>();
                var entity = new Simple { DefaultErrorMessageTest = "123" };
                repository.TestRegex.Simple.Insert(new[] { entity });

                entity.DefaultErrorMessageTest = "x";
                TestUtility.ShouldFail<Rhetos.UserException>(
                    () => repository.TestRegex.Simple.Update(new[] { entity }),
                    "Property DefaultErrorMessageTest does not match required format.", "Property:DefaultErrorMessageTest");
            }
        }
    }
}
