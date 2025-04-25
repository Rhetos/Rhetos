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

using Rhetos.Extensibility;
using System.Collections.Generic;

namespace Rhetos.Utilities
{
    [Options("Rhetos:DbUpdate")]
    public class DbUpdateOptions
    {
        public static readonly string ConfigurationFileName = "rhetos-dbupdate.settings.json";

        public bool ShortTransactions { get; set; } = false;

        public bool DataMigrationSkipScriptsWithWrongOrder { get; set; } = false;

        public bool SkipRecompute { get; set; } = false;

        /// <summary>
        /// If dbupdate fails when modifying database structure based on new DSL model,
        /// the data-migration scripts that were executed in the current dbupdate command will be executed again
        /// the next time.
        /// This feature was always enabled before Rhetos v5.
        /// <para>
        /// This feature will be disabled (option setting ignored) if the <see cref="ShortTransactions"/> is enabled.
        /// </para>
        /// </summary>
        /// <remarks>
        /// This feature prevents data corruption in rare cases when the migration data is obsolete, but not yet applied to the main table.
        /// For example, if a column is renamed and a migration script successfully copies data from old column to a new column,
        /// but the database update fails for some other cause, leaving the new data in the migration table. In that case, if the application
        /// is used (even though the update failed) and the user or some service modifies the data in the old column, the next dbupdate
        /// would not migrate the newly entered data from the old column to the new one, because this migration script has already been executed.
        /// <para>
        /// Generally this feature is not needed if the application is not used after a failed dbupdate,
        /// because (1) data-migration scripts always leave migration tables in sync with main tables,
        /// and (2) database updates are coupled with data backup/restore from migration tables and Rhetos metadata updates.
        /// </para>
        /// </remarks>
        public bool RepeatDataMigrationsAfterFailedUpdate { get; set; } = true;

        /// <summary>
        /// Overrides ordering of <see cref="IServerInitializer"/> plugins.
        /// The default ordering is specified by <see cref="IServerInitializer.Dependencies"/>.
        /// This option is only for exceptional cases.
        /// The key is a full type name of the <see cref="IServerInitializer"/> plugin.
        /// Default value for all plugins is 0, recommended values are between -100 and 100.
        /// </summary>
        public Dictionary<string, decimal> OverrideServerInitializerOrdering { get; set; } = new();
    }
}
