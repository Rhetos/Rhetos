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
using System.Configuration;
using System.Data.SqlClient;
using Oracle.DataAccess.Client;
using Oracle.DataAccess.Types;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

namespace Rhetos.Utilities
{
    public static class SqlUtility
    {
        private static int _sqlCommandTimeout = -1;
        public static int SqlCommandTimeout
        {
            get
            {
                if (_sqlCommandTimeout == -1)
                {
                    const string key = "SqlCommandTimeout";
                    string value = ConfigurationManager.AppSettings[key];
                    if (!string.IsNullOrEmpty(value))
                        _sqlCommandTimeout = int.Parse(ConfigurationManager.AppSettings[key]);
                    else
                        _sqlCommandTimeout = 30;
                }
                return _sqlCommandTimeout;
            }
        }

        private static string _databaseLanguage;
        private static string _nationalLanguage;

        private static void SetLanguageFromProviderName(string connectionStringProvider)
        {
            var match = new Regex(@"^Rhetos\.(?<DatabaseLanguage>\w+)(.(?<NationalLanguage>\w+))?$").Match(connectionStringProvider);
            if (!match.Success)
                throw new FrameworkException("Invalid 'providerName' format in 'ServerConnectionString' connection string. Expected providerName format is 'Rhetos.<database language>' or 'Rhetos.<database language>.<natural language settings>', for example 'Rhetos.MsSql' or 'Rhetos.Oracle.XGERMAN_CI'.");
            _databaseLanguage = match.Groups["DatabaseLanguage"].Value ?? "";
            _nationalLanguage = match.Groups["NationalLanguage"].Value ?? "";
        }

        private static string GetProviderNameFromConnectionString()
        {
            var connectionStringProvider = GetConnectionStringConfiguration().ProviderName;
            if (string.IsNullOrEmpty(connectionStringProvider))
                throw new FrameworkException("Missing 'providerName' attribute in 'ServerConnectionString' connection string. Expected providerName format is 'Rhetos.<database language>' or 'Rhetos.<database language>.<natural language settings>', for example 'Rhetos.MsSql' or 'Rhetos.Oracle.XGERMAN_CI'.");
            return connectionStringProvider;
        }

        public static string DatabaseLanguage
        {
            get
            {
                if (_databaseLanguage == null)
                    SetLanguageFromProviderName(GetProviderNameFromConnectionString());

                return _databaseLanguage;
            }
        }

        public static string NationalLanguage
        {
            get
            {
                if (_nationalLanguage == null)
                    SetLanguageFromProviderName(GetProviderNameFromConnectionString());
                    
                return _nationalLanguage;
            }
        }


        private static string _connectionString;
        public static string ConnectionString
        {
            get
            {
                if (_connectionString == null)
                {
                    _connectionString = GetConnectionStringConfiguration().ConnectionString;
                    if (string.IsNullOrEmpty(_connectionString))
                        throw new FrameworkException("Empty 'ServerConnectionString' connection string in application configuration.");
                }
                return _connectionString;
            }
        }

        private static ConnectionStringSettings GetConnectionStringConfiguration()
        {
            var connectionStringConfiguration = ConfigurationManager.ConnectionStrings["ServerConnectionString"];
            if (connectionStringConfiguration == null)
                throw new FrameworkException("Missing 'ServerConnectionString' connection string in application configuration.");
            return connectionStringConfiguration;
        }

        /// <summary>
        /// Use only for testing.
        /// This function overrides ConnectionString that is normally read from application's configuration file.
        /// </summary>
        public static void LoadSpecificConnectionString(string configFilePath)
        {
            if (_connectionString != null)
                throw new FrameworkException("Cannot execute LoadSpecificConnectionString: Connection string is already loaded.");

            var xml = new XmlDocument();
            xml.Load(configFilePath);
            try
            {
                _connectionString = xml.SelectSingleNode("/connectionStrings/add[@name='ServerConnectionString']").Attributes["connectionString"].Value;
                var providerName = xml.SelectSingleNode("/connectionStrings/add[@name='ServerConnectionString']").Attributes["providerName"].Value;
                SetLanguageFromProviderName(providerName);
            }
            catch (NullReferenceException)
            {
                throw new ApplicationException("Config file " + configFilePath + " does not have a valid connection string format.");
            }
        }

        public static string UserContextInfoText(IUserInfo userInfo)
        {
            if (!userInfo.IsUserRecognized)
                return "";

            return "Rhetos:" + userInfo.UserName + "," + userInfo.Workstation;
        }

        /// <summary>
        /// Throws an exception if 'name' is not a valid SQL database object name.
        /// Function returns given argument so it can be used as fluent interface.
        /// In some cases the function may change the identifier (limit identifier length to 30 on Oracle database, e.g.).
        /// </summary>
        public static string Identifier(string name)
        {
            CheckIdentifier(name);

            if (DatabaseLanguage == "Oracle")
                name = OracleSqlUtility.LimitIdentifierLength(name);

            return name;
        }

        /// <summary>
        /// Use Identifier(name) instead of CheckIdentifier(name) if converting a DSL feature name or object model name to an SQL identifier.
        /// </summary>
        public static string CheckIdentifier(string name)
        {
            if (name == null)
                throw new FrameworkException("Given database object name '" + name + "' is null.");

            if (string.IsNullOrEmpty(name))
                throw new FrameworkException("Given database object name '" + name + "' is empty.");

            {
                foreach (char c in name)
                    if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && (c < '0' || c > '9') && c != '_')
                        throw new FrameworkException("Given database object name '" + name + "' is not valid. Character '" + c + "' is not an english letter or number or undescore.");
            }

