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
using Rhetos.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.Deployment
{
    public class DataMigrationScriptsExecuter
    {
        protected readonly ISqlExecuter _sqlExecuter;
        protected readonly ILogger _logger;
        protected readonly DataMigrationScripts _dataMigrationScripts;
        protected readonly SqlTransactionBatches _sqlTransactionBatches;
        protected readonly DbUpdateOptions _dbUpdateOptions;

        public DataMigrationScriptsExecuter(ISqlExecuter sqlExecuter, ILogProvider logProvider, DataMigrationScripts dataMigrationScripts,
            DbUpdateOptions dbUpdateOptions, SqlTransactionBatches sqlTransactionBatches)
        {
            _sqlExecuter = sqlExecuter;
            _logger = logProvider.GetLogger("DataMigration");
            _dataMigrationScripts = dataMigrationScripts;
            _sqlTransactionBatches = sqlTransactionBatches;
            _dbUpdateOptions = dbUpdateOptions;
        }

        public DataMigrationReport Execute()
        {
            var newScripts = _dataMigrationScripts.Scripts;

            var scriptsInOtherLanguages = FindScriptsInOtherLanguages(newScripts, SqlUtility.DatabaseLanguage);
            LogScripts("Ignoring scripts in other database languages", scriptsInOtherLanguages);
            newScripts = newScripts.Except(scriptsInOtherLanguages).ToList();
            LogScripts("Script on disk", newScripts);

            var oldScripts = LoadScriptsFromDatabase();
            LogScripts("Script in database", oldScripts);

            var newIndex = new HashSet<string>(newScripts.Select(s => s.Tag));
            var oldIndex = new HashSet<string>(oldScripts.Select(s => s.Tag));
            List<DataMigrationScript> toRemove = oldScripts.Where(os => !newIndex.Contains(os.Tag)).ToList();
            List<DataMigrationScript> toExecute = newScripts.Where(ns => !oldIndex.Contains(ns.Tag)).ToList();

            // "skipped" are the new scripts that are ordered *before* some old scripts that were already executed.
            List<DataMigrationScript> skipped = FindSkipedScriptsInEachPackage(oldScripts, newScripts);
            string skippedReport = string.Empty;
            if (skipped.Count > 0)
            {
                if (_dbUpdateOptions.DataMigrationSkipScriptsWithWrongOrder)
                {
                    // Ignore skipped scripts for backward compatibility.
                    LogScripts("Skipped older script", skipped, EventType.Warning);
                    toExecute = toExecute.Except(skipped).ToList();
                    skippedReport = " " + skipped.Count + " older skipped.";
                }
                else
                {
                    // Execute skipped scripts even though this means the scripts will be executed in the incorrect order.
                    // The message is logged as an *error* to increase the chance of being noticed because it is one-off event, even though it is not blocking.
                    LogScripts("Executing script in an incorrect order", skipped, EventType.Warning);
                }
            }

            ApplyToDatabase(toRemove, toExecute);

            _logger.Info($"Executed {toExecute.Count} of {newScripts.Count} scripts.{skippedReport}");

            return new DataMigrationReport { CreatedTags = toExecute.Select(s => s.Tag).ToList() };
        }

        public void Undo(List<string> createdTags)
        {
            _sqlExecuter.ExecuteSql(createdTags.Select(tag =>
                "UPDATE Rhetos.DataMigrationScript SET Active = 0 WHERE Tag = " + SqlUtility.QuoteText(tag)));
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
            LogScripts("Removing", toRemove, EventType.Info);
            Undo(toRemove.Select(s => s.Tag).ToList());

            LogScripts("Executing", toExecute, EventType.Info);
            _sqlTransactionBatches.Execute(toExecute
                .SelectMany(script => new[]
                {
                    new SqlTransactionBatches.SqlScript { Sql = script.Content, IsBatch = true, Name = script.Path },
                    new SqlTransactionBatches.SqlScript { Sql = SaveDataMigrationScriptMetadata(script), IsBatch = false, Name = null },
                }));
        }

        protected static string SaveDataMigrationScriptMetadata(DataMigrationScript script)
        {
            return string.Format(
                "DELETE FROM Rhetos.DataMigrationScript WHERE Active = 0 AND Tag = {0};\r\n"
                + "INSERT INTO Rhetos.DataMigrationScript (Tag, Path, Content, Active) VALUES ({0}, {1}, {2}, 1);",
                SqlUtility.QuoteText(script.Tag),
                SqlUtility.QuoteText(script.Path),
                SqlUtility.QuoteText(script.Content));
        }

        protected List<DataMigrationScript> FindSkipedScriptsInEachPackage(List<DataMigrationScript> oldScripts, List<DataMigrationScript> newScripts)
        {
            var oldIndex = new HashSet<string>(oldScripts.Select(s => s.Tag));

            var newByPackage = newScripts
                .GroupBy(ns => GetFirstSubfolder(ns.Path))
                .ToDictionary(g => g.Key, g => g.ToList());

            var skipped = new List<DataMigrationScript>();
            foreach (var group in newByPackage)
            {
                var lastExecuted = group.Value
                    .Where(newScript => oldIndex.Contains(newScript.Tag))
                    .OrderBy(executedNewScript => executedNewScript)
                    .LastOrDefault();
                if (lastExecuted != null)
                {
                    var folder = group.Key;
                    _logger.Trace(() => $"Last executed script in '{folder}' is '{lastExecuted.Path}' of new scripts provided.");

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
                _logger.Write(eventType, () => $"{msg} {script.Path} (tag {script.Tag})");
        }

        protected List<DataMigrationScript> LoadScriptsFromDatabase()
        {
            var scripts = new List<DataMigrationScript>();
            _sqlExecuter.ExecuteReader(
                "SELECT Tag, Path, Content FROM Rhetos.DataMigrationScript WHERE Active = 1 ORDER BY DateExecuted",
                reader => scripts.Add(new DataMigrationScript
                    {
                        Tag = reader.GetString(0),
                        Path = reader.GetString(1),
                        Content = reader.GetString(2)
                    }));
            return scripts.ToList();
        }
    }

    public class DataMigrationReport
    {
        public List<string> CreatedTags { get; set; }
    }
}
