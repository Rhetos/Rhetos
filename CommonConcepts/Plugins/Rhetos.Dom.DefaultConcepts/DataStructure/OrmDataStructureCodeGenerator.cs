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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;
using Rhetos.Processing.DefaultCommands;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureCodeGenerator))]
    public class OrmDataStructureCodeGenerator : IConceptCodeGenerator
    {
        public static readonly DataStructureCodeGenerator.DataStructureTag GetHashCodeTag =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*get hash code {0}.{1}*/");
        public static readonly DataStructureCodeGenerator.DataStructureTag EqualsBaseTag =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*equals base {0}.{1}*/");
        public static readonly DataStructureCodeGenerator.DataStructureTag EqualsInterfaceTag =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*equals interface {0}.{1}*/");


        protected static readonly DataStructureCodeGenerator.DataStructureTag CodeSnippet = new DataStructureCodeGenerator.DataStructureTag(TagType.CodeSnippet,
@"
        public override int GetHashCode()
        {{
            " + GetHashCodeTag + @"
            return _ID.GetHashCode();
        }}

        public override bool Equals(object o)
        {{
            " + EqualsBaseTag + @"
            var other = o as {1};
            return other != null && other._ID == _ID;
        }}

        bool System.IEquatable<{1}>.Equals({1} other)
        {{
            " + EqualsInterfaceTag + @"
            return other != null && other._ID == _ID;
        }}
");

        protected static string QuerySnippet(DataStructureInfo info)
        {
            return string.Format(
@"            return _executionContext.NHibernateSession.Query<global::{0}.{1}>();
",
                info.Module.Name, info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;

            if (info is IOrmDataStructure)
            {
                codeBuilder.InsertCode(CodeSnippet.Evaluate(info), DataStructureCodeGenerator.BodyTag, info);
                codeBuilder.AddInterfaceAndReference(string.Format("System.IEquatable<{0}>", info.Name), typeof (System.IEquatable<>), info);

                PropertyInfo idProperty = new PropertyInfo {DataStructure = info, Name = "ID"};
                PropertyHelper.GenerateCodeForType(idProperty, codeBuilder, "Guid", true);
                codeBuilder.AddInterfaceAndReference(typeof (IEntity), info);

                RepositoryHelper.GenerateRepository(info, codeBuilder);
                RepositoryHelper.GenerateQueryableRepositoryFunctions(info, codeBuilder, QuerySnippet(info));

                codeBuilder.AddReferencesFromDependency(typeof(IQueryDataSourceCommandImplementation));
                codeBuilder.AddReferencesFromDependency(typeof(GenericFilterWithPagingUtility));
                codeBuilder.AddReferencesFromDependency(typeof(QueryDataSourceCommandResult));
            }
        }
    }
}
