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
