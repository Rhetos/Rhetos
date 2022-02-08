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

using System;

namespace Rhetos.Utilities
{
    public static class ErrorReporting
    {
        /// <summary>
        /// The recommended error message to be returned to end user in case of a <see cref="ClientException"/>.
        /// The response should additionally include the <see cref="ClientException"/>'s <see cref="Exception.Message"/>.
        /// </summary>
        public static readonly string ClientExceptionUserMessage = "Operation could not be completed because the request sent to the server was not valid or not properly formatted.";

        /// <summary>
        /// The recommended error message to be returned to end user in case of any exception except <see cref="UserException"/> and <see cref="ClientException"/>.
        /// </summary>
        public static string GetInternalServerErrorMessage(ILocalizer localizer, Exception exception)
        {
            return localizer[
                "Internal server error occurred. See server log for more information. ({0}, {1})",
                exception.GetType().Name,
                DateTime.Now.ToString("s")];
        }
    }
}
