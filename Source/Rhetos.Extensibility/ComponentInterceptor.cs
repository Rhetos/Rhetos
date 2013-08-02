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
using Autofac.Core;
using System.Diagnostics.Contracts;

namespace Rhetos.Extensibility
{
    public class ComponentInterceptor : IComponentInterceptorRegistrator, IComponentInterceptorExecutor
    {
        private readonly Dictionary<Type, List<Action<ActivatedEventArgs<object>>>> activatedDictonary = new Dictionary<Type, List<Action<ActivatedEventArgs<object>>>>();
        private readonly Dictionary<Type, List<Action<ActivatingEventArgs<object>>>> activatingDictonary = new Dictionary<Type, List<Action<ActivatingEventArgs<object>>>>();

        private static void AddToDictionary<TAction>(Dictionary<Type, List<TAction>> dict, Type type, TAction action)
        {
            Contract.Requires(dict != null);
            Contract.Requires(type != null);
            Contract.Requires(action != null);

            lock (dict)
            {
                if (dict.ContainsKey(type))
                    dict[type].Add(action);
                else
                    dict.Add(type, new List<TAction>(new[] { action }));
            }
        }

        private static void RunActions<TEventArgs>(Type type, Dictionary<Type, List<Action<TEventArgs>>> dict, TEventArgs args)
        {
            Contract.Requires(dict != null);
            Contract.Requires(type != null);

            List<Action<TEventArgs>> list;
            if (dict.TryGetValue(type, out list))
                foreach (var action in list)
                    action(args);
        }

        public void RegisterOnActivated<TComponent>(Action<ActivatedEventArgs<object>> action)
        {
            AddToDictionary(activatedDictonary, typeof(TComponent), action);
        }

        public void RegisterOnActivated(Type type, Action<ActivatedEventArgs<object>> action)
        {
            AddToDictionary(activatedDictonary, type, action);
        }

        public void RunActivatedActions(Type type, ActivatedEventArgs<object> args)
        {
            RunActions(type, activatedDictonary, args);
        }

        public void RegisterOnActivating<TComponent>(Action<ActivatingEventArgs<object>> action)
        {
            AddToDictionary(activatingDictonary, typeof(TComponent), action);
        }

        public void RegisterOnActivating(Type type, Action<ActivatingEventArgs<object>> action)
        {
            AddToDictionary(activatingDictonary, type, action);
        }

        public void RunActivatingActions(Type type, ActivatingEventArgs<object> args)
        {
            RunActions(type, activatingDictonary, args);
        }

    }
}
