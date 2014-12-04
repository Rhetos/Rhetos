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

using System.ComponentModel.Composition;
using System.Collections.Generic;
using System.Linq;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Logging")]
    public class EntityLoggingInfo : IConceptInfo
    {
        [ConceptKey]
        public EntityInfo Entity { get; set; }
    }

    [Export(typeof(IConceptMacro))]
    public class EntityLoggingMacro : IConceptMacro<EntityLoggingInfo>
    {
        public IEnumerable<IConceptInfo> CreateNewConcepts(EntityLoggingInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var extension = existingConcepts.FindByType<DataStructureExtendsInfo>().Where(e => e.Extension == conceptInfo.Entity).SingleOrDefault();

            if (extension != null
                && existingConcepts.FindByType<EntityLoggingInfo>().Where(l => l.Entity == extension.Base).Any())
            {
                newConcepts.Add(new LoggingRelatedItemInfo
                {
                    Logging = conceptInfo,
                    Table = SqlUtility.Identifier(extension.Base.Module.Name) + "." + SqlUtility.Identifier(extension.Base.Name),
                    Column = "ID",
                    Relation = "Extension"
                });
            }

            return newConcepts;
        }
    }
}
