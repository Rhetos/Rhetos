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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Rhetos.Utilities;
using Rhetos.Logging;

namespace Rhetos.Deployment
{
    public class DataMigrationScript : IComparable<DataMigrationScript>
    {
        public string Tag;
        public string Path;
        public string Content;

        protected string _order;
        protected string Order
        {
            get { return _order ?? (_order = ComputeOrder(Path)); }
        }

        protected static readonly Regex Numerics = new Regex(@"(\d+|\D+)");

        protected static string ComputeOrder(string s)
        {
            return NumericString(s).Replace(@"\", @" \");
        }

        protected static string NumericString(string s)
        {
            var result = new StringBuilder();
            foreach (var match in Numerics.Matches(s))
            {
                var part = match.ToString();
                if (char.IsDigit(part[0]))
                    result.Append(new string('0', Math.Max(10 - part.Length, 0)) + part);
                else
                    result.Append(part);
            }
            return result.ToString();
        }

        public int CompareTo(DataMigrationScript other)
        {
            return string.Compare(Order, other.Order, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public class DataMigrationReport
    {
        public string Message;
        public List<string> CreatedTags;
        public override string ToString()
        {
            return Message;
        }
    }

    public class DataMigration
    {
        protected readonly ISqlExecuter _sqlExecuter;
        protected readonly ILogger _logger;

        public DataMigration(ISqlExecuter sqlExecuter, ILogProvider logProvider)
        {
            _sqlExecuter = sqlExecuter;
            _logger = logProvider.GetLogger("DataMigration");
        }

        public DataMigrationReport ExecuteDataMigrationScripts(string dataMigrationScriptsFolder)
        {
            var newScripts = LoadScriptsFromDisk(dataMigrationScriptsFolder);

            var scriptsInOtherLanguages = FindScriptsInOtherLanguages(newScripts, SqlUtility.DatabaseLanguage);
            LogScripts("Ignoring scripts in other database languages", scriptsInOtherLanguages);
            newScripts = newScripts.Except(scriptsInOtherLanguages).ToList();
            LogScripts("Script on disk", newScripts);

            var oldScripts = LoadScriptsFromDatabase();
            LogScripts("Script in database", oldScripts);

            var newIndex = new HashSet<string>(newScripts.Select(s => s.Tag));
            var oldIndex = new HashSet<string>(oldScripts.Select(s => s.Tag));

            List<DataMigrationScript> skipped = SkipOlderScriptsInEachFolder(oldIndex, newScripts);
            List<DataMigrationScript> toRemove = oldScripts.Where(os => !newIndex.Contains(os.Tag)).ToList();
            List<DataMigrationScript> toExecute = newScripts.Where(ns => !oldIndex.Contains(ns.Tag)).Except(skipped).ToList();
            LogScripts("Skipped older script", skipped, EventType.Info);
            LogScripts("Removed scripts", toRemove, EventType.Info);
            LogScripts("Executing script", toExecute, EventType.Info);

            ApplyToDatabase(toRemove, toExecute);

            string report = string.Format("Executed {0} of {1} scripts.", toExecute.Count, newScripts.Count);
            if (skipped.Count > 0)
                report = report + " " + skipped.Count + " older skipped.";
            return new DataMigrationReport { Message = report, CreatedTags = toExecute.Select(s => s.Tag).ToList() };
        }

        public void UndoDataMigrationScripts(List<string> createdTags)
        {
            _sqlExecuter.ExecuteSql(createdTags.Select(tag =>
                "DELETE FROM Rhetos.DataMigrationScript WHERE Tag = " + SqlUtility.QuoteText(tag)));
        }

        protected static readonly Regex ScriptLanguageRegex = new Regex(@"\((?<DatabaseLanguage>\w*)\).sql$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        protected static List<DataMigrationScript> FindScriptsInOtherLanguages(IEnumerable<DataMigrationScript> newScripts, string databaseLanguage)
        {
            return
                (from script in newScripts
                 let match = ScriptLanguageRegex.Match(script.Path)
                 where match.Success && match.Groups["DatabaseLanguage"].Value != databaseLanguage
                 select script).ToList();
        }

        protected void ApplyToDatabase(List<DataMigrationScript> toRemove, List<DataMigrationScript> toExecute)
        {
            UndoDataMigrationScripts(toRemove.Select(s => s.Tag).ToList());

            var sql = new List<string>();
            foreach (var script in toExecute)
            {
                sql.AddRange(script.Content
                                 .Split(new[] { "\r\nGO\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                                 .Where(c => !string.IsNullOrWhiteSpace(c)));
                sql.Add(SaveDataMigrationScriptEntiry(script));
            }
            _sqlExecuter.ExecuteSql(sql);
        }

        protected static string SaveDataMigrationScriptEntiry(DataMigrationScript script)
        {
            return string.Format(
                "INSERT INTO Rhetos.DataMigrationScript (Tag, Path, Content) VALUES ({0}, {1}, {2})",
                SqlUtility.QuoteText(script.Tag),
                SqlUtility.QuoteText(script.Path),
                SqlUtility.QuoteText(script.Content));
        }

        protected List<DataMigrationScript> SkipOlderScriptsInEachFolder(
            HashSet<string> oldIndex, List<DataMigrationScript> newScripts)
        {
            var newByFolder = newScripts
                .GroupBy(ns => GetFirstSubfolder(ns.Path))
                .ToDictionary(g => g.Key, g => g.ToList());

            var skipped = new List<DataMigrationScript>();
            foreach (var group in newByFolder)
            {
                var lastExecuted = group.Value
                    .Where(newScript => oldIndex.Contains(newScript.Tag))
                    .OrderBy(executedNewScript => executedNewScript)
                    .LastOrDefault();
                if (lastExecuted != null)
                {
                    var folder = group.Key;
                    _logger.Trace(() => "Last executed script in '" + folder + "' is '" + lastExecuted.Path + "' of new scripts provided.");

                    skipped.AddRange(group.Value
                        .Where(newScriptInGroup => newScriptInGroup.CompareTo(lastExecuted) < 0));
                }
            }

            return skipped.Where(newScript => !oldIndex.Contains(newScript.Tag)).ToList();
        }

        protected static string GetFirstSubfolder(string path)
        {
            if (path.Contains('\\'))
                return path.Substring(0, path.IndexOf('\\'));
            return "";
        }

        protected void LogScripts(string msg, IEnumerable<DataMigrationScript> scripts, EventType eventType = EventType.Trace)
        {
            foreach (var script in scripts)
            {
                var s = script;
                _logger.Write(eventType, () => msg + ": " + s.Tag + " " + s.Path);
            }
        }

        protected List<DataMigrationScript> LoadScriptsFromDatabase()
        {
            var scripts = new List<DataMigrationScript>();
            _sqlExecuter.ExecuteReader(
                "SELECT Tag, Path, Content FROM Rhetos.DataMigrationScript", 
                reader => scripts.Add(new DataMigrationScript
                                            {
                                                Tag = reader.GetString(0),
                                                Path = reader.GetString(1),
                                                Content = reader.GetString(2)
                                            }));
            return scripts.OrderBy(s => s).ToList();
        }

        protected List<DataMigrationScript> LoadScriptsFromDisk(string dataMigrationScriptsFolder)
        {
            var files = Directory.GetFiles(dataMigrationScriptsFolder, "*.*", SearchOption.AllDirectories);

            const string expectedExtention = ".sql";
            var badFile = files.FirstOrDefault(file => Path.GetExtension(file).ToLower() != expectedExtention);
            if (badFile != null)
                throw new FrameworkException("Data migration script '"+ badFile +"' does not have expected extention '" + expectedExtention + "'.");

            int baseFolderLength = GetFullPathLength(dataMigrationScriptsFolder);

            var scripts = (from file in files
                    let scriptPath = Path.GetFullPath(file).Substring(baseFolderLength)
                    let scriptContent = File.ReadAllText(file, Encoding.Default)
                    select new DataMigrationScript
                               {
                                   Tag = ParseScriptTag(scriptContent, scriptPath),
                                   Path = scriptPath,
                                   Content = scriptContent
                               }).ToList();

            var badGroup = scripts.GroupBy(s => s.Tag).FirstOrDefault(g => g.Count() >= 2);
            if (badGroup != null)
                throw new FrameworkException(string.Format(
                    "Data migration scripts '{0}' and '{1}' have same tag '{2}' in their headers.",
                    badGroup.First().Path, badGroup.ElementAt(1).Path, badGroup.Key));
            return scripts.OrderBy(s => s).ToList();
        }

        protected static int GetFullPathLength(string dataMigrationScriptsFolder)
        {
            dataMigrationScriptsFolder = Path.GetFullPath(dataMigrationScriptsFolder);
            if (dataMigrationScriptsFolder.Last() != '\\')
                dataMigrationScriptsFolder = dataMigrationScriptsFolder + '\\';
            return Path.GetFullPath(dataMigrationScriptsFolder).Length;
        }

        protected static readonly Regex ScriptIdRegex = new Regex(@"^/\*DATAMIGRATION (.+)\*/");

        protected static string ParseScriptTag(string scriptContent, string file)
        {
            if (!ScriptIdRegex.IsMatch(scriptContent))
                throw new FrameworkException("Data migration script '" + file + "' should start with a header '/*DATAMIGRATION unique_script_identifier*/'.");
            string tag = ScriptIdRegex.Match(scriptContent).Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(tag) || tag.Contains("\n"))
                throw new FrameworkException("Data migration script '" + file + "' has invalid header. It should start with a header '/*DATAMIGRATION unique_script_identifier*/'");
            return tag;
        }
    }
}
