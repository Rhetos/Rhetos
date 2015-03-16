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
        private static string CodeSnippetDeleteChildren(ReferenceCascadeDeleteInfo info)
        {
            return string.Format(
@"            if (deleted.Count() > 0)
            {{
                {0}.{1}[] childItems = deleted.SelectMany(parent => _executionContext.NHibernateSession.Query<{0}.{1}>().Where(child => child.{2}.ID == parent.ID).ToArray()).ToArray();
                if (childItems.Count() > 0)
                {{
                    _domRepository.{0}.{1}.Delete(childItems);
                    // Workaround to restore NH proxies after using NHSession.Clear() when deleting the detail entity.
                    for (int i = 0; i < updated.Length; i++) updated[i] = _executionContext.NHibernateSession.Load<{3}.{4}>(updated[i].ID);
                    for (int i = 0; i < deleted.Length; i++) deleted[i] = _executionContext.NHibernateSession.Load<{3}.{4}>(deleted[i].ID);
                }}
            }}
",
            info.Reference.DataStructure.Module.Name,
            info.Reference.DataStructure.Name,
            info.Reference.Name,
            info.Reference.Referenced.Module.Name,
            info.Reference.Referenced.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = ((ReferenceCascadeDeleteInfo)conceptInfo);

            if(info.Reference.Referenced is IWritableOrmDataStructure)
                codeBuilder.InsertCode(CodeSnippetDeleteChildren(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Reference.Referenced);
        }
    }
}
