﻿/*
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlIndex")]
    public class SqlIndex2Info : IConceptInfo, IValidationConcept, IMacroConcept
    {
        [ConceptKey]
        public EntityInfo Entity { get; set; } // TODO: Entity to DataStructure.
        [ConceptKey]
        public PropertyInfo Property1 { get; set; }
        [ConceptKey]
        public PropertyInfo Property2 { get; set; }

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {
            if (Property1.DataStructure != Entity)
                throw new Exception(string.Format(
                    "SqlIndex is not well defined because property {0}.{1}.{2} is not in entity {3}.{4}.",
                    Property1.DataStructure.Module.Name,
                    Property1.DataStructure.Name,
                    Property1.Name,
                    Entity.Module.Name,
                    Entity.Name));

            if (Property2.DataStructure != Entity)
                throw new Exception(string.Format(
                    "SqlIndex is not well defined because property {0}.{1}.{2} is not in entity {3}.{4}.",
                    Property2.DataStructure.Module.Name,
                    Property2.DataStructure.Name,
                    Property2.Name,
                    Entity.Module.Name,
                    Entity.Name));
        }

        public SqlIndexMultipleInfo GetCreatedIndex()
        {
            return new SqlIndexMultipleInfo { Entity = Entity, PropertyNames = Property1.Name + " " + Property2.Name };
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new[] { GetCreatedIndex() };
        }
    }
}
