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

using Rhetos.Utilities;

namespace Rhetos
{
    public static class LegacyUtilities
    {
        /// <summary>
        /// Use to initialize obsolete static utilities <see cref="ConfigUtility"/>, <see cref="Utilities.Configuration"/> and <see cref="SqlUtility"/> 
        /// prior to using any of their methods. This will bind those utilities to configuration source compliant with new configuration convention.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ConfigUtility.Initialize(configuration);
            SqlUtility.Initialize(configuration);
            Utilities.Configuration.Initialize(configuration);
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
