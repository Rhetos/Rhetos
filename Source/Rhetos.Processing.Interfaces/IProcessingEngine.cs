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
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rhetos.Processing
{
    public interface IProcessingEngine
    {
        /// <summary>
        /// Executes given commands within a current unit of work (atomic transaction).
        /// </summary>
        /// <remarks>
        /// If all commands have been completed successfully, the methods returns <see cref="ProcessingResult"/>.
        /// In case of any error, the unit of work will be discarded (transaction rollback for all commands),
        /// and an exception thrown.
        /// <para>
        /// In case of <see cref="UserException"/> or <see cref="ClientException"/>, the exception's error message
        /// should be returned to the end user or the client application.
        /// All other exception types should be considered as "internal server error", and not sent to the client.
        /// See also <see cref="ErrorMessages"/> for recommended messages.
        /// </para>
        /// <para>
        /// This method decorates some exceptions with the summary description of the failed command,
        /// for error logging and debugging purpose.
        /// Call <see cref="ExceptionsUtility.GetCommandSummary"/> to read the summary.
        /// For performance reasons, <see cref="UserException"/> does not contain the command summary;
        /// use any of the ProcessingEngine loggers instead to debug a UserException.
        /// </para>
        /// </remarks>
        ProcessingResult Execute(IList<ICommandInfo> commands);
    }

    public static class ProcessingEngineExtensions
    {
        /// <summary>
        /// Executes given commands withing a unit of work (atomic transaction).
        /// </summary>
        /// <remarks>
        /// If the command have been completed successfully, the methods returns the command result.
        /// In case of any error, the unit of work will be discarded (transaction rollback for all commands),
        /// and an exception thrown.
        /// <para>
        /// In case of <see cref="UserException"/> or <see cref="ClientException"/>, the exception's error message
        /// should be returned to the end user or the client application.
        /// All other exception types should be considered as "internal server error", and not sent to the client.
        /// See also <see cref="ErrorMessages"/> for recommended messages.
        /// </para>
        /// <para>
        /// This method decorates some exceptions with the summary description of the failed command,
        /// for error logging and debugging purpose.
        /// Call <see cref="ExceptionsUtility.GetCommandSummary"/> to read the summary.
        /// For performance reasons, <see cref="UserException"/> does not contain the command summary;
        /// use any of the ProcessingEngine loggers instead to debug a UserException.
        /// </para>
        /// </remarks>
        public static TCommandResult Execute<TCommandResult>(this IProcessingEngine processingEngine, ICommandInfo<TCommandResult> command)
        {
            return (TCommandResult)processingEngine.Execute(new[] { command }).CommandResults.Single();
        }

        /// <summary>
        /// Executes given commands withing a unit of work (atomic transaction).
        /// </summary>
        /// <remarks>
        /// If the command have been completed successfully, the methods returns the command result.
        /// In case of any error, the unit of work will be discarded (transaction rollback for all commands),
        /// and an exception thrown.
        /// <para>
        /// In case of <see cref="UserException"/> or <see cref="ClientException"/>, the exception's error message
        /// should be returned to the end user or the client application.
        /// All other exception types should be considered as "internal server error", and not sent to the client.
        /// See also <see cref="ErrorMessages"/> for recommended messages.
        /// </para>
        /// <para>
        /// This method decorates some exceptions with the summary description of the failed command,
        /// for error logging and debugging purpose.
        /// Call <see cref="ExceptionsUtility.GetCommandSummary"/> to read the summary.
        /// For performance reasons, <see cref="UserException"/> does not contain the command summary;
        /// use any of the ProcessingEngine loggers instead to debug a UserException.
        /// </para>
        /// </remarks>
        public static object Execute(this IProcessingEngine processingEngine, ICommandInfo command)
        {
            return processingEngine.Execute(new[] { command }).CommandResults.Single();
        }
    }
}
