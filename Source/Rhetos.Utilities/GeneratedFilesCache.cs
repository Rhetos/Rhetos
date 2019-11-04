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
    public class GeneratedFilesCache
    {
        private readonly RhetosAppEnvironment _rhetosAppEnvironment;
        private readonly FilesUtility _filesUtility;
        private readonly FileSyncer _syncer;
        private readonly ILogger _logger;
        private readonly SHA1 _sha1;

        public GeneratedFilesCache(RhetosAppEnvironment rhetosAppEnvironment, ILogProvider logProvider)
        {
            _rhetosAppEnvironment = rhetosAppEnvironment;
            _filesUtility = new FilesUtility(logProvider);
            _syncer = new FileSyncer(logProvider);
            _logger = logProvider.GetLogger("FilesCache");
            _sha1 = new SHA1CryptoServiceProvider();
        }

        /// <summary>
        /// Moves only the successfully generated file groups to the cache folder, otherwise keeps the old cached files.
        /// Deletes all generated files.
        /// </summary>
        public void MoveGeneratedFilesToCache()
        {
            // Group files by name without extension:

            var generatedFiles = _filesUtility.SafeGetFiles(_rhetosAppEnvironment.GeneratedFolder, "*", SearchOption.AllDirectories)
                .GroupBy(file => Path.GetFileNameWithoutExtension(file))
                .ToDictionary(g => g.Key, g => g.ToList());

            var oldFiles = ListCachedFiles();

            // Move the successfully generated file groups to the cache, delete the others:

            var succesfullyGeneratedGroups = generatedFiles.Keys
                .Where(key => !oldFiles.ContainsKey(key) || generatedFiles[key].Count() >= oldFiles[key].Count())
                .ToList();

            foreach (string moveGroup in succesfullyGeneratedGroups)
                foreach (string moveFile in generatedFiles[moveGroup])
                    _syncer.AddFile(moveFile, Path.Combine(_rhetosAppEnvironment.GeneratedFilesCacheFolder, moveGroup));
            _syncer.UpdateDestination(deleteSource: true);

            foreach (string deleteGroup in generatedFiles.Keys.Except(succesfullyGeneratedGroups))
                foreach (string deleteFile in generatedFiles[deleteGroup])
                    _filesUtility.SafeDeleteFile(deleteFile);
        }

        /// <summary>
        /// Writes source file with encoding <see cref="Encoding.UTF8"/>.
        /// </summary>
        public byte[] SaveSourceAndHash(string sourceFile, string sourceCode)
        {
            File.WriteAllText(sourceFile, sourceCode, Encoding.UTF8);

            byte[] hash = GetHash(sourceCode);
            SaveHash(sourceFile, hash);
            return hash;
        }

        public byte[] GetHash(string sourceContent)
        {
            return _sha1.ComputeHash(Encoding.UTF8.GetBytes(sourceContent));
        }

        /// <param name="sampleSourceFile">Any file from the cached file group, extension will be ignored.</param>
        public void SaveHash(string sampleSourceFile, byte[] hash)
        {
            string hashFile = Path.GetFullPath(Path.ChangeExtension(sampleSourceFile, ".hash"));
            File.WriteAllText(hashFile, CsUtility.ByteArrayToHex(hash), Encoding.ASCII);
        }

        /// <param name="sampleSourceFile">Any file from the cached file group, extension will be ignored.</param>
        public byte[] LoadHash(string sampleSourceFile)
        {
            string hashFile = Path.GetFullPath(Path.ChangeExtension(sampleSourceFile, ".hash"));
            return CsUtility.HexToByteArray(File.ReadAllText(hashFile, Encoding.ASCII));
        }

        /// <summary>
        /// Copies the files from cache only if all of the extensions are found in the cache,
        /// and if the sourceContent matches the corresponding sourceFile in the cache.
        /// </summary>
        /// <param name="sampleSourceFile">Any file from the cached file group, extension will be ignored.</param>
        /// <returns>List of the restored files, if the files are copied from the cache, null otherwise.</returns>
        public List<string> RestoreCachedFiles(string sampleSourceFile, byte[] sourceHash, string targetFolder, IEnumerable<string> copyExtensions)
        {
            CsUtility.Materialize(ref copyExtensions);
            var cachedFiles = ListCachedFiles(Path.GetFileNameWithoutExtension(sampleSourceFile), sourceHash, copyExtensions);

            List<string> targetFiles;
            string report;

            if (!cachedFiles.IsError)
            {
                targetFiles = cachedFiles.Value.Select(source =>
                    _filesUtility.SafeCopyFileToFolder(source, targetFolder)).ToList();
                report = "Restored " + string.Join(", ", copyExtensions) + ".";
            }
            else
            {
                targetFiles = null;
                report = cachedFiles.Error;
            }

            _logger.Trace(() => "RestoreCachedFiles for " + Path.GetFileName(sampleSourceFile) + ": " + report);
            return targetFiles;
        }

        private Dictionary<string, List<string>> ListCachedFiles()
        {
            return _filesUtility.SafeGetFiles(_rhetosAppEnvironment.GeneratedFilesCacheFolder, "*", SearchOption.AllDirectories)
                .GroupBy(file => Path.GetFileName(Path.GetDirectoryName(file)))
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        public byte[] JoinHashes(IEnumerable<byte[]> hashes)
        {
            byte[] result = new byte[hashes.Max(h => h.Length)];
            foreach (var hash in hashes)
                for (int i = 0; i < hash.Length; i++)
                    result[i] ^= hash[i];
            return result;
        }

        private ValueOrError<List<string>> ListCachedFiles(string fileGroupName, byte[] sourceHash, IEnumerable<string> requestedExtensions)
        {
            var cachedFilesByExt = ListCachedFiles()
                .GetValueOrDefault(fileGroupName)
                ?.ToDictionary(file => Path.GetExtension(file));

            if (cachedFilesByExt == null)
                return ValueOrError.CreateError("File group not cached.");

            string cachedHashFile = cachedFilesByExt.GetValueOrDefault(".hash");
            if (cachedHashFile == null)
                return ValueOrError.CreateError("Missing hash file.");

            byte[] cachedHash = CsUtility.HexToByteArray(File.ReadAllText(cachedHashFile, Encoding.ASCII));
            if (cachedHash == null || cachedHash.Length == 0)
                return ValueOrError.CreateError("Missing hash value.");

            if (!sourceHash.SequenceEqual(cachedHash))
                return ValueOrError.CreateError("Different hash value.");

            var requestedFiles = new List<string>(requestedExtensions.Count());
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
