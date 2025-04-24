﻿/*
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
using Rhetos.Utilities;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Compiler
{
    public class SourceWriter : ISourceWriter
    {
        private readonly ILogger _logger;
        private readonly RhetosBuildEnvironment _buildEnvironment;
        private readonly FilesUtility _filesUtility;
        private readonly ConcurrentDictionary<string, string> _files = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public SourceWriter(RhetosBuildEnvironment buildEnvironment, ILogProvider logProvider, FilesUtility filesUtility)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _buildEnvironment = buildEnvironment;
            _filesUtility = filesUtility;
        }

        public void Add(string relativePath, string content)
        {
            string filePath = Path.GetFullPath(Path.Combine(_buildEnvironment.GeneratedSourceFolder, relativePath));

            if (!FilesUtility.IsInsideDirectory(filePath, _buildEnvironment.GeneratedSourceFolder))
                throw new FrameworkException($"Generated source file '{filePath}' should be inside the folder '{_buildEnvironment.GeneratedSourceFolder}'." +
                    $" Provide a simple file name or a relative path for {nameof(ISourceWriter)}.{nameof(ISourceWriter.Add)} method.");
            
            _files.AddOrUpdate(filePath, content, ErrorOnUpdate);

            if (File.Exists(filePath))
            {
                if (File.ReadAllText(filePath, Encoding.UTF8).Equals(content, StringComparison.Ordinal))
                {
                    Log("Unchanged", filePath, EventType.Trace);
                }
                else
                {
                    Log("Updating", filePath, EventType.Trace);
                    WriteFile(filePath, content);
                }
            }
            else
            {
                Log("Creating", filePath, EventType.Info);
                _filesUtility.SafeCreateDirectory(Path.GetDirectoryName(filePath));
                WriteFile(filePath, content);
            }
        }

        private string ErrorOnUpdate(string filePath, string oldValue)
        {
            var sameFiles = _files.Keys.Where(oldFilePath => string.Equals(filePath, oldFilePath, StringComparison.OrdinalIgnoreCase)).ToList(); // Robust error reporting, even though only one file is expected.
            string oldFileInfo = (sameFiles.Count == 1 && string.Equals(sameFiles.Single(), filePath, StringComparison.Ordinal))
                ? ""
                : $" Previously generated file is '{string.Join(", ", sameFiles)}'.";

            throw new FrameworkException($"Multiple code generators are writing the same file '{filePath}'.{oldFileInfo}");
        }

        private void WriteFile(string filePath, string content)
        {
            _filesUtility.WriteAllText(filePath, content, readonlyFile: true);
        }

        public void CleanUp()
        {
            var deleteFiles = Directory.GetFiles(_buildEnvironment.GeneratedSourceFolder, "*", SearchOption.AllDirectories)
                .Except(_files.Keys);

            foreach (var deleteFile in deleteFiles)
            {
                Log("Deleting", deleteFile, EventType.Info);
                _filesUtility.SafeDeleteFile(deleteFile);
            }
        }

        private void Log(string title, string filePath, EventType eventType)
        {
            _logger.Write(eventType, () => $"{title} '{FilesUtility.AbsoluteToRelativePath(_buildEnvironment.GeneratedSourceFolder, filePath)}'.");
        }
    }
}
