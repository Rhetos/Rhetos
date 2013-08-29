/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Ionic.Zip;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Utilities;

namespace Rhetos.TestCommon
{
    public static class TestUtility
    {
        public static Exception ShouldFail(Action action, string reason, params string[] expectedErrorContent)
        {
            Exception exception = null;
            string message = null;
            try
            {
                action();
            }
            catch (Exception ex)
            {
                exception = ex;
                message = ex.Message;
                if (ex is UserException && ((UserException)ex).SystemMessage != null)
                    message += "\r\n    SystemMessage: " + ((UserException)ex).SystemMessage;

                Console.WriteLine("[" + ex.GetType().Name + "] " + message);
            }
            Assert.IsNotNull(exception, "Exception did not happen. " + reason);
            AssertContains(message, expectedErrorContent, "Exception message does not contain the pattern. " + reason);
            return exception;
        }

        /// <summary>
        /// Case-insensitive comparison.
        /// </summary>
        public static void AssertContains(string text, string[] patterns, string message = null)
        {
            Console.WriteLine("[AssertContains] Text: '" + text + "'.");

            foreach (var pattern in patterns)
            {
                Console.Write("[AssertContains] Looking for pattern '" + pattern + "'.");

                if (text.ToLower().Contains(pattern.ToLower()))
                    Console.WriteLine(" Found.");
                else
                {
                    Console.WriteLine(" Not found.");
                    Assert.Fail("Text should contain pattern '" + pattern + "'. " + message ?? "");
                }
            }
        }

        /// <summary>
        /// Case-insensitive comparison.
        /// </summary>
        public static void AssertContains(string text, string pattern, string message = null)
        {
            AssertContains(text, new[] { pattern }, message);
        }

        /// <summary>
        /// Case-insensitive comparison.
        /// </summary>
        public static void AssertNotContains(string text, string[] patterns, string message = null)
        {
            Console.WriteLine("[AssertContains] Text: '" + text + "'.");

            foreach (var pattern in patterns)
            {
                Console.Write("[AssertContains] Looking for pattern '" + pattern + "'.");

                if (text.ToLower().Contains(pattern.ToLower()))
                {
                    Console.WriteLine(" Found.");
                    Assert.Fail("Text should not contain pattern '" + pattern + "'. " + message ?? "");
                }
                else
                    Console.WriteLine(" Not found.");
            }
        }

        /// <summary>
        /// Case-insensitive comparison.
        /// </summary>
        public static void AssertNotContains(string text, string pattern, string message = null)
        {
            Console.WriteLine("[AssertNotContains] Not expecting pattern '" + pattern + "' in text '" + text + "'.");

            if (text.ToLower().Contains(pattern.ToLower()))
                Assert.Fail("Text should not contain pattern '" + pattern + "'. " + message ?? "");
        }

        public static string DumpSorted<T>(IEnumerable<T> list, Func<T, object> selector = null)
        {
            if (selector == null)
                selector = item => item;
            var result = string.Join(", ", list.Select(element => selector((T)element).ToString()).OrderBy(text => text));
            Console.WriteLine("[DumpSorted] " + result);
            return result;
        }

        public static string Dump<T>(IEnumerable<T> list, Func<T, object> selector = null)
        {
            if (selector == null)
                selector = item => item;
            var result = string.Join(", ", list.Select(element => selector((T)element).ToString()));
            Console.WriteLine("[Dump] " + result);
            return result;
        }

        public static string TextFromDocx(byte[] file)
        {
            using (var reader = new MemoryStream(file))
                using (ZipFile zip = ZipFile.Read(reader))
                    return TextFromDocxReadZip(zip);
        }

        public static string TextFromDocx(string fileName)
        {
            using (ZipFile zip = ZipFile.Read(fileName))
                return TextFromDocxReadZip(zip);
        }

        private static string TextFromDocxReadZip(ZipFile zip)
        {
            MemoryStream stream = new MemoryStream();
            zip.FlattenFoldersOnExtract = true;
            const string xmlFile = @"document.xml";
            if (File.Exists(xmlFile))
                File.Delete(xmlFile);
            zip.ExtractSelectedEntries(@"word/document.xml", ExtractExistingFileAction.OverwriteSilently);

            var xml = XElement.Load(xmlFile);
            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

            const string embedLineSeparator = "#embedLineSeparator#";
            foreach (var lineBreakNode in xml.Descendants(w + "br"))
                lineBreakNode.Value = embedLineSeparator;

            return string.Join("|", xml.Descendants(w + "p")
                                                        .Select(node => node.Value)
                                                        .Where(line => !string.IsNullOrWhiteSpace(line))
                                                        .SelectMany(line => line.Split(new[]{embedLineSeparator}, StringSplitOptions.RemoveEmptyEntries))
                                                        .Where(line => !string.IsNullOrWhiteSpace(line))
                                                        .Where(line => !line.Equals(@"Evaluation Warning : The document was created with Spire.Doc for .NET.")));
        }

        /// <summary>
        /// Unit test will be marked as "Inconclusive" if this function fails.
        /// </summary>
        public static void CheckDatabaseAvailability(string expectedLanguage = null)
        {
            const string connectionStringLocation = @"Enter the database connection in Rhetos\bin\ConnectionStrings.config, then rebuild this project.";
            try
            {
                Assert.IsNotNull(SqlUtility.ConnectionString);
            }
            catch (Exception ex)
            {
                Assert.Inconclusive(@"A live database is needed for this unit test to run. " + connectionStringLocation
                    + Environment.NewLine + ex.GetType().Name + ": " + ex.Message);
            }

            if (expectedLanguage != null && SqlUtility.DatabaseLanguage != expectedLanguage)
                Assert.Inconclusive("This test will run only on '" + expectedLanguage + "' database language, not '" + SqlUtility.DatabaseLanguage + "'. " + connectionStringLocation);
        }
    }
}
