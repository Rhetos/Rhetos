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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using NHibernate.Cfg;
using NHibernate.Cfg.Loquacious;
using NHibernate.Hql.Ast;
using NHibernate.Linq;
using NHibernate.Linq.Functions;
using NHibernate.Linq.Visitors;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Persistence.NHibernate;


namespace Rhetos.Persistence.NHibernateDefaultConcepts
{
    [Export(typeof(INHibernateConfigurationExtension))]
    public sealed class CommonConceptsNHibernateConfigurationExtension : INHibernateConfigurationExtension
    {
        public void ExtendConfiguration(Configuration configuration)
        {
            configuration.LinqToHqlGeneratorsRegistry<MyLinqToHqlGeneratorsRegistry>();
        }
    }

    public sealed class MyLinqToHqlGeneratorsRegistry : DefaultLinqToHqlGeneratorsRegistry
    {
        private static int? _nullInt = 0;

        public MyLinqToHqlGeneratorsRegistry()
        {
            RegisterGenerator(ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.IsLessThen(null, null)), new StringIsLessThenGenerator());
            RegisterGenerator(ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.IsLessThenOrEqual(null, null)), new StringIsLessThenOrEqualGenerator());
            RegisterGenerator(ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.StartsWith(_nullInt, null)), new IntStartsWithGenerator());
            RegisterGenerator(ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.Like(null, null)), new StringLikeGenerator());
            RegisterGenerator(ReflectionHelper.GetMethodDefinition(() => DatabaseExtensionFunctions.CastToString(_nullInt)), new IntCastToStringGenerator());
        }
    }

    /// <summary>
    /// LING2NH implementation of DatabaseExtensionFunctions.IsLessThen().
    /// </summary>
    public class StringIsLessThenGenerator : BaseHqlGeneratorForMethod // TODO: Make internal if possible.
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
    public class StringIsLessThenOrEqualGenerator : BaseHqlGeneratorForMethod // TODO: Make internal if possible.
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
    public class IntStartsWithGenerator : BaseHqlGeneratorForMethod // TODO: Make internal if possible.
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
    public class StringLikeGenerator : BaseHqlGeneratorForMethod // TODO: Make internal if possible.
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
    public class IntCastToStringGenerator : BaseHqlGeneratorForMethod // TODO: Make internal if possible.
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
