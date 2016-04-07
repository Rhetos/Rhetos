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
    [ExportMetadata(MefProvider.DependsOn, typeof(DataStructureQueryableCodeGenerator))]
    public class OrmDataStructureCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<DataStructureInfo> GetHashCodeTag = "Orm GetHashCode";
        public static readonly CsTag<DataStructureInfo> EqualsBaseTag = "Orm EqualsBase";
        public static readonly CsTag<DataStructureInfo> EqualsInterfaceTag = "Orm EqualsInterface";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DataStructureInfo)conceptInfo;
            var orm = info as IOrmDataStructure;

            if (orm != null)
            {
                codeBuilder.InsertCode(SnippetEntityClassMembers(info), DataStructureCodeGenerator.BodyTag, info);
                DataStructureCodeGenerator.AddInterfaceAndReference(codeBuilder, string.Format("System.IEquatable<{0}>", info.Name), typeof(System.IEquatable<>), info);

                RepositoryHelper.GenerateRepository(info, codeBuilder);
                RepositoryHelper.GenerateQueryableRepositoryFunctions(info, codeBuilder, QuerySnippet(info));
                codeBuilder.InsertCode(SnippetQueryableFilterById(info), RepositoryHelper.RepositoryMembers, info);

                PropertyInfo idProperty = new PropertyInfo { DataStructure = info, Name = "ID" };
                PropertyHelper.GenerateCodeForType(idProperty, codeBuilder, "Guid");
                DataStructureCodeGenerator.AddInterfaceAndReference(codeBuilder, typeof(IEntity), info);

                codeBuilder.InsertCode(
                    string.Format("public System.Data.Entity.DbSet<Common.Queryable.{0}_{1}> {0}_{1} {{ get; set; }}\r\n        ",
                        info.Module.Name, info.Name),
                    DomInitializationCodeGenerator.EntityFrameworkContextMembersTag);
                codeBuilder.InsertCode(
                    string.Format("modelBuilder.Ignore<global::{0}.{1}>();\r\n            "
                        + "modelBuilder.Entity<Common.Queryable.{0}_{1}>().Map(m => {{ m.MapInheritedProperties(); m.ToTable(\"{3}\", \"{2}\"); }});\r\n            ",
                        info.Module.Name, info.Name, orm.GetOrmSchema(), orm.GetOrmDatabaseObject()),
                    DomInitializationCodeGenerator.EntityFrameworkOnModelCreatingTag);
            }
        }

        protected static string SnippetEntityClassMembers(DataStructureInfo info)
        {
            return
        @"public override int GetHashCode()
        {
            " + GetHashCodeTag.Evaluate(info) + @"
            return ID.GetHashCode();
        }

        public override bool Equals(object o)
        {
            " + EqualsBaseTag.Evaluate(info) + @"
            var other = o as " + info.Name + @";
            return other != null && other.ID == ID;
        }

        public bool Equals(" + info.Name + @" other)
        {
            " + EqualsInterfaceTag.Evaluate(info) + @"
            return other != null && other.ID == ID;
        }

        ";
        }

        protected static string QuerySnippet(DataStructureInfo info)
        {
            return string.Format(
                @"return _executionContext.EntityFrameworkContext.{0}_{1}.AsNoTracking();",
                info.Module.Name, info.Name);
        }

        public static string SnippetQueryableFilterById(DataStructureInfo info)
        {
            return string.Format(
        @"public IQueryable<Common.Queryable.{0}_{1}> Filter(IQueryable<Common.Queryable.{0}_{1}> items, IEnumerable<Guid> ids)
        {{
            if (!(ids is System.Collections.IList))
                ids = ids.ToList();

            if (ids.Count() < 2000) // EF 6.1.3. has performance issues on Contains function with large lists. It seems to have O(n^2) time complexity.
                return items.Where(item => ids.Contains(item.ID));
            else
            {{
                var idsQuery = _domRepository.Common.FilterId.CreateQueryableFilterIds(ids);
                return items.Where(item => idsQuery.Contains(item.ID));
            }}
        }}

        ",
                info.Module.Name, info.Name);
        }
    }
}
