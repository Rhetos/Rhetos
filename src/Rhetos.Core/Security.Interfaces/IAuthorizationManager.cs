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

using Rhetos.Processing;
using System.Collections.Generic;

namespace Rhetos.Security
{
    /// <summary>
    /// Provides current user's permissions for specific claims or server commands.
    /// </summary>
    public interface IAuthorizationManager
    {
        /// <summary>
        /// If the user is authorized to execute the commands, the method returns <see langword="null"/>.
        /// Otherwise it returns the authorization error message.
        /// </summary>
        /// <remarks>
        /// This method returns localized error message.
        /// </remarks>
        string Authorize(IList<ICommandInfo> commandInfos);

        /// <summary>
        /// If the user is authorized to execute the commands, the method returns <see langword="null"/>.
        /// Otherwise it returns the authorization error message.
        /// </summary>
        /// <remarks>
        /// Error is separated to <paramref name="message"/> and <paramref name="messageParameters"/> for localization.
        /// </remarks>
        void Authorize(IList<ICommandInfo> commandInfos, out string message, out string[] messageParameters);

        IList<bool> GetAuthorizations(IList<Claim> requiredClaims);
    }
}
