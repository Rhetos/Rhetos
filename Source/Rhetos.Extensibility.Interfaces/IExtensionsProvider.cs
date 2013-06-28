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
using System.Diagnostics.Contracts;

namespace Rhetos.Extensibility
{
    [ContractClass(typeof(PluginProviderContract))]
    public interface IExtensionsProvider
    {
        Dictionary<Type , List<Type>> OrganizePlugins<TPluginInterface>(Lazy<TPluginInterface, Dictionary<string, object>>[] plugins);
        Dictionary<string, KeyValuePair<Type, Dictionary<string, object>>> FindConcepts<TConceptInterface>(Lazy<TConceptInterface, Dictionary<string, object>>[] implementations);
    }

    [ContractClassFor(typeof(IExtensionsProvider))]
    sealed class PluginProviderContract : IExtensionsProvider
    {
        public Dictionary<Type, List<Type>> OrganizePlugins<TPluginInterface>(Lazy<TPluginInterface, Dictionary<string, object>>[] plugins)
        {
            Contract.Requires(plugins != null);
            Contract.Ensures(Contract.Result<Dictionary<Type, List<Type>>>() != null);

            return null;
        }

        public Dictionary<string, KeyValuePair<Type, Dictionary<string, object>>> FindConcepts<TConceptInterface>(Lazy<TConceptInterface, Dictionary<string, object>>[] implementations)
        {
            Contract.Requires(implementations != null);
            Contract.Ensures(Contract.Result<Dictionary<string, Tuple<Type, Dictionary<string, object>>>>() != null);

            return null;
        }
    }

}
