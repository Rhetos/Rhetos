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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(SubtypeExtendPolymorphicInfo))]
    public class SubtypeExtendPolymorphicDatabaseDefinition : IConceptDatabaseDefinitionExtension
    {
        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            return null;
        }

        public void ExtendDatabaseStructure(IConceptInfo conceptInfo, ICodeBuilder codeBuilder, out IEnumerable<Tuple<IConceptInfo, IConceptInfo>> createdDependencies)
        {
            var info = (SubtypeExtendPolymorphicInfo)conceptInfo;

            codeBuilder.InsertCode(UnionSubquerySnippet(info), PolymorphicUnionViewInfo.SubtypeQueryTag, info.PolymorphicUnionView);
            createdDependencies = new[] { Tuple.Create<IConceptInfo, IConceptInfo>(info.SubtypeImplementationView, info.PolymorphicUnionView) };
        }

        private string UnionSubquerySnippet(SubtypeExtendPolymorphicInfo info)
        {
            return string.Format(@"UNION ALL
    SELECT {4}' + REPLACE(REPLACE(@columnList, ', Subtype = NULL', ', Subtype = ''{2}'''), ', {3} = NULL', ', {3} = ID') + '
    FROM {0}.{1}
",
                info.SubtypeImplementationView.Module.Name,
                info.SubtypeImplementationView.Name,
                info.IsSubtypeOf.Subtype.Module.Name + "." + info.IsSubtypeOf.Subtype.Name + (info.IsSubtypeOf.ImplementationName != "" ? " " + info.IsSubtypeOf.ImplementationName : ""),
                info.IsSubtypeOf.GetSubtypeReferenceName() + "ID",
                info.IsSubtypeOf.ImplementationName == "" ? "ID" : "ID = SubtypeImplementationID");
        }
    }
}