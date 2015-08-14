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

using NHibernate.Hql.Ast;
using NHibernate.Linq;
using NHibernate.Linq.Functions;
using NHibernate.Linq.Visitors;
using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos.Persistence.NHibernateDefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(InitializationConcept))]
    [ExportMetadata(MefProvider.DependsOn, typeof(DomInitializationCodeGenerator))]
    public class FullTextSearchNHibernateExtension : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(

            @"{
                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.FullTextSearch(null, null, null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.FullTextSearchGenerator());
            }
            ",
                ModuleCodeGenerator.LinqToHqlGeneratorsRegistryTag);

            codeBuilder.InsertCode(@"RegisterFunction(
                    ""FullTextSearch"",
                    new NHibernate.Dialect.Function.SQLFunctionTemplate(NHibernate.NHibernateUtil.Boolean, ""?1 IN (SELECT [KEY] FROM CONTAINSTABLE(?3.?4, ?5, ?2))""));
            ",
                ModuleCodeGenerator.NHibernateDatabaseDialectTag);

            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Persistence.NHibernateDefaultConcepts.FullTextSearchGenerator));
            codeBuilder.AddReferencesFromDependency(typeof(global::NHibernate.Dialect.Function.SQLFunctionTemplate));
            codeBuilder.AddReferencesFromDependency(typeof(global::NHibernate.NHibernateUtil));
        }
    }

    public class FullTextSearchGenerator : BaseHqlGeneratorForMethod
    {
        public FullTextSearchGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.FullTextSearch(null, null, null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            // There is a problem with NHibernate when an identifier matches an entity's name. The resulting
            // SQL may be null in that situation. The searchTable's name is split in 2 identifiers to mitigate the problem.
            string searchTable = (string)((ConstantExpression)arguments[2]).Value;
            var searchTableFullName = searchTable.Split('.');
            if (searchTableFullName.Length != 2)
                throw new FrameworkException("FullTextSearch table name '" + searchTable + "' must have format 'schema.table'.");

            string searchColumns = (string)((ConstantExpression)arguments[3]).Value;

            var parameters = new HqlExpression[]
            {
                visitor.Visit(arguments[0]).AsExpression(),
                visitor.Visit(arguments[1]).AsExpression(),
                treeBuilder.Ident(searchTableFullName[0]),
                treeBuilder.Ident(searchTableFullName[1]),
                treeBuilder.Ident(searchColumns),
            };

            return treeBuilder.BooleanMethodCall("FullTextSearch", parameters);
        }
    }
}
