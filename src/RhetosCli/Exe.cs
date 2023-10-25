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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Rhetos
{
    internal static class Exe
    {
        public static int Run(
            string executable,
            IReadOnlyList<string> args,
            ILogger logger)
        {
            var arguments = ToArguments(args);

            ProcessStartInfo start = new ProcessStartInfo(executable)
            {
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var processOutputLock = new object();
            int processErrorCode;
            using (var process = Process.Start(start))
            {
                logger.Info(() => $"Started '{Path.GetFileName(executable)}' process {process.Id}.");

                var outputs = new[] { process.StandardOutput, process.StandardError };
                System.Threading.Tasks.Parallel.ForEach(outputs, output =>
                {
                    using (StreamReader reader = output)
                    {
                        string line;
                        while ((line = output.ReadLine()) != null)
                            lock (processOutputLock)
                            {
                                Console.WriteLine(line);
                            }
                    }
                });

                process.WaitForExit();
                processErrorCode = process.ExitCode;
            }

            return processErrorCode;
        }

        private static string ToArguments(IReadOnlyList<string> args)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < args.Count; i++)
            {
                if (i != 0)
                {
                    builder.Append(' ');
                }

                if (!args[i].Contains(' '))
                {
                    builder.Append(args[i]);

                    continue;
                }

                builder.Append('"');

                var pendingBackslashes = 0;
                for (var j = 0; j < args[i].Length; j++)
                {
                    switch (args[i][j])
                    {
                        case '\"':
                            if (pendingBackslashes != 0)
                            {
                                builder.Append('\\', pendingBackslashes * 2);
                                pendingBackslashes = 0;
                            }

                            builder.Append("\\\"");
                            break;

                        case '\\':
                            pendingBackslashes++;
                            break;

                        default:
                            if (pendingBackslashes != 0)
                            {
                                if (pendingBackslashes == 1)
                                {
                                    builder.Append('\\');
                                }
                                else
                                {
                                    builder.Append('\\', pendingBackslashes * 2);
                                }

                                pendingBackslashes = 0;
                            }

                            builder.Append(args[i][j]);
                            break;
                    }
                }

                if (pendingBackslashes != 0)
                {
                    builder.Append('\\', pendingBackslashes * 2);
                }

                builder.Append('"');
            }

            return builder.ToString();
        }
    }
}
