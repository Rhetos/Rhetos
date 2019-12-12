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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dsl;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.DatabaseGenerator.Test
{
    public partial class DatabaseGeneratorTest
    {
        private class SimpleConcept : IConceptInfo
        {
            public SimpleConcept(string name, string sql, string data = null)
            {
                Name = name;
                Sql = sql;
                Data = data ?? "";
            }

            [ConceptKey]
            public string Name { get; set; }

            public string Sql { get; set; }

            public string Data { get; set; }
        }

        private class NoImplementationConcept : IConceptInfo
        {
            public NoImplementationConcept(string name, IConceptInfo parent)
            {
                Name = name;
                Parent = parent;
            }

            [ConceptKey]
            public string Name { get; set; }

            public IConceptInfo Parent { get; set; }
        }

        private class ReferenceConcept : IConceptInfo
        {
            public ReferenceConcept(string name, IConceptInfo parent, string sql)
            {
                Name = name;
                Parent = parent;
                Sql = sql;
            }

            [ConceptKey]
            public string Name { get; set; }

            public IConceptInfo Parent { get; set; }

            public string Sql { get; set; }
        }

        private class ReferenceReferenceConcept : IConceptInfo
        {
            public ReferenceReferenceConcept(string name, ReferenceConcept parent, string sql)
            {
                Name = name;
                Parent = parent;
                Sql = sql;
            }

            [ConceptKey]
            public string Name { get; set; }

            public ReferenceConcept Parent { get; set; }

            public string Sql { get; set; }
        }

        private class SimpleImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) => ((SimpleConcept)conceptInfo).Sql;

            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) => RemoveQuery(CreateDatabaseStructure(conceptInfo));
        }

        private class ReferenceImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) => ((ReferenceConcept)conceptInfo).Sql;

            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) => RemoveQuery(CreateDatabaseStructure(conceptInfo));
        }

        private class ReferenceReferenceImplementation : IConceptDatabaseDefinition
        {
            public string CreateDatabaseStructure(IConceptInfo conceptInfo) => ((ReferenceReferenceConcept)conceptInfo).Sql;

            public string RemoveDatabaseStructure(IConceptInfo conceptInfo) => RemoveQuery(CreateDatabaseStructure(conceptInfo));
        }

        public static string RemoveQuery(string createQuery)
        {
            if (createQuery.StartsWith(SqlUtility.NoTransactionTag))
                return SqlUtility.NoTransactionTag + "drop-" + createQuery.Substring(SqlUtility.NoTransactionTag.Length);
            else
                return "drop-" + createQuery;
        }
    }
}
