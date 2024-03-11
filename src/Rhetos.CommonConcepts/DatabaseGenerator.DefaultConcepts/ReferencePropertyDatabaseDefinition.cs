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

using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
#pragma warning disable CS0618 // Type or member is obsolete
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    public class ReferencePropertyDatabaseDefinition : IConceptDatabaseDefinition
#pragma warning restore CS0618 // Type or member is obsolete
    {
        ConceptMetadata _conceptMetadata;

        protected ISqlResources Sql { get; private set; }

        protected ISqlUtility SqlUtility { get; private set; }

        public ReferencePropertyDatabaseDefinition(ConceptMetadata conceptMetadata, ISqlResources sqlResources, ISqlUtility sqlUtility)
        {
            _conceptMetadata = conceptMetadata;
            this.Sql = sqlResources;
            this.SqlUtility = sqlUtility;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (ReferencePropertyInfo)conceptInfo;

            if (info.DataStructure is EntityInfo)
                return PropertyDatabaseDefinition.AddColumn(SqlUtility, Sql, _conceptMetadata, info);

            return "";
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (ReferencePropertyInfo)conceptInfo;
            if (info.DataStructure is EntityInfo)
                return PropertyDatabaseDefinition.RemoveColumn(SqlUtility, Sql, _conceptMetadata, info);
            return "";
        }
    }
}
