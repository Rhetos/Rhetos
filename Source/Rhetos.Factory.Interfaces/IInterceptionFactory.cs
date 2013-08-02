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
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics.Contracts;
using Castle.DynamicProxy;

namespace Rhetos.Factory
{
    [ContractClass(typeof(InterceptionFactoryContract<>))]
    public interface IInterceptionFactory<TIntercepted>
    {
        void RegisterBeforeMethod(MethodInfo method, Action<IInvocation> action);
        void RegisterBeforeMethod(LambdaExpression lambda, Action<IInvocation> action);
        void RegisterAfterMethod(MethodInfo method, Action<IInvocation> action);
        void RegisterAfterMethod(LambdaExpression lambda, Action<IInvocation> action);
    }

    public static class IInterceptionFactoryExpressions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void RegisterBeforeMethod<TIntercepted>(
            this IInterceptionFactory<TIntercepted> factory,
            Expression<Action<TIntercepted>> expression,
            Action<IInvocation> action)
        {
            Contract.Requires(expression != null);
            Contract.Requires(action != null);

            factory.RegisterBeforeMethod(expression, action);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void RegisterBeforeMethod<TIntercepted, TResult>(
            this IInterceptionFactory<TIntercepted> factory,
            Expression<Func<TIntercepted, TResult>> expression,
            Action<IInvocation> action)
        {
            Contract.Requires(expression != null);
            Contract.Requires(action != null);

            factory.RegisterBeforeMethod(expression, action);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void RegisterAfterMethod<TIntercepted>(
            this IInterceptionFactory<TIntercepted> factory,
            Expression<Action<TIntercepted>> expression,
            Action<IInvocation> action)
        {
            Contract.Requires(expression != null);
            Contract.Requires(action != null);

            factory.RegisterAfterMethod(expression, action);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public static void RegisterAfterMethod<TIntercepted, TResult>(
            this IInterceptionFactory<TIntercepted> factory,
            Expression<Func<TIntercepted, TResult>> expression,
            Action<IInvocation> action)
        {
            Contract.Requires(expression != null);
            Contract.Requires(action != null);
            
            factory.RegisterAfterMethod(expression, action);
        }

    }

    [ContractClassFor(typeof(IInterceptionFactory<>))]
    sealed class InterceptionFactoryContract<TIntercepted> : IInterceptionFactory<TIntercepted>
    {
        public void RegisterBeforeMethod(MethodInfo method, Action<IInvocation> action)
        {
            Contract.Requires(method != null);
            Contract.Requires(action != null);
        }

        public void RegisterBeforeMethod(LambdaExpression expression, Action<IInvocation> action)
        {
            Contract.Requires(expression != null);
            Contract.Requires(action != null);
        }

        public void RegisterAfterMethod(MethodInfo method, Action<IInvocation> action)
        {
            Contract.Requires(method != null);
            Contract.Requires(action != null);
        }

        public void RegisterAfterMethod(LambdaExpression expression, Action<IInvocation> action)
        {
            Contract.Requires(expression != null);
            Contract.Requires(action != null);
        }
    }
}
