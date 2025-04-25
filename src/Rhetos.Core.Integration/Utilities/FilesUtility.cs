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
using System.Runtime.InteropServices;
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

        private static readonly StringComparison _pathComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        private static readonly StringComparer _pathComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        public StringComparison PathComparison => _pathComparison;

        public StringComparer PathComparer => _pathComparer;

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

        /// <summary>
        /// Creates the target directory if required. Retries if copying fails.
        /// If the destination file exists, copying will fail. See <see cref="SafeCopyFile(string, string, bool)"/> for the overwrite option.
        /// </summary>
        public void SafeCopyFile(string sourceFile, string destinationFile)
        {
            SafeCopyFile(sourceFile, destinationFile, false);
        }

        /// <summary>
        /// Creates the target directory if required. Retries if copying fails.
        /// </summary>
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
                return Array.Empty<string>();
        }

        /// <summary>
        /// Reads the file with UTF-8 encoding.
        /// If invalid characters are detected, it will show a warning and read the file again with the system's default local codepage.
        /// </summary>
        /// <remarks>
        /// This method is intended to warn developers for non-English text files that are accidentally saved
        /// in ANSI encoding (with default local codepage) instead of UTF-8.
        /// Such file will be loaded correctly, but warning is displayed because the application's behavior could change
        /// if the same operation is executed on another machine with different default codepage.
        /// </remarks>
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

        /// <summary>
        /// Writes the text file with UTF-8 encoding.
        /// This method writes the file in the way to minimize the burden on the source file monitoring services such as Visual Studio.
        /// </summary>
        /// <param name="readonlyFile">Creates the file with the read-only attribute. Allows overwriting the existing read-only file.</param>
        public void WriteAllText(string path, string content, bool writeOnlyIfModified = false, bool readonlyFile = false)
        {
            bool newFile;
            if (writeOnlyIfModified)
            {
                if (File.Exists(path))
                {
                    if (File.ReadAllText(path, Encoding.UTF8).Equals(content, StringComparison.Ordinal))
                    {
                        _logger.Trace(() => $"WriteAllText: Unchanged '{path}'.");
                        return;
                    }
                    else
                    {
                        _logger.Trace(() => $"WriteAllText: Updating '{path}'.");
                        newFile = false;
                    }
                }
                else
                {
                    _logger.Trace(() => $"WriteAllText: Creating '{path}'.");
                    newFile = true;
                }
            }
            else
            {
                _logger.Trace(() => $"WriteAllText: Writing '{path}'.");
                newFile = true;
            }

            if (newFile)
                SafeCreateDirectory(Path.GetDirectoryName(path));
            WriteAllText(path, content, readonlyFile);
        }

        private void WriteAllText(string path, string content, bool readonlyFile)
        {
            FileAttributes? attributes = null;
            if (readonlyFile)
            {
                // Remove read-only attribute to allow write.
                attributes = File.Exists(path) ? File.GetAttributes(path) : null;
                if (attributes != null && (attributes.Value & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    File.SetAttributes(path, attributes.Value & ~FileAttributes.ReadOnly);
            }

            // This method tries to keep and update an existing file instead of deleting it and creating a new one,
            // in order to lessen the effect to any file monitoring service such as Visual Studio.
            // The previous version of this method, that always created new source files, caused instability in Visual Studio while the generated project was open.
            using (var fs = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var sw = new StreamWriter(fs, Encoding.UTF8))
                {
                    sw.Write(content);
                    fs.SetLength(fs.Position); // Truncates rest of the file, if the previous file version was larger.
                }
            }

            if (readonlyFile)
            {
                // The generated files are marked as read-only, as a hint that they are not indended to be manually edited.
                attributes ??= File.GetAttributes(path);
                File.SetAttributes(path, attributes.Value | FileAttributes.ReadOnly);
            }
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
            var baseParts = Path.GetFullPath(baseFolder).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var targetParts = Path.GetFullPath(target).Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            int common = 0;
            while (common < baseParts.Length && common < targetParts.Length
                && string.Equals(baseParts[common], targetParts[common], _pathComparison))
                common++;

            if (common == 0)
                return target;

            var resultParts = Enumerable.Repeat(@"..", baseParts.Length - common)
                .Concat(targetParts.Skip(common))
                .ToArray();

            var resultPath = Path.Combine(resultParts);
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
                // Since .NET 6, File.SetLastWriteTime sets the time, even of the file is read-only. Keeping the old code to support .NET 5. See https://docs.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/6.0/set-timestamp-readonly-file
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
                _pathComparison);
        }

        public static bool IsInsideDirectory(string child, string parent)
        {
            return Path.GetFullPath(Path.Combine(child, "."))
                .StartsWith(Path.GetFullPath(Path.Combine(parent, ".")), _pathComparison);
        }
    }
}
