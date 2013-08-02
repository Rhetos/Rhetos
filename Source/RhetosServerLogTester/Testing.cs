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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Rhetos;
using Rhetos.Utilities;
using Rhetos.TestCommon;

namespace RhetosServerLogTester
{
    public sealed class Testing
    {
        public Testing(string fileName)
        {
            FileName = fileName;
            XDocument = XDocument.Load(FileName);
        }

        public string FileName { get; private set; }

        public static string SuffixFileNameWith(string fileName, string suffix)
        {
            var dir = Path.GetDirectoryName(fileName);
            return Path.ChangeExtension(
                Path.Combine(String.IsNullOrEmpty(dir) ? "" : dir, Path.GetFileNameWithoutExtension(fileName) + suffix), 
                Path.GetExtension(fileName));
        }

        public XDocument XDocument { get; private set; }

        public List<ServerCommandInfo[]> Commands;
        public List<ServerProcessingResult> Results;

        private static LogEntry DeserializeLogEntry(string xml)
        {
            using (var sr = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(sr))
            using (var xmlDict = XmlDictionaryReader.CreateDictionaryReader(xmlReader))
            {
                var serializer = new DataContractSerializer(typeof(LogEntry), new[] { typeof(ServerCommandInfo[]), typeof(ServerProcessingResult) });
                return serializer.ReadObject(xmlDict, false) as LogEntry;
            }
        }

        public void ExtractCommandsAndResults()
        {
            if (Commands != null || Results != null)
                return;

            // There are two log formats ..
            var logEntries = XDocument.Descendants()
                .Where(el => el.Name.LocalName == typeof (LogEntry).Name)
                .Select(el => DeserializeLogEntry(el.ToString())).ToList();
            if (logEntries.Any())
            // RhetosClient creates LogEntry nodes with ServerCallID to match commands and responses ..
            {
                var commandLogEntries = logEntries.Where(e => e.Entry is ServerCommandInfo[]);
                Commands = commandLogEntries.Select(e => e.Entry as ServerCommandInfo[]).ToList();

                var resultLogEntries = logEntries.Where(e => e.Entry is ServerProcessingResult);
                Results = commandLogEntries.Select(ce => resultLogEntries.Single(re => re.ServerCallID == ce.ServerCallID).Entry as ServerProcessingResult).ToList();

                if (Commands.Count + Results.Count != logEntries.Count)
                    LogCorrupted(Commands.Count, logEntries.Count - Results.Count);
            }
            else
            // RhetosServerLogTester creates cmd[]/response pairs, in correct order ..
            {
                Commands = XDocument.Descendants()
                    .Where(el => el.Name.LocalName == "ArrayOf" + typeof(ServerCommandInfo).Name)
                    .Select(el => XmlUtility.DeserializeArrayFromXml<ServerCommandInfo>(el.ToString())).ToList();
                Results = XDocument.Descendants()
                    .Where(el => el.Name.LocalName == typeof (ServerProcessingResult).Name)
                    .Select(el => XmlUtility.DeserializeFromXml<ServerProcessingResult>(el.ToString())).ToList();
                
                if (Results.Count != Commands.Count)
                    LogCorrupted(Results.Count, Commands.Count);
            }

            if (Results.Count == 0)
                throw new InvalidOperationException("Nothing to do: test (log) file empty.");
        }

        private static void LogCorrupted(int commandsCount, int resultCount)
        {
            throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                "Test (server log) file corrupted: number of commands doesn't match number of results ({0} vs {1})",
                commandsCount, resultCount));
        }

        internal string RunTest(IServerApplication serverApplication, TextWriter textWriter)
        {
            ExtractCommandsAndResults();

            textWriter.Write("Executing command sets ({0}): ", Commands.Count);
            var newXDocument = new XDocument(new XElement("Log"));
            var root = newXDocument.Root;
            if (root != null)
                foreach (var cmd in Commands)
                {
                    root.Add(XElement.Parse(XmlUtility.SerializeArrayToXml(cmd)));
                    textWriter.Write(".");
                    root.Add(XElement.Parse(XmlUtility.SerializeToXml(serverApplication.Execute(cmd))));
                }

            var newFileName = SuffixFileNameWith(FileName, "_New");
            newXDocument.Save(newFileName);

            return newFileName;
        }

        public string ProcessAndSave()
        {
            Process();
            
            // Save processed xml ..
            var processedFileName = SuffixFileNameWith(FileName, "_Processed");
            XDocument.Save(processedFileName);
            return processedFileName;
        }

        public void Process()
        {
            ExtractCommandsAndResults();

            // Sort server commands and responses ..
            XDocument = new XDocument(new XElement("Log"));
            var root = XDocument.Root;
            if (root != null)
                for (var i = 0; i < Commands.Count; i++)
                {
                    root.Add(XElement.Parse(XmlUtility.SerializeArrayToXml(Commands[i])));
                    root.Add(XElement.Parse(XmlUtility.SerializeToXml(Results[i])));
                }

            // Remove comments ..
            XDocument.DescendantNodes().Where(node => node.NodeType == XmlNodeType.Comment).Remove();

            // Convert (.doc) reports to txt ..
            foreach (var reportData in XDocument.Descendants()
                .Where(node => node.Name.LocalName == typeof(ServerCommandResult).Name)
                .Where(cmdRes => cmdRes.Descendants().Any(desc => desc.Name.LocalName == "Message" && desc.Value == "Report generated"))
                .SelectMany(cmdResRep => cmdResRep.Descendants()).Where(desc => desc.Name.LocalName == "Data"))
            {
                reportData.Value = ConvertDocx(reportData.Value);
            }

            // Remove DslScript ..
            ProcessRemoveDslScript(XDocument);
        }

        private static void ProcessRemoveDslScript(XContainer xContainer)
        {
            foreach (var dslScriptData in xContainer.Descendants()
                .Where(node => node.Name.LocalName == typeof(ServerCommandResult).Name)
                .Where(cmdRes => cmdRes.Descendants().Any(desc => desc.Name.LocalName == "Message" && desc.Value == "Model loaded"))
                .SelectMany(cmdResRep => cmdResRep.Descendants()).Where(desc => desc.Name.LocalName == "Data"))
            {
                dslScriptData.Value = "<DSL script removed by RhetosServerLogTester>";
            }
        }

        private static string ConvertDocx(string data)
        {
            var binary = XmlUtility.DeserializeFromXml<byte[]>(data);
            var txt = TestUtility.TextFromDocx(binary);
            return txt;
        }
    }
}
