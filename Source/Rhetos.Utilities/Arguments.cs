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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class DeployArguments
    {
        public bool Help { get; private set; }
        public bool StartPaused { get; private set; }
        public bool Debug { get; private set; }
        public bool NoPauseOnError { get; private set; }
        public bool IgnorePackageDependencies { get; private set; }
        public bool ShortTransactions { get; private set; }
        public bool DeployDatabaseOnly { get; private set; }
        public bool SkipRecompute { get; private set; }

        public DeployArguments(string[] args)
        {
            var arguments = new List<string>(args);

            if (arguments.Contains("/?", StringComparer.InvariantCultureIgnoreCase))
            {
                ShowHelp();
                Help = true;
                return;
            }

            StartPaused = Pop(arguments, "/StartPaused");
            Debug = Pop(arguments, "/Debug");
            NoPauseOnError = Pop(arguments, "/NoPause");
            IgnorePackageDependencies = Pop(arguments, "/IgnoreDependencies");
            ShortTransactions = Pop(arguments, "/ShortTransactions");
            DeployDatabaseOnly = Pop(arguments, "/DatabaseOnly");
            SkipRecompute = Pop(arguments, "/SkipRecompute");

            if (arguments.Count > 0)
            {
                ShowHelp();
                throw new ApplicationException("Unexpected command-line argument: '" + arguments.First() + "'.");
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Command-line arguments:");
            Console.WriteLine("/StartPaused   Use for debugging with Visual Studio (Attach to Process).");
            Console.WriteLine("/Debug         Generates unoptimized dlls (ServerDom.*.dll, e.g.) for debugging.");
            Console.WriteLine("/NoPause       Don't pause on error. Use this switch for build automation.");
            Console.WriteLine("/IgnoreDependencies  Allow installing incompatible versions of Rhetos packages.");
            Console.WriteLine("/ShortTransactions  Commit transaction after creating or dropping each database object.");
            Console.WriteLine("/DatabaseOnly  Keep old plugins and files in bin\\Generated.");
            Console.WriteLine("/SkipRecompute  Use this if you want to skip all computed data.");
        }

        /// <summary>
        /// Reads and removes the option form the arguments list.
        /// </summary>
        private bool Pop(List<string> arguments, string option)
        {
            var position = arguments.FindIndex(a => option.Equals(a, StringComparison.InvariantCultureIgnoreCase));
            if (position != -1)
                arguments.RemoveAt(position);

            return position != -1;
        }
    }
}