            {
                char c = name[0];
                if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z') && c != '_')
                    throw new FrameworkException("Given database object name '" + name + "' is not valid. First character is not an english letter or undescore.");
            }

            return name;
        }

        public static string QuoteText(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }

        public static string GetSchemaName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                if (DatabaseLanguage == "MsSql")
                    return "dbo";
                else if (DatabaseLanguage == "Oracle")
                    throw new FrameworkException("Missing schema name for database object '" + fullObjectName + "'.");
                else
                    throw new FrameworkException(UnsupportedLanguageError);

            return fullObjectName.Substring(0, dotPosition);
        }

        public static string GetShortName(string fullObjectName)
        {
            int dotPosition = fullObjectName.IndexOf('.');
            if (dotPosition == -1)
                return fullObjectName;

            var shortName = fullObjectName.Substring(dotPosition + 1);

            int secondDot = shortName.IndexOf('.');
            if (secondDot != -1 || string.IsNullOrEmpty(shortName))
                throw new FrameworkException("Invalid database object name: '" + fullObjectName + "'. Expected format is 'schema.name' or 'name'.");
            return shortName;
        }

        public static string GetFullName(string objectName)
        {
            var schema = SqlUtility.GetSchemaName(objectName);
            var name = SqlUtility.GetShortName(objectName);
            SqlUtility.CheckIdentifier(schema);
            SqlUtility.CheckIdentifier(name);
            return schema + "." + name;
        }

        private static string UnsupportedLanguageError
        {
            get
            {
                return "SqlUtility functions are not supported for database language '" + DatabaseLanguage + "'."
                    + " Supported database languages are: 'MsSql', 'Oracle'.";
            }
        }

        public static Guid ReadGuid(DbDataReader dataReader, int column)
        {
            if (string.Equals(DatabaseLanguage, "MsSql", StringComparison.Ordinal))
                return dataReader.GetGuid(column);
            else if (string.Equals(DatabaseLanguage, "Oracle", StringComparison.Ordinal))
                return new Guid(((OracleDataReader)dataReader).GetOracleBinary(0).Value);
            else
                throw new FrameworkException(UnsupportedLanguageError);
        }

        public static Guid StringToGuid(string guid)
        {
            if (string.Equals(DatabaseLanguage, "MsSql", StringComparison.Ordinal))
                return Guid.Parse(guid);
            else if (string.Equals(DatabaseLanguage, "Oracle", StringComparison.Ordinal))
                return new Guid(StringToByteArray(guid));
            else
                throw new FrameworkException(UnsupportedLanguageError);
        }

        public static string QuoteGuid(Guid guid)
        {
            return "'" + GuidToString(guid) + "'";
        }

        public static string GuidToString(Guid guid)
        {
            if (string.Equals(DatabaseLanguage, "MsSql", StringComparison.Ordinal))
                return guid.ToString().ToUpper();
            else if (string.Equals(DatabaseLanguage, "Oracle", StringComparison.Ordinal))
                return ByteArrayToString(guid.ToByteArray());
            else
                throw new FrameworkException(UnsupportedLanguageError);
        }

        private static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:X2}", b);
            return hex.ToString();
        }

        private static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length / 2;
            byte[] bytes = new byte[NumberChars];
            StringReader sr = new StringReader(hex);
            for (int i = 0; i < NumberChars; i++)
                bytes[i] = Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            sr.Dispose();
            return bytes;
        }

        /// <summary>
        /// Returns empty string if the string value is null.
        /// This function is used for compatibility between MsSql and Orace string behavior.
        /// </summary>
        public static string EmptyNullString(DbDataReader dataReader, int column)
        {
            if (string.Equals(DatabaseLanguage, "MsSql", StringComparison.Ordinal))
                return dataReader.GetString(column) ?? "";
            else if (string.Equals(DatabaseLanguage, "Oracle", StringComparison.Ordinal))
            {
                var s = ((OracleDataReader)dataReader).GetOracleString(column);
                return !s.IsNull ? s.Value : "";
            }
            else
                throw new FrameworkException(UnsupportedLanguageError);
        }

        public static string MaskPassword(string connectionString)
        {
            var passwordSearchRegex = new[]
            {
                @"\b(password|pwd)\s*=(?<pwd>[^;]*)",
                @"\b/(?<pwd>[^/;=]*)@"
            };

            foreach (var regex in passwordSearchRegex)
            {
                var matches = new Regex(regex, RegexOptions.IgnoreCase).Matches(connectionString);
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    var pwdGroup = matches[i].Groups["pwd"];
                    if (pwdGroup.Success)
                        connectionString = connectionString
                            .Remove(pwdGroup.Index, pwdGroup.Length)
                            .Insert(pwdGroup.Index, "*");
                }
            }
            return connectionString;
        }

        /// <summary>
        /// Used in DatabaseGenerator to split SQL script generated by IConceptDatabaseDefinition plugins.
        /// </summary>
        public const string ScriptSplitter = "/* database generator splitter */";

        public static DateTime GetDatabaseTime(ISqlExecuter sqlExecuter)
        {
            if (DatabaseLanguage == "MsSql")
                return MsSqlUtility.GetDatabaseTime(sqlExecuter);
            else if (DatabaseLanguage == "Oracle")
                throw new FrameworkException("GetDatabaseTime function is not yet supported in Rhetos for Oracle database.");
            else
                throw new FrameworkException(UnsupportedLanguageError);
        }
    }
}
