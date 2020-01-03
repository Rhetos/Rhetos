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

using Rhetos.Utilities;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Rhetos.Compiler
{
    internal class CacheUtility
    {
        private readonly BuildOptions _buildOptions;
        private readonly FilesUtility _filesUtility;
        private readonly SHA1 _sha1;
        private readonly string _cacheDirectory;

        public CacheUtility(Type generatorType, BuildOptions buildOptions, FilesUtility filesUtility)
        {
            _buildOptions = buildOptions;
            _filesUtility = filesUtility;
            _sha1 = new SHA1CryptoServiceProvider();
            _cacheDirectory = Path.Combine(_buildOptions.CacheFolder, generatorType.Name);
        }

        private string GetHashFile(string sourceFile) => Path.Combine(_cacheDirectory, Path.GetFileName(sourceFile) + ".hash");

        public byte[] ComputeHash(string sourceContent) => _sha1.ComputeHash(Encoding.UTF8.GetBytes(sourceContent));

        public void SaveHash(string sourceFile, byte[] hash)
        {
            CreateCacheDirectoryIfNotExist();
            File.WriteAllText(GetHashFile(sourceFile), CsUtility.ByteArrayToHex(hash), Encoding.ASCII);
        }

        public byte[] LoadHash(string sourceFile)
        {
            return File.Exists(GetHashFile(sourceFile)) ? CsUtility.HexToByteArray(File.ReadAllText(GetHashFile(sourceFile), Encoding.ASCII)) : new byte[] { };
        }

        public void MoveToCache(string file)
        {
            CreateCacheDirectoryIfNotExist();
            _filesUtility.SafeCopyFile(file, GetCachedFile(file), true);
        }

        public bool MoveFromCache(string file)
        {
            if (!File.Exists(GetCachedFile(file)))
                return false;

            _filesUtility.SafeCopyFile(GetCachedFile(file), file);

            return true;
        }

        private string GetCachedFile(string file)
        {
            //TODO: We need to decide if the Paths.GeneratedFolder (for generated assemblies) will be mapped to a folder in the BuildOptions class or something else
            var relativePathToGeneratedFolder = FilesUtility.AbsoluteToRelativePath(Paths.GeneratedFolder, file);
            return Path.Combine(_cacheDirectory, relativePathToGeneratedFolder);
        }

        private void CreateCacheDirectoryIfNotExist()
        {
            if (!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);
        }
    }
}
