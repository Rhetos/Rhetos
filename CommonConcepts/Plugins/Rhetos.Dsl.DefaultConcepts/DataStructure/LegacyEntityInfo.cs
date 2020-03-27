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
using Rhetos.Dsl;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    ///  Simplifies access from Rhetos application to legacy application database.
    ///  It maps a Rhetos data structure to the legacy database table or view.
    ///  It allows both read and write operations (either with updateable views or generated instead-of triggers).
    ///  It allows mapping of complex primary and foreign keys to standard Rhetos reference properties.
    ///  Prerequisites: The legacy table needs to be extended with uniqueidentifier ID column with default NEWID() and a unique index.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("LegacyEntity")]
    public class LegacyEntityInfo : DataStructureInfo, IWritableOrmDataStructure
    {
        public string Table { get; set; }
        public string View { get; set; }

        string IOrmDataStructure.GetOrmSchema()
        {
            return SqlUtility.GetSchemaName(View);
        }

        string IOrmDataStructure.GetOrmDatabaseObject()
        {
            return SqlUtility.GetShortName(View);
        }
    }
}
