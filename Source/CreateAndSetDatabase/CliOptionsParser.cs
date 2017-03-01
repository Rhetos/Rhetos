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

namespace CreateAndSetDatabase
{
    public static class CliOptionsParser
    {
        public static T Parse<T>(string[] arguments, bool hasCommand = false)
            where T : CliOptions
        {
            var result = Activator.CreateInstance<T>();
            var argumentQueue = new Queue<string>(arguments);

            if (hasCommand)
            {
                result.Command = argumentQueue.Dequeue();
            }

            while(argumentQueue.Count > 0)
            {
                if (!argumentQueue.Peek().StartsWith("-"))
                {
                    break;
                }

                var option = argumentQueue.Dequeue();
                var optionArgument = string.Empty;

                if (result.RequireOptionArgument(option) && argumentQueue.Count > 0)
                {
                    if (!argumentQueue.Peek().StartsWith("-"))
                    {
                        optionArgument = argumentQueue.Dequeue();
                    }
                }

                result.Options.Add(option.ToLower(), optionArgument);
            }

            result.Arguments = argumentQueue.ToArray();

            result.Initialize();
            return result;
        }
    }

    public abstract class CliOptions
    {
        public CliOptions()
        {
            Options = new Dictionary<string, string>();
        }

        public string Command { get; set; }

        public Dictionary<string, string> Options { get; set; }

        public string[] Arguments { get; set; }

        public virtual void Initialize() { }

        public virtual bool RequireOptionArgument(string option)
        {
            return false;
        }
    }
}
