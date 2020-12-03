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
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Utilities
{
    public class FilesUtility
    {
        private readonly ILogger _logger;

        public FilesUtility(ILogProvider logProvider)
        {
            _logger = logProvider.GetLogger(GetType().Name);
        }

        private void Retry(Action action, Func<string> actionName)
        {
            const int maxTries = 10;
            for (int tries = maxTries; tries > 0; tries--)
            {
                try
                {
                    action();
                    break;
                }
                catch
                {
                    if (tries <= 1)
                        throw;

                    if (tries == maxTries) // First retries are very common on some environments.
                        _logger.Trace(() => $"Waiting to {actionName.Invoke()}.");
                    if (tries == maxTries - 1) // Second retry is often result of a locked file.
                        _logger.Warning(() => $"Waiting to {actionName.Invoke()}.");

                    System.Threading.Thread.Sleep(500);
                }
            }
        }

        public void SafeCreateDirectory(string path)
        {
            try
            {
                // When using TortoiseHg and the Rhetos folder is opened in Windows Explorer,
                // Directory.CreateDirectory() will stochastically fail with UnauthorizedAccessException or DirectoryNotFoundException.
                Retry(() => Directory.CreateDirectory(path), () => "create directory " + path);
            }
            catch (Exception ex)
            {
                throw new FrameworkException($"Can't create directory '{path}'. Check that it's not locked.", ex);
            }
        }

        public void SafeDeleteDirectory(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    return;

                File.SetAttributes(path, FileAttributes.Normal);

                foreach (var dir in Directory.GetDirectories(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(dir, FileAttributes.Normal);

                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    File.SetAttributes(file, FileAttributes.Normal);

                Retry(() => Directory.Delete(path, true), () => "delete directory " + path);

                Retry(() => { if (Directory.Exists(path)) throw new FrameworkException("Failed to delete directory " + path); }, () => "check if directory deleted " + path);
            }
            catch (Exception ex)
            {
                throw new FrameworkException($"Can't delete directory '{path}'. Check that it's not locked.", ex);
            }
        }

        /// <summary>
        /// Creates the directory if it doesn't exists and deletes its content.
        /// This method will not delete the directory and create a new one; the existing directory is kept, in order to reduce locking issues if the folder is opened in command prompt or other application.
        /// </summary>
        public void EmptyDirectory(string path)
        {
            SafeCreateDirectory(path);

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly))
                SafeDeleteFile(file);
            foreach (var folder in Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly))
                SafeDeleteDirectory(folder);
        }

        public void SafeMoveFile(string source, string destination)
        {
            try
            {
                SafeCreateDirectory(Path.GetDirectoryName(destination)); // Less problems with locked folders if the directory is created before moving the file. Locking may occur with different file-monitoring utilities or if the folder is opened in Windows Explorer.
                Retry(() => File.Move(source, destination), () => "move file " + source);
            }
            catch (Exception ex)
            {
                throw new FrameworkException($"Can't move file '{source}' to '{destination}'. Check that destination file or folder is not locked.", ex);
            }
        }

        public void SafeCopyFile(string sourceFile, string destinationFile)
        {
            SafeCopyFile(sourceFile, destinationFile, false);
        }

        public void SafeCopyFile(string sourceFile, string destinationFile, bool overwrite)
        {
            try
            {
                SafeCreateDirectory(Path.GetDirectoryName(destinationFile));
                Retry(() => File.Copy(sourceFile, destinationFile, overwrite), () => "copy file " + sourceFile);
            }
            catch (Exception ex)
            {
                throw new FrameworkException($"Can't copy file '{sourceFile}' to '{destinationFile}'. Check that destination folder is not locked.", ex);
            }
        }

        public string SafeCopyFileToFolder(string sourceFile, string destinationFolder)
        {
            string destinationFile = Path.Combine(destinationFolder, Path.GetFileName(sourceFile));
            SafeCopyFile(sourceFile, destinationFile);
            return destinationFile;
        }

        public void SafeDeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.SetAttributes(path, FileAttributes.Normal);
                Retry(() => File.Delete(path), () => "delete file " + path);
            }
        }

        public string[] SafeGetFiles(string directory, string pattern, SearchOption searchOption)
        {
            if (Directory.Exists(directory))
                return Directory.GetFiles(directory, pattern, searchOption);
            else
                return new string[] { };
        }

        public string ReadAllText(string path)
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            //Occurrence of the character � is interpreted as invalid UTF-8
            var invalidCharIndex = text.IndexOf((char)65533);
            if (invalidCharIndex != -1)
            {
                bool tryDefault = !Encoding.Default.Equals(Encoding.UTF8);

                _logger.Warning($"Warning: File '{path}' contains invalid UTF-8 character at line {ScriptPositionReporting.Line(text, invalidCharIndex)}." +
                    (tryDefault ? $" Reading with default system encoding instead." : "") +
                    $" Save the text file as UTF-8.");

                if (tryDefault)
                    text = File.ReadAllText(path, CodePagesEncodingProvider.Instance.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage));
            }
            return text;
        }

        public static string RelativeToAbsolutePath(string baseFolder, string path)
        {
            if (path == null)
                return null;
            return Path.GetFullPath(Path.Combine(baseFolder, path));
        }

        public static string AbsoluteToRelativePath(string baseFolder, string target)
        {
            if (target == null)
                return null;
            var baseParts = Path.GetFullPath(baseFolder).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var targetParts = Path.GetFullPath(target).Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            int common = 0;
            while (common < baseParts.Length && common < targetParts.Length
                && string.Equals(baseParts[common], targetParts[common], StringComparison.OrdinalIgnoreCase))
                common++;

            if (common == 0)
                return target;

            var resultParts = Enumerable.Repeat(@"..", baseParts.Length - common)
                .Concat(targetParts.Skip(common));

            var resultPath = string.Join(@"\", resultParts);
            if (resultPath != "")
                return resultPath;
            else
                return ".";
        }

        public static bool SafeTouch(string path)
        {
            var file = new FileInfo(path);
            if (file.Exists)
            {
                var isReadOnly = file.IsReadOnly;
                file.IsReadOnly = false;
                file.LastWriteTime = DateTime.Now;
                file.IsReadOnly = isReadOnly;
                return true;
            }
            else
                return false;
        }

        public static bool IsSameDirectory(string path1, string path2)
        {
            return string.Equals(
                Path.GetFullPath(Path.Combine(path1, ".")),
                Path.GetFullPath(Path.Combine(path2, ".")),
                StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsInsideDirectory(string child, string parent)
        {
            return Path.GetFullPath(Path.Combine(child, "."))
                .StartsWith(Path.GetFullPath(Path.Combine(parent, ".")),
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
