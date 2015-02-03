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

using Rhetos.Logging;
using System;

namespace Rhetos.Deployment
{
    public static class DeploymentUtility
    {
        public static void WriteError(string msg)
        {
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(msg);
            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }

        private static ILogProvider _initializationLogProvider;

        /// <summary>To be used during system initialization while the IoC container is yet not built.
        /// In all other situations the ILogProvider should be resolved from the IoC container.</summary>
        public static ILogProvider InitializationLogProvider
        {
            get
            {
                if (_initializationLogProvider == null)
                    _initializationLogProvider = new NLogProvider();
                return _initializationLogProvider;
            }
            set
            {
                _initializationLogProvider = value;
            }
        }
    }
}
