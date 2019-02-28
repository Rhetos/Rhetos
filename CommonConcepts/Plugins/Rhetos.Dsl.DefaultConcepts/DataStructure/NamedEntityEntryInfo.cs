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
using Rhetos.Dsl;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Entry")]
    public class NamedEntityEntryInfo : IMacroConcept
    {
        [ConceptKey]
        public NamedEntityInfo NamedEntity { get; set; }

        [ConceptKey]
        public string Name { get; set; }

        public Guid GetIdentifier()
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(Name);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                return new Guid(hashBytes);
            }
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            return new List<IConceptInfo>
            {
                new SqlFunctionInfo{
                    Module = NamedEntity.Module,
                    Name = NamedEntity.Name + "_" + Name,
                    Source = $@"
RETURNS uniqueidentifier
AS
BEGIN
	RETURN CONVERT(uniqueidentifier, '{GetIdentifier()}');
END"
                }
            };
        }
    }

    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("Value")]
    public class EntryValueInfo : IConceptInfo, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public NamedEntityEntryInfo Entry { get; set; }

        [ConceptKey]
        public string PropertyName { get; set; }

        public string Value { get; set; }

        public PropertyInfo Property { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Property" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Property = new PropertyInfo { DataStructure = Entry.NamedEntity, Name = PropertyName };
            createdConcepts = new[] { Property };
        }
    }
}
