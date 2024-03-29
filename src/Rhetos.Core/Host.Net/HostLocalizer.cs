﻿/*
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
using Microsoft.Extensions.Options;
using Rhetos.Utilities;

namespace Rhetos.Host.Net
{
    public class HostLocalizer : ILocalizer
    {
        protected readonly IStringLocalizer stringLocalizer;

        protected HostLocalizer(IStringLocalizer stringLocalizer)
        {
            this.stringLocalizer = stringLocalizer;
        }

        public HostLocalizer(IStringLocalizerFactory stringLocalizerFactory, IOptions<HostLocalizerOptions> localizerOptions)
        {
            this.stringLocalizer = stringLocalizerFactory.Create(localizerOptions.Value.BaseName, localizerOptions.Value.Location);
        }

        public LocalizedString this[object message, params object[] args]
        {
#pragma warning disable CA1508 // Avoid dead conditional code. This warning is false positive here for params args.
            get
            {
                if(message is string)
                    return stringLocalizer[message.ToString(), args ?? System.Array.Empty<object>()];
                else
                    return stringLocalizer["{0}", message];

            }
#pragma warning restore CA1508 // Avoid dead conditional code
        }
    }

    public class HostLocalizer<T> : HostLocalizer, ILocalizer<T>
    {
        public HostLocalizer(IStringLocalizerFactory stringLocalizerFactory)
            : base(stringLocalizerFactory.Create(typeof(T)))
        {
        }
    }
}
