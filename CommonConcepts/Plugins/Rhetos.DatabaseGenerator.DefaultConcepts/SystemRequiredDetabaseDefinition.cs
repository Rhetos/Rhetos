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
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SystemRequiredInfo))]
    public class SystemRequiredDetabaseDefinition : IConceptDatabaseDefinition
    {
        private static bool IsSupported(SystemRequiredInfo info)
        {
            return info.Property.DataStructure is EntityInfo;
        }

        private static string GetConstraintName(SystemRequiredInfo info)
        {
            return "CK_" + info.Property.DataStructure.Name + "_" + info.Property.Name + "_NOTNULL";
        }

        private static string GetColumnName(SystemRequiredInfo info)
        {
            if (info.Property is ReferencePropertyInfo)
                return info.Property.Name + "ID";
            return info.Property.Name;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SystemRequiredInfo)conceptInfo;

            // TODO: Use NOT NULL instead of CHECK constraint, after we implement IConceptInfo metadata (SQL column type)
            if (IsSupported(info))
                return string.Format("ALTER TABLE {0}.{1} WITH NOCHECK ADD CONSTRAINT {2} CHECK ({3} IS NOT NULL)",
                    SqlUtility.Identifier(info.Property.DataStructure.Module.Name),
                    SqlUtility.Identifier(info.Property.DataStructure.Name),
                    SqlUtility.Identifier(GetConstraintName(info)),
                    SqlUtility.Identifier(GetColumnName(info)));

            return null;

        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (SystemRequiredInfo)conceptInfo;

            if (IsSupported(info))
                return string.Format("ALTER TABLE {0}.{1} DROP CONSTRAINT {2}",
                    SqlUtility.Identifier(info.Property.DataStructure.Module.Name),
                    SqlUtility.Identifier(info.Property.DataStructure.Name),
                    SqlUtility.Identifier(GetConstraintName(info)));

            return null;
        }
    }
}
