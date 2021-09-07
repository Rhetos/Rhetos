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

using Rhetos;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    /// <summary>
    /// Copies the given files from source to destination and deletes all other old files from the destination folder.
    /// </summary>
    public class FileSyncer
    {
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly FilesUtility _filesUtility;

        public FileSyncer(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _filesUtility = new FilesUtility(logProvider);
        }

        private class CopyFile { public string File; public string Target; }

        private readonly MultiDictionary<string, CopyFile> _filesByDestination = new MultiDictionary<string, CopyFile>();

        public void AddFolderContent(string sourceFolder, string destinationFolder, bool recursive)
        {
            AddFolderContent(sourceFolder, destinationFolder, ".", recursive);
        }

        /// <summary>
        /// The destinationSubfolder parameter is separated from destinationFolder because
        /// all obsolete files in the destinationFolder must be deleted.
        /// </summary>
        public void AddFolderContent(string sourceFolder, string destinationFolder, string destinationSubfolder, bool recursive)
        {
            sourceFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourceFolder));
            destinationFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destinationFolder));

            string invalidFolder = _filesByDestination.Keys.FirstOrDefault(existingDestination => existingDestination != destinationFolder
                && (existingDestination.StartsWith(destinationFolder) || destinationFolder.StartsWith(existingDestination)));
            if (invalidFolder != null)
                throw new ArgumentException(GetType().Name + " cannot be used on two destination folders where one contains another: \""
                    + invalidFolder + "\" and \"" + destinationFolder + "\".");

            if (Directory.Exists(sourceFolder))
                foreach (var file in Directory.GetFiles(sourceFolder, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                    _filesByDestination.Add(destinationFolder, new CopyFile
                    {
                        File = file,
                        Target = Path.Combine(destinationFolder, destinationSubfolder, file.Substring(sourceFolder.Length + 1))
                    });
            else
                _filesByDestination.AddKey(destinationFolder);
        }

        /// <summary>
        /// The destinationFolder parameter is separated from destinationFile because
        /// all obsolete files in the destinationFolder must be deleted.
        /// </summary>
        /// <param name="destinationFile">If null, the original file name is used.</param>
        public void AddFile(string file, string destinationFolder, string destinationFile = null)
        {
            destinationFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destinationFolder));
            destinationFile = destinationFile ?? Path.GetFileName(file);
            destinationFile = Path.Combine(destinationFolder, destinationFile);
            destinationFile = Path.GetFullPath(destinationFile);
            file = Path.GetFullPath(file);

            _filesByDestination.Add(destinationFolder, new CopyFile { File = file, Target = destinationFile });
        }

        public void AddDestinations(params string[] destinationFolders)
        {
            foreach (string destinationFolder in destinationFolders)
                _filesByDestination.AddKey(destinationFolder);
        }

        /// <param name="deleteSource">Deleting the source files might improve performance because it moves the files instead of copying them.</param>
        public void UpdateDestination(bool deleteSource = false, bool emptyDestinationFolder = true)
        {
            var sw = Stopwatch.StartNew();

            int countDestination = 0;
            int countMoved = 0;
            int countCopied = 0;
            int countDeleted = 0;

            var ignoreFiles = CheckSourceForDuplicates();

            foreach (var destination in _filesByDestination)
            {
                if(emptyDestinationFolder)
                    _filesUtility.EmptyDirectory(destination.Key);
                countDestination++;

                foreach (var copyFile in destination.Value)
                    if (!ignoreFiles.Contains(copyFile.File))
                    {
                        if (deleteSource)
                        {
                            _filesUtility.SafeMoveFile(copyFile.File, copyFile.Target);
                            countMoved++;
                        }
                        else
                        {
                            _filesUtility.SafeCopyFile(copyFile.File, copyFile.Target);
                            countCopied++;
                        }
                    }
                    else
                    {
                        if (deleteSource)
                        {
                            _filesUtility.SafeDeleteFile(copyFile.File);
                            countDeleted++;
                        }
                    }
            }
            _filesByDestination.Clear();

            var report = new StringBuilder(100);
            report.Append("UpdateDestination ").Append(countDestination).Append(" destinations");
            if (countMoved > 0)
                report.Append($", {countMoved} moved");
            if (countCopied > 0)
                report.Append($", {countCopied} copied");
            if (countDeleted > 0)
                report.Append($", {countDeleted} deleted");
            report.Append('.');

            _performanceLogger.Write(sw, () => report.ToString());
        }

        private HashSet<string> CheckSourceForDuplicates()
        {
            Dictionary<string, List<string>> duplicatesByTarget =_filesByDestination
                .SelectMany(destination => destination.Value)
                .GroupBy(copyFile => Path.GetFullPath(copyFile.Target).ToLower())
                .Where(group => group.Count() > 1)
                .ToDictionary(group => group.First().Target, group => group.Select(copyFile => copyFile.File).ToList());

            var ignoreFiles = new HashSet<string>();
            foreach (var group in duplicatesByTarget)
            {
                var newestInGroup = new FileInfo(group.Value.First());
                foreach (var otherFile in group.Value.Skip(1).Select(path => new FileInfo(path)))
                {
                    if (newestInGroup.Length != otherFile.Length)
                        _logger.Error("Conflicting source files with different file size: '{0}' and '{1}'.", newestInGroup.FullName, otherFile.FullName);
                    else if (newestInGroup.LastWriteTime != otherFile.LastWriteTime)
                        _logger.Warning("Duplicate source files with different modification time: '{0}' and '{1}'.", newestInGroup.FullName, otherFile.FullName);
                    else
                        _logger.Info("Duplicate source files: '{0}' and '{1}'.", newestInGroup.FullName, otherFile.FullName);

                    if (otherFile.LastWriteTime > newestInGroup.LastWriteTime
                        || otherFile.LastWriteTime == newestInGroup.LastWriteTime && otherFile.Length > newestInGroup.Length)
                        newestInGroup = otherFile;
                }

                foreach (string file in group.Value)
                    if (file != newestInGroup.FullName)
                        ignoreFiles.Add(file);
            }

            return ignoreFiles;
        }
    }
}
