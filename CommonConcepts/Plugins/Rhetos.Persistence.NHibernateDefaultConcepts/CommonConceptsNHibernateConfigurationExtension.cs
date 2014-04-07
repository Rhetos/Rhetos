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
using Rhetos.Extensibility;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos.Persistence.NHibernateDefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ModuleInfo))] // TODO: Initial code generator
    [ExportMetadata(MefProvider.DependsOn, typeof(ModuleCodeGenerator))] // TODO: Initial code generator
    public class CommonConceptsNHibernateConfigurationExtension : IConceptCodeGenerator
    {
        private static bool _initialized;

        public void GenerateCode(Dsl.IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            if (_initialized)
                return;
            _initialized = true;

            codeBuilder.InsertCode(

            @"{
                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.IsLessThen(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringIsLessThenGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.IsLessThenOrEqual(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringIsLessThenOrEqualGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.Like(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringLikeGenerator());

                int? _nullInt = 0;

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.StartsWith(_nullInt, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.IntStartsWithGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.CastToString(_nullInt)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.IntCastToStringGenerator());
            }
            ",
                ModuleCodeGenerator.LinqToHqlGeneratorsRegistryTag);

            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Persistence.NHibernateDefaultConcepts.StringIsLessThenGenerator));
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.IsLessThen().
    /// </summary>
    public class StringIsLessThenGenerator : BaseHqlGeneratorForMethod
    {
        public StringIsLessThenGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.IsLessThen(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.LessThan(visitor.Visit(arguments[0]).AsExpression(), visitor.Visit(arguments[1]).AsExpression());
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.IsLessThenOrEqual().
    /// </summary>
    public class StringIsLessThenOrEqualGenerator : BaseHqlGeneratorForMethod
    {
        public StringIsLessThenOrEqualGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.IsLessThenOrEqual(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.LessThanOrEqual(visitor.Visit(arguments[0]).AsExpression(), visitor.Visit(arguments[1]).AsExpression());
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.StartsWith().
    /// </summary>
    public class IntStartsWithGenerator : BaseHqlGeneratorForMethod
    {
        private static readonly int? _nullInt = 0;

        public IntStartsWithGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.StartsWith(_nullInt, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.Like(
                treeBuilder.Cast(visitor.Visit(arguments[0]).AsExpression(), typeof(string)),
                treeBuilder.Concat(visitor.Visit(arguments[1]).AsExpression(), treeBuilder.Constant("%")));
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.Like().
    /// </summary>
    public class StringLikeGenerator : BaseHqlGeneratorForMethod
    {
        public StringLikeGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.Like(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.Like(
                visitor.Visit(arguments[0]).AsExpression(),
                visitor.Visit(arguments[1]).AsExpression());
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.CastToStringGenerator().
    /// </summary>
    public class IntCastToStringGenerator : BaseHqlGeneratorForMethod
    {
        private static readonly int? _nullInt = 0;

        public IntCastToStringGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.CastToString(_nullInt)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.Cast(visitor.Visit(arguments[0]).AsExpression(), typeof(string));
        }
    }
}
