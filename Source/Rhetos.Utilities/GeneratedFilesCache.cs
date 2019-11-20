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
        private readonly FilesUtility _filesUtility;
        private readonly FileSyncer _syncer;
        private readonly SHA1 _sha1;

        public GeneratedFilesCache(ILogProvider logProvider)
        {
            _filesUtility = new FilesUtility(logProvider);
            _syncer = new FileSyncer(logProvider);
            _sha1 = new SHA1CryptoServiceProvider();
        }

        /// <summary>
        /// Moves only the successfully generated files to the cache folder, otherwise keeps the old cached files.
        /// Deletes all generated files.
        /// </summary>
        public void MoveGeneratedFilesToCache()
        {
            var generatedFiles = _filesUtility.SafeGetFiles(Paths.GeneratedFolder, "*", SearchOption.AllDirectories)
                .Select(x => FilesUtility.AbsoluteToRelativePath(Paths.GeneratedFolder, x));

            foreach (string generatedFile in generatedFiles)
                _syncer.AddFile(Path.Combine(Paths.GeneratedFolder, generatedFile), Paths.GeneratedFilesCacheFolder);
            _syncer.UpdateDestination(deleteSource: true);

            foreach (string fileToDelete in generatedFiles)
                _filesUtility.SafeDeleteFile(Path.Combine(Paths.GeneratedFolder, fileToDelete));
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
            string hashFile = sampleSourceFile + ".hash";
            File.WriteAllText(hashFile, CsUtility.ByteArrayToHex(hash), Encoding.ASCII);
        }

        /// <summary>
        /// Copies the files from cache folder into the generated folder
        /// </summary>
        /// <param name="files">Any file from the cached file group, extension will be ignored.</param>
        /// <returns>List of the restored files, if the files are copied from the cache.</returns>
        public List<string> RestoreCachedFiles(params string[] files)
        {
            var resoredFiles = new List<string>();
            foreach (var path in files)
            {
                var fileTorestore = Path.Combine(Paths.GeneratedFilesCacheFolder, FilesUtility.AbsoluteToRelativePath(Paths.GeneratedFolder, path));
                if (File.Exists(fileTorestore))
                {
                    var destinationPath = Path.Combine(Paths.GeneratedFolder, path);
                    File.Move(fileTorestore, destinationPath);
                    resoredFiles.Add(path);
                }
            }
            return resoredFiles;
        }

        public byte[] GetHashForCachedFile(string file)
        {
            var cachedHashFile = Path.Combine(Paths.GeneratedFilesCacheFolder, FilesUtility.AbsoluteToRelativePath(Paths.GeneratedFolder, file)) + ".hash";
            if (File.Exists(cachedHashFile))
                return CsUtility.HexToByteArray(File.ReadAllText(cachedHashFile, Encoding.ASCII));
            else
                return new byte[0];
        }
    }
}
