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

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RhetosBuildTask
{
    public class RhetosBuild : Task
    {
        [Required]
        public string RhetosBuildExePath { get; set; }

        [Required]
        public string ProjectDirectory { get; set; }

        public string AssemblyName { get; set; }

        public ITaskItem[] Assemblies { get; set; }

        public override bool Execute()
        {
            var commandLineArgument = GetCommandLineArguments();
            var processExitCode = ExecuteApplication(Path.GetFullPath(RhetosBuildExePath), commandLineArgument);
            return processExitCode == 0;
        }

        private string GetCommandLineArguments()
        {
            var commandLineArgument = $"build \"{ProjectDirectory}\"";

            if (!string.IsNullOrEmpty(AssemblyName))
                commandLineArgument = commandLineArgument + " --assembly-name " + AssemblyName;

            if (Assemblies != null && Assemblies.Any())
                commandLineArgument = commandLineArgument + " --assemblies " + string.Join(" ", Assemblies.Select(x => $"\"{x.ItemSpec}\""));

            return commandLineArgument;
        }

        private int ExecuteApplication(string path, string arguments)
        {
            Log.LogMessage(MessageImportance.High, path + " " + arguments); // Using LogMessage because LogCommandLine does not show in Build Output in Visual Studio.

            ProcessStartInfo start = new ProcessStartInfo(path)
            {
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            int processExitCode;
            using (Process process = Process.Start(start))
            {
                while (!process.HasExited)
                {
                    var log = process.StandardOutput.ReadLine();
                    while (process.StandardOutput.Peek() > -1)
                        log = log + Environment.NewLine + process.StandardOutput.ReadLine();

                    if (log != null && log.StartsWith("[Error]"))
                        Log.LogError(log);
                    else if(log != null)
                        Log.LogMessage(log);
                }


                var errorOutput = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(errorOutput))
                    Log.LogError(errorOutput);

                processExitCode = process.ExitCode;
            }

            return processExitCode;
        }
    }
}
