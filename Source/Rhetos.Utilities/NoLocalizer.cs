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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    /// <summary>
    /// This is the default implementation of ILocalizer.
    /// It simply returns the given string, without modification.
    /// </summary>
    public class NoLocalizer : ILocalizer
    {
        public LocalizedString this[object message, params object[] args]
        {
            get
            {
                string messageText = message.ToString();
                if (args != null)
                    return new LocalizedString(messageText, string.Format(CultureInfo.InvariantCulture, messageText, args));
                else
                    return new LocalizedString(messageText, messageText);
            }
        }
    }

    /// <summary>
    /// This is the default implementation of ILocalizer.
    /// It simply returns the given string, without modification.
    /// </summary>
    public class NoLocalizer<T> : NoLocalizer, ILocalizer<T>
    {
    }
}
