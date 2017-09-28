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
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Rhetos.Utilities
{
    public class FilesCache
    {
        private readonly FilesUtility _files;
        private readonly FileSyncer _syncer;
        private readonly ILogger _logger;
        private readonly SHA1 _sha1;

        public FilesCache(ILogProvider logProvider)
        {
            _files = new FilesUtility(logProvider);
            _syncer = new FileSyncer(logProvider);
            _logger = logProvider.GetLogger("FilesCache");
            _sha1 = new SHA1CryptoServiceProvider();
        }

        /// <summary>
        /// Deletes all of the source files.
        /// Move only the successfully generated file groups to the cache folder.
        /// </summary>
        public void MoveToCache(IEnumerable<string> sourceFiles)
        {
            // Group files by name without extension:

            var newFiles = sourceFiles
                .GroupBy(file => Path.GetFileNameWithoutExtension(file))
                .ToDictionary(g => g.Key, g => g.ToList());

            var oldFiles = ListCachedFiles();

            // Move the successfully generated file groups to the cache, delete the others:

            var succesfullyGeneratedGroups = newFiles.Keys
                .Where(key => !oldFiles.ContainsKey(key) || newFiles[key].Count() >= oldFiles[key].Count())
                .ToList();

            foreach (string moveGroup in succesfullyGeneratedGroups)
                foreach (string moveFile in newFiles[moveGroup])
                    _syncer.AddFile(moveFile, Path.Combine(Paths.GeneratedFilesCacheFolder, moveGroup));
            _syncer.UpdateDestination(deleteSource: true);

            foreach (string deleteGroup in newFiles.Keys.Except(succesfullyGeneratedGroups))
                foreach (string deleteFile in newFiles[deleteGroup])
                    _files.SafeDeleteFile(deleteFile);
        }

        public byte[] SaveSourceAndHash(string sourceFile, string sourceCode)
        {
            File.WriteAllText(sourceFile, sourceCode, Encoding.UTF8);

            string hashFile = Path.GetFullPath(Path.ChangeExtension(sourceFile, ".sha1"));
            byte[] hash = _sha1.ComputeHash(Encoding.UTF8.GetBytes(sourceCode));
            File.WriteAllText(hashFile, CsUtility.ByteArrayToHex(hash), Encoding.ASCII);

            return hash;
        }

        /// <summary>
        /// Copies the files from cache only if all of the extensions are found in the cache,
        /// and if the sourceContent matches the corresponding sourceFile in the cache.
        /// </summary>
        /// <returns>List of the restored files, if the files are copied from the cache, null otherwise.</returns>
        public List<string> RestoreCachedFiles(string sourceFile, byte[] sourceHash, string targetFolder, string[] copyExtensions)
        {
            var cachedFiles = ListCachedFiles(Path.GetFileNameWithoutExtension(sourceFile), sourceHash, copyExtensions);

            List<string> targetFiles;
            string report;

            if (!cachedFiles.IsError)
            {
                targetFiles = cachedFiles.Value.Select(source =>
                    _files.SafeCopyFileToFolder(source, targetFolder)).ToList();
                report = "Restored " + string.Join(", ", copyExtensions) + ".";
            }
            else
            {
                targetFiles = null;
                report = cachedFiles.Error;
            }

            _logger.Trace(() => "RestoreCachedFiles for " + Path.GetFileName(sourceFile) + ": " + report);
            return targetFiles;
        }

        private Dictionary<string, List<string>> ListCachedFiles()
        {
            return _files.SafeGetFiles(Paths.GeneratedFilesCacheFolder, "*", SearchOption.AllDirectories)
                .GroupBy(file => Path.GetFileName(Path.GetDirectoryName(file)))
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        private ValueOrError<List<string>> ListCachedFiles(string fileGroupName, byte[] sourceHash, string[] requestedExtensions)
        {
            var cachedFilesByExt = ListCachedFiles()
                .GetValueOrDefault(fileGroupName)
                ?.ToDictionary(file => Path.GetExtension(file));

            if (cachedFilesByExt == null)
                return ValueOrError.CreateError("File group not cached.");

            string cachedHashFile = cachedFilesByExt.GetValueOrDefault(".sha1");
            if (cachedHashFile == null)
                return ValueOrError.CreateError("Missing hash file.");

            byte[] cachedHash = CsUtility.HexToByteArray(File.ReadAllText(cachedHashFile, Encoding.Default));
            if (!sourceHash.SequenceEqual(cachedHash))
                return ValueOrError.CreateError("Different hash value.");

            var requestedFiles = new List<string>(requestedExtensions.Length);
            foreach (var extension in requestedExtensions)
            {
                string cachedFile = cachedFilesByExt.GetValueOrDefault(extension);
                if (cachedFile == null)
                    return ValueOrError.CreateError($"Extension '{extension}' not in cache.");
                requestedFiles.Add(cachedFile);
            }

            return requestedFiles;
        }
    }
}
