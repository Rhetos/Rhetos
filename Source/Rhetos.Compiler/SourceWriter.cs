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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Rhetos.Dom;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.CodeDom.Compiler;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rhetos.Compiler
{
    public class SourceWriter : ISourceWriter
    {
        private readonly ILogger _logger;
        private readonly BuildOptions _buildOptions;
        private readonly FilesUtility _filesUtility;
        private readonly ConcurrentDictionary<string, string> _files = new ConcurrentDictionary<string, string>();

        public SourceWriter(BuildOptions buildOptions, ILogProvider logProvider, FilesUtility filesUtility)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _buildOptions = buildOptions;
            _filesUtility = filesUtility;
        }

        public void Add(string fileName, string content)
        {
            _files.AddOrUpdate(fileName, content, ErrorOnUpdate);
        }

        private string ErrorOnUpdate(string fileName, string oldValue)
        {
            throw new FrameworkException($"Multiple code generators are writing same generated file '{fileName}'.");
        }

        public void WriteAllFiles()
        {
            var deleteFiles = Directory.GetFiles(_buildOptions.GeneratedSourceFolder, "*")
                .Where(existing => !_files.Keys.Contains(Path.GetFileName(existing)));

            foreach (var deleteFile in deleteFiles)
            {
                _logger.Info(() => $"Deleting '{deleteFile}'.");
                _filesUtility.SafeDeleteFile(deleteFile);
            }

            foreach (var file in _files)
            {
                _logger.Info(() => $"Writing ' {file.Key}'.");

                using (var fs = new FileStream(Path.Combine(_buildOptions.GeneratedSourceFolder, file.Key), FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    using (var sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        sw.Write(file.Value);
                        fs.SetLength(fs.Position);
                    }
                }
            }
        }
    }
}