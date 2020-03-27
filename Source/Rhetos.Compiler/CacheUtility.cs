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
        private readonly FilesUtility _filesUtility;
        private readonly SHA256 _sha256;
        private readonly string _cacheDirectory;

        public CacheUtility(Type generatorType, BuildOptions buildOptions, FilesUtility filesUtility)
        {
            _filesUtility = filesUtility;
            _sha256 = SHA256.Create();
            _cacheDirectory = Path.Combine(buildOptions.CacheFolder, generatorType.Name);
        }

        private string GetHashFile(string sourceFile) => Path.Combine(_cacheDirectory, Path.GetFileName(sourceFile) + ".hash");

        public byte[] ComputeHash(string sourceContent) => _sha256.ComputeHash(Encoding.UTF8.GetBytes(sourceContent));

        public void SaveHash(string sourceFile, byte[] hash)
        {
            CreateCacheDirectoryIfNotExist();
            File.WriteAllText(GetHashFile(sourceFile), CsUtility.ByteArrayToHex(hash), Encoding.ASCII);
        }

        public byte[] LoadHash(string sourceFile)
        {
            return File.Exists(GetHashFile(sourceFile)) ? CsUtility.HexToByteArray(File.ReadAllText(GetHashFile(sourceFile), Encoding.ASCII)) : new byte[] { };
        }

        public bool FileIsCached(string file)
        {
            if (!File.Exists(GetCachedFile(file)))
                return false;
            return true;
        }

        public void CopyToCache(string file)
        {
            CreateCacheDirectoryIfNotExist();
            _filesUtility.SafeCopyFile(file, GetCachedFile(file), true);
        }

        public void CopyFromCache(string file)
        {
            _filesUtility.SafeCopyFile(GetCachedFile(file), file);
        }

        private string GetCachedFile(string file) => Path.Combine(_cacheDirectory, Path.GetFileName(file));

        private void CreateCacheDirectoryIfNotExist()
        {
            if (!Directory.Exists(_cacheDirectory))
                Directory.CreateDirectory(_cacheDirectory);
        }
    }
}
