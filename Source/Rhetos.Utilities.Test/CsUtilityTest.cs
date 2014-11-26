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

using Microsoft.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.TestCommon;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rhetos.Utilities.Test
{
    [TestClass]
    public class CsUtilityTest
    {
        [TestMethod]
        public void QuotedString()
        {
            string stringConstant = "\r\nabc \" \\ \\\" 123 \\\"\" \\\\\"\"\r\n\t\t \rx\nx ";

            string code = string.Format(
                @"using System;
                namespace GeneratedModuleQuotedString
                {{
                    public class C
                    {{
                        public static string F1()
                        {{
                            return {0};
                        }}
                        public static string F2()
                        {{
                            return {1};
                        }}
                    }}
                }}",
                CsUtility.QuotedString(stringConstant),
                CsUtility.QuotedString(null));

            Console.WriteLine(code);
            CSharpCodeProvider provider = new CSharpCodeProvider();
            CompilerResults results = provider.CompileAssemblyFromSource(new CompilerParameters(new string[] { }, "GeneratedQuotedStringAssembly"), code);
            foreach (CompilerError error in results.Errors)
                Console.WriteLine(error);
            Assert.AreEqual(0, results.Errors.Count, "Compiler errors");

            Console.WriteLine("CompiledAssembly: " + results.CompiledAssembly.Location);
            Type generatedClass = results.CompiledAssembly.GetType("GeneratedModuleQuotedString.C");

            {
                MethodInfo generatedMethod = generatedClass.GetMethod("F1");
                string generatedCodeResult = (string)generatedMethod.Invoke(null, new object[] { });
                Assert.AreEqual(stringConstant, generatedCodeResult);
            }

            {
                MethodInfo generatedMethod = generatedClass.GetMethod("F2");
                string generatedCodeResult = (string)generatedMethod.Invoke(null, new object[] { });
                Assert.IsNull(generatedCodeResult);
            }
        }

        [TestMethod()]
        public void ValidateNameTest()
        {
            string[] validNames = new[] {
                "abc", "ABC", "i",
                "a12300", "a1a",
                "_abc", "_123", "_", "a_a_"
            };

            string[] invalidNames = new[] {
                "0", "2asdasd", "123", "1_",
                null, "",
                " abc", "abc ", " ",
                "!", "@", "#", "a!", "a@", "a#",
                "ač", "č",
            };

            foreach (string name in validNames)
            {
                Console.WriteLine("Testing valid name '" + name + "'.");
                string error = CsUtility.GetIdentifierError(name);
                Console.WriteLine("Error: " + error);
                Assert.IsNull(error);
            }

            foreach (string name in invalidNames)
            {
                Console.WriteLine("Testing invalid name '" + name + "'.");
                string error = CsUtility.GetIdentifierError(name);
                Console.WriteLine("Error: " + error);

                if (name == null)
                    TestUtility.AssertContains(error, "null");
                else if (name == "")
                    TestUtility.AssertContains(error, "empty");
                else if (name == "")
                    TestUtility.AssertContains(error, new[] { name, "not valid" });
            }
        }
    }
}
