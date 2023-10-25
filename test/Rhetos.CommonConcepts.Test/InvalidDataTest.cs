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
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class InvalidDataTest
    {
        [TestMethod]
        public void GetErrorMessageMethodName()
        {
            var tests = new ListOfTuples<string, string, string>()
            {
                { "Mod.Ent", "Filt", "GetErrorMessage_Filt" },
                { "Mod.Ent", "Mod.Filt", "GetErrorMessage_Filt" },
                { "Mod.Ent", "Mod2.Filt", "GetErrorMessage_Mod2_2E_Filt" },
                { "Mod.Ent", "Dictionary<List<System.Guid>, object[]>", "GetErrorMessage_Dictionary_3C_List_3C_System_2E_Guid_3E__2C__20_object_5B__5D__3E_" },
            };

            foreach (var test in tests)
            {
                Console.WriteLine("Input: " + test.Item1 + ", " + test.Item2 + ".");
                var dataStructureParts = test.Item1.Split('.');
                var invalidData = new InvalidDataInfo
                {
                    Source = new DataStructureInfo { Module = new ModuleInfo { Name = dataStructureParts[0] }, Name = dataStructureParts[1] },
                    FilterType = test.Item2,
                    ErrorMessage = "[Test]"
                };
                Assert.AreEqual(test.Item3, invalidData.GetErrorMessageMethodName());                    
            }
        }
    }
}
