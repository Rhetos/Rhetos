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

namespace Rhetos.Utilities
{
    /// <summary>
    /// Implementation of this interface is a security principal provider.
    /// </summary>
    public interface IUserInfo
    {
        bool IsUserRecognized { get; }

        string UserName { get; }

        /// <summary>
        /// Client host name or address.
        /// </summary>
        string Workstation { get; }

        /// <summary>
        /// Specially formatted user info. Usual implementation is "Domain\UserName,WorkStation".
        /// </summary>
        /// <returns></returns>
        string Report();
    }

    public class NullUserInfo : IUserInfo
    {
        public bool IsUserRecognized => false;
        public string UserName => throw new FrameworkException($"This operation is not supported for {nameof(NullUserInfo)}.");
        public string Workstation => null;
        public string Report() => nameof(NullUserInfo);
    }
}
