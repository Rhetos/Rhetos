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

using System.Data.Common;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using Rhetos.DatabaseGenerator;
using Autofac;

namespace Rhetos.Utilities
{
    public static class SqlUtility
    {
        #region Static helpers for ISqlUtility

        /// <summary>Using WeakReference to avoid interfering with the DI container disposal</summary>
        private static WeakReference<ISqlUtility> _currentSqlUtilityReference;

        private static ISqlUtility SqlUtilityVerified
        {
            get
            {
                if (_currentSqlUtilityReference == null)
                    throw new FrameworkException($"SqlUtility has not been initialized. Call {nameof(StaticUtilities)}.{nameof(StaticUtilities.Initialize)} at startup.");

                if (!_currentSqlUtilityReference.TryGetTarget(out var sqlUtility))
                    throw new InvalidOperationException($"The previously provided DI container has already been disposed.");

                return sqlUtility;
            }
        }

        /// <summary>
        /// The static <see cref="ISqlUtility"/> instance is a helper for writing code generators, as a trade-off
        /// between convenience and cleanness, and for backward compatibility. Since it is used in many classes and plugin types,
        /// it might complicate the code to resolve it each time from the dependency injection.
        /// </summary>
        public static void Initialize(ISqlUtility sqlUtility)
        {
            if (sqlUtility != null)
            {
                if (_currentSqlUtilityReference != null)
                    throw new ArgumentException($"{typeof(SqlUtility)} class has already been initialized.");

                _currentSqlUtilityReference = new WeakReference<ISqlUtility>(sqlUtility);
            }
            else
            {
                _currentSqlUtilityReference = null;
            }
        }

        /// <summary>
        /// Throws an exception if 'name' is not a valid SQL database object name.
        /// Function returns given argument so it can be used as fluent interface.
        /// In some cases the function may change the identifier (limit identifier length to 30 on Oracle database, e.g.).
        /// </summary>
        public static string Identifier(string name) => SqlUtilityVerified.Identifier(name);

        public static string QuoteText(string value) => SqlUtilityVerified.QuoteText(value);

        public static string QuoteIdentifier(string sqlIdentifier) => SqlUtilityVerified.QuoteIdentifier(sqlIdentifier);

        public static string GetSchemaName(string fullObjectName) => SqlUtilityVerified.GetSchemaName(fullObjectName);

        public static string GetShortName(string fullObjectName) => SqlUtilityVerified.GetShortName(fullObjectName);

        public static string GetFullName(string objectName) => SqlUtilityVerified.GetFullName(objectName);

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public static Guid ReadGuid(DbDataReader dataReader, int column) => SqlUtilityVerified.ReadGuid(dataReader, column);

        /// <summary>
        /// Vendor-independent database reader.
        /// </summary>
        public static int ReadInt(DbDataReader dataReader, int column) => SqlUtilityVerified.ReadInt(dataReader, column);

        public static Guid StringToGuid(string guid) => SqlUtilityVerified.StringToGuid(guid);

        public static string QuoteGuid(Guid? guid) => SqlUtilityVerified.QuoteGuid(guid);

        public static string GuidToString(Guid? guid) => SqlUtilityVerified.GuidToString(guid);

        public static string QuoteDateTime(DateTime? dateTime) => SqlUtilityVerified.QuoteDateTime(dateTime);

        public static string DateTimeToString(DateTime? dateTime) => SqlUtilityVerified.DateTimeToString(dateTime);

        public static string QuoteBool(bool? b) => SqlUtilityVerified.QuoteBool(b);

        public static string BoolToString(bool? b) => SqlUtilityVerified.BoolToString(b);

        /// <summary>
        /// Returns empty string if the string value is null.
        /// This function is used for compatibility between MsSql and Oracle string behavior.
        /// </summary>
        public static string EmptyNullString(DbDataReader dataReader, int column) => SqlUtilityVerified.EmptyNullString(dataReader, column);

        public static DateTime GetDatabaseTime(ISqlExecuter sqlExecuter) => SqlUtilityVerified.GetDatabaseTime(sqlExecuter);

        #endregion

        #region User context in database

        public static string UserContextInfoText(IUserInfo userInfo)
        {
            string userReport = userInfo.IsUserRecognized ? userInfo.Report() : "";
            return "Rhetos:" + userReport;
        }

        public static IUserInfo ExtractUserInfo(string contextInfo)
        {
            if (contextInfo == null)
                return new ReconstructedUserInfo { IsUserRecognized = false, UserName = null, Workstation = null };

            string prefix1 = "Rhetos:";
            string prefix2 = "Alpha:";

            int positionUser;
            if (contextInfo.StartsWith(prefix1))
                positionUser = prefix1.Length;
            else if (contextInfo.StartsWith(prefix2))
                positionUser = prefix2.Length;
            else
                return new ReconstructedUserInfo { IsUserRecognized = false, UserName = null, Workstation = null };

            var result = new ReconstructedUserInfo();

            int positionWorkstation = contextInfo.IndexOf(',', positionUser);
            if (positionWorkstation > -1)
            {
                result.UserName = contextInfo.Substring(positionUser, positionWorkstation - positionUser);
                result.Workstation = contextInfo.Substring(positionWorkstation + 1);
            }
            else
            {
                result.UserName = contextInfo.Substring(positionUser);
                result.Workstation = "";
            }

            result.UserName = result.UserName.Trim();
            if (result.UserName == "") result.UserName = null;
            result.Workstation = result.Workstation.Trim();
            if (result.Workstation == "") result.Workstation = null;

            result.IsUserRecognized = result.UserName != null;
            return result;
        }

        private class ReconstructedUserInfo : IUserInfo
        {
            public bool IsUserRecognized { get; set; }
            public string UserName { get; set; }
            public string Workstation { get; set; }
            public string Report() { return UserName + "," + Workstation; }
        }

        #endregion

        #region Script splitter

        /// <summary>
        /// Split SQL script generated by IConceptDatabaseGenerator plugins into multiple SQL scripts.
        /// </summary>
        public static readonly string ScriptSplitterTag = "\r\nGO\r\n";

        /// <summary>
        /// Add this tag to the beginning of the DatabaseGenerator SQL script to execute it without transaction.
        /// Used for special database changes that must be executed without transaction, for example
        /// creating a full-text search index.
        /// </summary>
        public const string NoTransactionTag = "/*DatabaseGenerator:NoTransaction*/";

        public static bool ScriptSupportsTransaction(string sql)
        {
            return !sql.TrimStart().StartsWith(NoTransactionTag);
        }

        /// <summary>
        /// Splits the script to multiple batches, separated by the GO command.
        /// It emulates the behavior of Microsoft SQL Server utilities, sqlcmd and osql,
        /// but it does not work perfectly: comments near GO, strings containing GO and the repeat count are currently not supported.
        /// </summary>
        public static string[] SplitBatches(string sql)
        {
            return batchSplitter.Split(sql).Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        }

        private static readonly Regex batchSplitter = new Regex(@"^\s*GO\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

        #endregion
    }
}
