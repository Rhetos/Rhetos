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
using Rhetos.Utilities;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;
using Rhetos.Factory;
using Rhetos.Processing;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(QueryableExtensionInfo))]
    public class QueryableExtensionCodeGenerator : IConceptCodeGenerator
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

        protected static string RepositoryFunctionsSnippet(QueryableExtensionInfo info)
        {
            return string.Format(
@"        public static readonly Func<IQueryable<{2}.{3}>, Common.DomRepository{4}, IQueryable<global::{0}.{1}>> Compute =
            {5};

",
                info.Module.Name, info.Name, info.Base.Module.Name, info.Base.Name, DomUtility.ComputationAdditionalParametersTypeTag.Evaluate(info), info.Expression);
        }

        protected static string QuerySnippet(QueryableExtensionInfo info)
        {
            return string.Format(
@"            return Compute(_domRepository.{0}.{1}.Query(), _domRepository{2});
",
                info.Base.Module.Name, info.Base.Name, DomUtility.ComputationAdditionalParametersArgumentTag.Evaluate(info));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (QueryableExtensionInfo)conceptInfo;

            codeBuilder.InsertCode(CodeSnippet.Evaluate(info), DataStructureCodeGenerator.BodyTag, info);
            codeBuilder.AddInterfaceAndReference(string.Format("System.IEquatable<{0}>", info.Name), typeof(IEquatable<>), info);

            PropertyInfo idProperty = new PropertyInfo { DataStructure = info, Name = "ID" };
            PropertyHelper.GenerateCodeForType(idProperty, codeBuilder, "Guid", true);
            codeBuilder.AddInterfaceAndReference(typeof(IEntity), info);

            RepositoryHelper.GenerateRepository(info, codeBuilder);
            RepositoryHelper.GenerateQueryableRepositoryFunctions(info, codeBuilder, QuerySnippet(info));
            codeBuilder.InsertCode(RepositoryFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
        }
    }
}
