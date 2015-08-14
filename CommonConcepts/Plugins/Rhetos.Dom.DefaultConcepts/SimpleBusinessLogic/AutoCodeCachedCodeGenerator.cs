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

using NHibernate;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(AutoCodeCachedInfo))]
    public class AutoCodeCachedCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (AutoCodeCachedInfo)conceptInfo;

            string snippet = string.Format(
                @"Rhetos.Dom.DefaultConcepts.AutoCodeHelper.UpdateCodes(
                _executionContext.NHibernateSession, ""{0}.{1}"", ""{2}"",
                insertedNew.Select(item => new Rhetos.Dom.DefaultConcepts.AutoCodeItem<{0}.{1}> {{ Item = item, Code = item.{2}, Grouping = """" }}),
                (item, newCode) => item.{2} = newCode);

            ",
                info.Property.DataStructure.Module.Name,
                info.Property.DataStructure.Name,
                info.Property.Name);
            codeBuilder.InsertCode(snippet, WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Property.DataStructure);
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.AutoCodeHelper));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.AutoCodeItem<>));
        }
    }
}
