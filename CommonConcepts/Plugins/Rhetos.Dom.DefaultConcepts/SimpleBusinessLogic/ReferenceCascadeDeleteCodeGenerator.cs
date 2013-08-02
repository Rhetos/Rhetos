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
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferenceCascadeDeleteInfo))]
    public class ReferenceCascadeDeleteCodeGenerator : IConceptCodeGenerator
    {
        private static string CodeSnippetDeleteChildren(ReferencePropertyInfo reference)
        {
            return string.Format(
@"            if (deleted.Count() > 0)
            {{
                {0}.{1}[] childItems = deleted.SelectMany(parent => _executionContext.NHibernateSession.Query<{0}.{1}>().Where(child => child.{2}.ID == parent.ID).ToArray()).ToArray();
                if (childItems.Count() > 0)
                    _domRepository.{0}.{1}.Delete(childItems);
            }}
",
            reference.DataStructure.Module.Name,
            reference.DataStructure.Name,
            reference.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var reference = ((ReferenceCascadeDeleteInfo)conceptInfo).Reference;

            if(reference.Referenced is IWritableOrmDataStructure)
                codeBuilder.InsertCode(CodeSnippetDeleteChildren(reference), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, reference.Referenced);
        }
    }
}
