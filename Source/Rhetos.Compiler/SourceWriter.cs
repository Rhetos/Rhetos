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
using Rhetos.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Rhetos.Compiler
{
    public class SourceWriter : ISourceWriter
    {
        private readonly ILogger _logger;
        private readonly RhetosBuildEnvironment _buildEnvironment;
        private readonly FilesUtility _filesUtility;
        private readonly ConcurrentDictionary<string, IEnumerable<string>> _files = new ConcurrentDictionary<string, IEnumerable<string>>(StringComparer.OrdinalIgnoreCase);

        public SourceWriter(RhetosBuildEnvironment buildEnvironment, ILogProvider logProvider, FilesUtility filesUtility)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _buildEnvironment = buildEnvironment;
            _filesUtility = filesUtility;
        }

        public void Add(string relativePath, IEnumerable<string> codeSegments)
        {
            string filePath = Path.GetFullPath(Path.Combine(_buildEnvironment.GeneratedSourceFolder, relativePath));

            if (!FilesUtility.IsInsideDirectory(filePath, _buildEnvironment.GeneratedSourceFolder))
                throw new FrameworkException($"Generated source file '{filePath}' should be inside the folder '{_buildEnvironment.GeneratedSourceFolder}'." +
                    $" Provide a simple file name or a relative path for {nameof(ISourceWriter)}.{nameof(ISourceWriter.Add)} method.");

            _files.AddOrUpdate(filePath, codeSegments, ErrorOnUpdate);

            if (File.Exists(filePath))
            {
                if (FilesUtility.IsContentEqual(filePath, codeSegments))
                {
                    Log("Unchanged", filePath, EventType.Trace);
                }
                else
                {
                    Log("Updating", filePath, EventType.Trace);
                    FilesUtility.WriteToFile(filePath, codeSegments);
                }
            }
            else
            {
                Log("Creating", filePath, EventType.Info);
                _filesUtility.SafeCreateDirectory(Path.GetDirectoryName(filePath));
                FilesUtility.WriteToFile(filePath, codeSegments);
            }
        }

        public void Add(string relativePath, string content)
        {
            Add(relativePath, new List<string> { content });
        }

        private IEnumerable<string> ErrorOnUpdate(string filePath, IEnumerable<string> oldValue)
        {
            var sameFiles = _files.Keys.Where(oldFilePath => string.Equals(filePath, oldFilePath, StringComparison.OrdinalIgnoreCase)); // Robust error reporting, even though only one file is expected.
            string oldFileInfo = (sameFiles.Count() == 1 && string.Equals(sameFiles.Single(), filePath, StringComparison.Ordinal))
                ? ""
                : $" Previously generated file is '{string.Join(", ", sameFiles)}'.";

            throw new FrameworkException($"Multiple code generators are writing the same file '{filePath}'.{oldFileInfo}");
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
