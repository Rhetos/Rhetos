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
        /// If database update fails (modifying database structure based on new DSL model),
        /// the data-migration scripts that were executed in the current dbupdate command will be executed again
        /// the next time.
        /// This feature was always enabled before Rhetos v5.
        /// </summary>
        /// <remarks>
        /// Generally this feature is not needed, because (1) data-migration scripts always leave migration tables in sync with main tables,
        /// and (2) database updates are coupled with data backup/restore from migration tables and Rhetos metadata updates.
        /// This option is left configurable to support any special cases that might require multiple executions of data-migration scrips.
        /// </remarks>
        public bool RepeatDataMigrationsAfterFailedUpdate { get; set; } = false;
    }
}
