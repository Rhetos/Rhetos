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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReplaceRegEx
{
    class Program
    {
        class Parameters
        {
            public string FileName { get; private set; }
            public string FindRegEx { get; private set; }
            public string ReplaceRegEx { get; private set; }
            public bool RootOnly { get; private set; }

            public Parameters(string[] args)
            {
                var msg = new StringBuilder();
                msg.AppendLine("Usage:");
                msg.AppendLine("ReplaceRegEx.exe <file name> <find regular expression> <replace regular expression> [/RootOnly]");
                msg.AppendLine();
                msg.AppendLine("Files will be searched recursively (unless /RootOnly switch is used) in current folder and subfolders.");
                msg.AppendLine("Use standard wildcards to specify file name.");
                msg.AppendLine("See examples of replacing with regular expressions at MSDN: 'Regex.Replace Method (String, String)'");
                msg.AppendLine("Works on unicode text files with BOM and ANSI files. Does not work on unicode files without BOM.");

                if (!(args.Length == 3
                    || args.Length == 4 && args[3].Equals("/RootOnly", StringComparison.OrdinalIgnoreCase)))
                    throw new ApplicationException(msg.ToString());

                FileName = args[0];
                FindRegEx = args[1];
                ReplaceRegEx = args[2];
                RootOnly = args.Length == 4 && args[3].Equals("/RootOnly", StringComparison.OrdinalIgnoreCase);
                Console.WriteLine("FileName = '" + FileName + "'");
                Console.WriteLine("FindRegEx = '" + FindRegEx + "'");
                Console.WriteLine("ReplaceRegEx = '" + ReplaceRegEx + "'");
                Console.WriteLine("RootOnly = " + RootOnly);
            }
        }

        static int Main(string[] args)
        {
            try
            {
                var parameters = new Parameters(args);

                var regex = new Regex(parameters.FindRegEx, RegexOptions.Compiled | RegexOptions.Singleline);

                foreach (string file in Directory.GetFiles(".", parameters.FileName, parameters.RootOnly ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories))
                {
                    Console.Write("Replacing text in file " + file + " ... ");

                    var text = File.ReadAllLines(file, Encoding.Default); // If file has BOM, the function will automatically detect format, otherwise it will use system default codepage.
                    var result = new List<string>(text.Length);
                    bool anyMatch = false;
                    foreach (string line in text)
                        if (regex.IsMatch(line))
                        {
                            anyMatch = true;
                            result.Add(regex.Replace(line, parameters.ReplaceRegEx));
                        }
                        else
                            result.Add(line);

                    if (anyMatch)
                    {
                        string oldText = string.Join(Environment.NewLine, text) + Environment.NewLine;
                        string newText = string.Join(Environment.NewLine, result.ToArray()) + Environment.NewLine;
                        if (!newText.Equals(oldText))
                        {
                            File.WriteAllText(file, newText, GuessFileEncoding(file));
                            Console.WriteLine("Done.");
                        }
                        else
                            Console.WriteLine("Old text is same as new text.");
                    }
                    else
                        Console.WriteLine("No match found.");
                }

                return 0;
            }
            catch (ApplicationException applicationException)
            {
                WriteError(applicationException.Message);
                return 1;
            }
            catch (Exception exception)
            {
                WriteError(exception.ToString());
                return 1;
            }
        }

        private static readonly List<Tuple<byte[], Encoding>> BomEncodings = Encoding.GetEncodings()
            .Select(encodingInfo => Tuple.Create(encodingInfo.GetEncoding().GetPreamble(), encodingInfo.GetEncoding()))
            .Where(bomEncoding => bomEncoding.Item1.Length > 0)
            .OrderBy(bomEncoding => -bomEncoding.Item1.Length)
            .ToList();

        private static Encoding GuessFileEncoding(string file)
        {
            byte[] buffer = new byte[] { 32, 32, 32, 32 };
            using (var fileStream = File.OpenRead(file))
                fileStream.Read(buffer, 0, 4);

            foreach (var bomEncoding in BomEncodings)
            {
                bool sameBom = !bomEncoding.Item1.Where((t, i) => buffer[i] != t).Any();
                if (sameBom)
                    return bomEncoding.Item2;
            }
            return Encoding.Default;
        }

        private static void WriteError(string message)
        {
            var oldFg = Console.ForegroundColor;
            var oldBg = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkRed;

            Console.WriteLine(message);

            Console.ForegroundColor = oldFg;
            Console.BackgroundColor = oldBg;
        }
    }
}
