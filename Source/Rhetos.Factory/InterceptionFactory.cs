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
using Rhetos.Extensibility;
using Rhetos.Persistence;
using Castle.DynamicProxy;
using System.Reflection;
using System.Linq.Expressions;
using System.Diagnostics.Contracts;

namespace Rhetos.Factory
{
    public class InterceptionFactory<TIntercepted> : IInterceptionFactory<TIntercepted>
    {
        private readonly Dictionary<MethodInfo, List<Action<IInvocation>>> BeforeMethodFactory = new Dictionary<MethodInfo, List<Action<IInvocation>>>();
        private readonly Dictionary<MethodInfo, List<Action<IInvocation>>> AfterMethodFactory = new Dictionary<MethodInfo, List<Action<IInvocation>>>();

        public InterceptionFactory(IAspectFactory aspectFactory)
        {
            Contract.Requires(aspectFactory != null);

            aspectFactory.RegisterAspect<TIntercepted>(new InterceptAspect(this));
        }

        private static void AddToDictionary(Dictionary<MethodInfo, List<Action<IInvocation>>> dict, MethodInfo method, Action<IInvocation> action)
        {
            Contract.Requires(dict != null);
            Contract.Requires(method != null);
            Contract.Requires(action != null);

            lock (dict)
            {
                if (dict.ContainsKey(method))
                    dict[method].Add(action);
                else
                    dict.Add(method, new List<Action<IInvocation>>(new[] { action }));
            }
        }

        public void RegisterBeforeMethod(MethodInfo method, Action<IInvocation> action)
        {
            AddToDictionary(BeforeMethodFactory, method, action);
        }

        public void RegisterBeforeMethod(LambdaExpression lambda, Action<IInvocation> action)
        {
            AddToDictionary(BeforeMethodFactory, StrongReflection.GetMethodInfo(lambda), action);
        }

        public void RegisterAfterMethod(MethodInfo method, Action<IInvocation> action)
        {
            AddToDictionary(AfterMethodFactory, method, action);
        }

        public void RegisterAfterMethod(LambdaExpression lambda, Action<IInvocation> action)
        {
            AddToDictionary(AfterMethodFactory, StrongReflection.GetMethodInfo(lambda), action);
        }

        private static void RunActions(Dictionary<MethodInfo, List<Action<IInvocation>>> dict, MethodInfo method, IInvocation args)
        {
            Contract.Requires(dict != null);
            Contract.Requires(method != null);

            List<Action<IInvocation>> list;
            if (dict.TryGetValue(method, out list))
                foreach (var action in list)
                    action(args);
        }

        private bool IsRegisteredFor(MethodInfo method)
        {
            Contract.Requires(method != null);

            return BeforeMethodFactory.ContainsKey(method)
                || AfterMethodFactory.ContainsKey(method);
        }

        private class InterceptAspect : IAspect
        {
            private readonly Func<Type, MethodInfo, bool> selector;
            private readonly InterceptAdvice advice;

            public InterceptAspect(InterceptionFactory<TIntercepted> interceptors)
            {
                selector = (t, m) => interceptors.IsRegisteredFor(m);

                advice = new InterceptAdvice(interceptors);
            }

            public int Priority { get { return 100; } }

            public Func<Type, System.Reflection.MethodInfo, bool> IsValidForMethod
            {
                get { return selector; }
            }

            public IInterceptor Advice
            {
                get { return advice; }
            }

            private class InterceptAdvice : IInterceptor
            {
                private readonly InterceptionFactory<TIntercepted> interceptors;

                public InterceptAdvice(InterceptionFactory<TIntercepted> interceptors)
                {
                    this.interceptors = interceptors;
                }

                public void Intercept(IInvocation invocation)
                {
                    InterceptionFactory<TIntercepted>.RunActions(interceptors.BeforeMethodFactory, invocation.Method, invocation);

                    invocation.Proceed();

                    InterceptionFactory<TIntercepted>.RunActions(interceptors.AfterMethodFactory, invocation.Method, invocation);
                }
            }
        }
    }
}
