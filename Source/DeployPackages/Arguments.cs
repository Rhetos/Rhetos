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

namespace DeployPackages
{
    class Arguments
    {
        public bool Help { get; private set; }
        public bool StartPaused { get; private set; }
        public bool Debug { get; private set; }
        public bool NoPauseOnError { get; private set; }

        public Arguments(string[] args)
        {
            var arguments = new List<string>(args);

            if (arguments.Contains("/?", StringComparer.InvariantCultureIgnoreCase))
            {
                ShowHelp();
                Help = true;
                return;
            }

            if (Pop(arguments, "/StartPaused") != -1)
                StartPaused = true;

            if (Pop(arguments, "/Debug") != -1)
                Debug = true;

            if (Pop(arguments, "/NoPause") != -1)
                NoPauseOnError = true;

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
            Console.WriteLine("/Debug         Generates nonoptimized dlls (ServerDom.dll, e.g.) for debuging.");
            Console.WriteLine("/NoPause       Don't pause on error. Use this switch for build automation.");
        }

        private int Pop(List<string> arguments, string option)
        {
            var position = arguments.FindIndex(a => option.Equals(a, StringComparison.InvariantCultureIgnoreCase));
            if (position != -1)
                arguments.RemoveAt(position);

            return position;
        }
    }
}
