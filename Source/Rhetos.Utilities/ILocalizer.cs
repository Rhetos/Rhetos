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

using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    /// <summary>
    /// Used for localization of end-user messages.
    /// Use the indexer method to translate a message to the client's language.
    /// </summary>
    public interface ILocalizer
    {
        /// <summary>
        /// Use of the indexer is similar to string.Format() function.
        /// Example: <code>string translatedMsg = _localizer["{0} cannot have value {1}.", somePropertyName, newValue]</code>
        /// </summary>
        /// <param name="message">The parameter type is object, instead of string, to enable simple localization of DateTime and other types.</param>
        LocalizedString this[object message, params object[] args]
        {
            get;
        }
    }

    /// <summary>
    /// Used for localization of end-user messages.
    /// Use the indexer method to translate a message to the client's language.
    /// </summary>
    /// <remarks>
    /// Full name of type <typeparamref name="T"/> is used as a base name (or context) for localization key.
    /// This can be used for localization of the property names for an entity of the given type, to allow different translations of the same property name on a different entity type.
    /// </remarks>
    public interface ILocalizer<T> : ILocalizer
    {
    }
}
