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
using System.Collections.Generic;

namespace Rhetos
{
    [Options("Rhetos:ConfigurationProvider")]
    public class ConfigurationProviderOptions
    {
        /// <summary>
        /// Allow old configuration files to work with new Rhetos applications (v4.0+),
        /// without updating configuration keys in the .config files.
        /// </summary>
        public LegacyKeysSupport LegacyKeysSupport { get; set; } = LegacyKeysSupport.Error;

        /// <summary>
        /// Report warnings if legacy keys are found in configuration.
        /// It is enabled by default on build and database update, but disabled on application runtime to avoid spamming the log.
        /// </summary>
        public bool LegacyKeysWarning { get; set; } = false;

        /// <summary>
        /// Old configuration keys, indexed by new key.
        /// </summary>
        public static readonly IDictionary<string, string> LegacyKeysMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Rhetos:App:AuthorizationAddUnregisteredPrincipals", "AuthorizationAddUnregisteredPrincipals" },
            { "Rhetos:App:AuthorizationCacheExpirationSeconds", "AuthorizationCacheExpirationSeconds" },
            { "Rhetos:AppSecurity:BuiltinAdminOverride", "BuiltinAdminOverride" },
            { "Rhetos:Build:InitialConceptsSort", "CommonConcepts.Debug.SortConcepts" },
            { "CommonConcepts:AutoGeneratePolymorphicProperty", "CommonConcepts.Legacy.AutoGeneratePolymorphicProperty" },
            { "CommonConcepts:CascadeDeleteInDatabase", "CommonConcepts.Legacy.CascadeDeleteInDatabase" },
            { "Rhetos:DbUpdate:DataMigrationSkipScriptsWithWrongOrder", "DataMigration.SkipScriptsWithWrongOrder" },
            { "Rhetos:App:EntityFrameworkUseDatabaseNullSemantics", "EntityFramework.UseDatabaseNullSemantics" },
            { "Rhetos:AppSecurity:AllClaimsForUsers", "Security.AllClaimsForUsers" },
            { "Rhetos:AppSecurity:LookupClientHostname", "Security.LookupClientHostname" },
            { "Rhetos:Database:SqlCommandTimeout", "SqlCommandTimeout" },
            { "Rhetos:SqlTransactionBatches:MaxJoinedScriptCount", "SqlExecuter.MaxJoinedScriptCount" },
            { "Rhetos:SqlTransactionBatches:MaxJoinedScriptSize", "SqlExecuter.MaxJoinedScriptSize" },
            { "Rhetos:SqlTransactionBatches:ReportProgressMs", "SqlExecuter.ReportProgressMs" },
        };
    }

    public enum LegacyKeysSupport
    {
        /// <summary>
        /// Legacy keys support is disabled.
        /// </summary>
        Ignore,

        /// <summary>
        /// Legacy keys are automatically converted to new configuration keys,
        /// so that the new Rhetos applications (v4.0+) can use old configuration files.
        /// </summary>
        Convert,

        /// <summary>
        /// Legacy keys support is disabled.
        /// If any legacy key is found in configuration, an error will be raised.
        /// </summary>
        Error
    };
}
