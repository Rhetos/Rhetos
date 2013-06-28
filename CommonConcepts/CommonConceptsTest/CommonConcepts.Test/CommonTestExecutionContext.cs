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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Rhetos;

namespace CommonConcepts.Test
{
    public class CommonTestExecutionContext : TestExecutionContext
    {
        private static string _rhetosServerPath;
        public static string RhetosServerPath
        {
            get
            {
                if (_rhetosServerPath == null)
                {
                    var folder = new DirectoryInfo(Environment.CurrentDirectory);

                    if (folder.Name == "Out") // Unit testing subfolder.
                        folder = folder.Parent.Parent.Parent;

                    if (folder.Name == "Debug") // Unit testing at project level, not at solution level. It depends on the way the testing has been started.
                        folder = folder.Parent.Parent.Parent.Parent.Parent; // Climbing up CommonConcepts\CommonConceptsTest\CommonConcepts.Test\bin\Debug.

                    if (folder.GetDirectories().Any(subDir => subDir.Name == "Source"))
                        folder = new DirectoryInfo(Path.Combine(folder.FullName, @".\Source\Rhetos\"));

                    if (folder.Name != "Rhetos")
                        throw new FrameworkException("Cannot locate Rhetos folder from '" + Environment.CurrentDirectory + "'. Unexpected folder '" + folder.Name + "'.");

                    _rhetosServerPath = folder.FullName;
                }
                return _rhetosServerPath;
            }
        }

        public CommonTestExecutionContext() : base(false, RhetosServerPath)
        {
        }
    }
}
