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
using System.Linq;
using System.Text;
using Rhetos.Utilities;
using System.ComponentModel.Composition;
using Rhetos.Compiler;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(NamedEntityInfo))]
    public class NamedEntityDatabseDefinition : IConceptDatabaseDefinition
    {
        public static readonly SqlTag<NamedEntityInfo> FirstValueTag = new SqlTag<NamedEntityInfo>("FirstValue");

        public static readonly SqlTag<NamedEntityInfo> ValuesTag = new SqlTag<NamedEntityInfo>("Values");

        public static readonly SqlTag<NamedEntityInfo> UpdateColumnsTag = new SqlTag<NamedEntityInfo>("UpdateColumns");

        public static readonly SqlTag<NamedEntityInfo> InsertColumnsTag = new SqlTag<NamedEntityInfo>("InsertColumns");

        IDslModel _dslModel;

        public NamedEntityDatabseDefinition(IDslModel dslModel)
        {
            _dslModel = dslModel;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (NamedEntityInfo)conceptInfo;

            return $@"
                MERGE {info.Module.Name}.{info.Name} AS target  
                USING (
	                {FirstValueTag.Evaluate(info)}{ValuesTag.Evaluate(info)}
                ) AS source
                ON target.ID = source.ID
                WHEN NOT MATCHED BY TARGET THEN  
                INSERT (ID) VALUES (ID)
                WHEN NOT MATCHED BY SOURCE THEN DELETE;
            ";
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }
    }
}
