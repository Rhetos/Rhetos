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
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.EqualsCaseInsensitive(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringEqualsGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.NotEqualsCaseInsensitive(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringNotEqualsGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.IsLessThen(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringIsLessThenGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.IsLessThenOrEqual(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringIsLessThenOrEqualGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.IsGreaterThen(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringIsGreaterThenGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.IsGreaterThenOrEqual(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringIsGreaterThenOrEqualGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.Like(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringLikeGenerator());

                int? _nullInt = 0;

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.StartsWith(_nullInt, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.IntStartsWithGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.StartsWithCaseInsensitive(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringStartsWithGenerator());

                RegisterGenerator(ReflectionHelper.GetMethodDefinition(
                    () => Rhetos.Dom.DefaultConcepts.DatabaseExtensionFunctions.ContainsCaseInsensitive(null, null)),
                    new Rhetos.Persistence.NHibernateDefaultConcepts.StringContainsGenerator());

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
    /// LING2NH implementation of DatabaseExtensionFunctions.EqualsCaseInsensitive().
    /// </summary>
    public class StringEqualsGenerator : BaseHqlGeneratorForMethod
    {
        public StringEqualsGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.EqualsCaseInsensitive(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            var constantParameter = arguments[1] as ConstantExpression;
            if (constantParameter != null && constantParameter.Value == null)
                return treeBuilder.IsNull(visitor.Visit(arguments[0]).AsExpression());
            else
                return treeBuilder.Equality(
                    visitor.Visit(arguments[0]).AsExpression(),
                    visitor.Visit(arguments[1]).AsExpression());
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.NotEqualsCaseInsensitive().
    /// </summary>
    public class StringNotEqualsGenerator : BaseHqlGeneratorForMethod
    {
        public StringNotEqualsGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.NotEqualsCaseInsensitive(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            var constantParameter = arguments[1] as ConstantExpression;
            if (constantParameter != null && constantParameter.Value == null)
                return treeBuilder.IsNotNull(visitor.Visit(arguments[0]).AsExpression());
            else
                return treeBuilder.Inequality(
                    visitor.Visit(arguments[0]).AsExpression(),
                    visitor.Visit(arguments[1]).AsExpression());
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
            return treeBuilder.LessThan(
                visitor.Visit(arguments[0]).AsExpression(),
                visitor.Visit(arguments[1]).AsExpression());
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
            return treeBuilder.LessThanOrEqual(
                visitor.Visit(arguments[0]).AsExpression(),
                visitor.Visit(arguments[1]).AsExpression());
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.IsGreaterThen().
    /// </summary>
    public class StringIsGreaterThenGenerator : BaseHqlGeneratorForMethod
    {
        public StringIsGreaterThenGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.IsGreaterThen(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.GreaterThan(
                visitor.Visit(arguments[0]).AsExpression(),
                visitor.Visit(arguments[1]).AsExpression());
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.IsGreaterThenOrEqual().
    /// </summary>
    public class StringIsGreaterThenOrEqualGenerator : BaseHqlGeneratorForMethod
    {
        public StringIsGreaterThenOrEqualGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.IsGreaterThenOrEqual(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.GreaterThanOrEqual(
                visitor.Visit(arguments[0]).AsExpression(),
                visitor.Visit(arguments[1]).AsExpression());
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
    /// LING2NH implementation of DatabaseExtensionFunctions.StartsWithCaseInsensitive().
    /// </summary>
    public class StringStartsWithGenerator : BaseHqlGeneratorForMethod
    {
        public StringStartsWithGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.StartsWithCaseInsensitive(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.Like(
                visitor.Visit(arguments[0]).AsExpression(),
                treeBuilder.Concat(visitor.Visit(arguments[1]).AsExpression(), treeBuilder.Constant("%")));
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.ContainsCaseInsensitive().
    /// </summary>
    public class StringContainsGenerator : BaseHqlGeneratorForMethod
    {
        public StringContainsGenerator()
        {
            SupportedMethods = new[] { ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.ContainsCaseInsensitive(null, null)) };
        }

        public override HqlTreeNode BuildHql(MethodInfo method, Expression targetObject,
                ReadOnlyCollection<Expression> arguments, HqlTreeBuilder treeBuilder, IHqlExpressionVisitor visitor)
        {
            return treeBuilder.Like(
                visitor.Visit(arguments[0]).AsExpression(),
                treeBuilder.Concat(treeBuilder.Constant("%"), visitor.Visit(arguments[1]).AsExpression(), treeBuilder.Constant("%")));
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
