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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Autofac.Features.Indexed;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Dom;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Logging;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    [ExportMetadata(MefProvider.Implements, typeof(ReadCommandInfo))]
    public class ReadCommand : ICommandImplementation
    {
        private readonly IDataTypeProvider _dataTypeProvider;
        private readonly GenericRepositories _repositories;
        private readonly ILogger _logger;

        public ReadCommand(
            IDataTypeProvider dataTypeProvider,
            GenericRepositories repositories,
            ILogProvider logProvider)
        {
            _dataTypeProvider = dataTypeProvider;
            _repositories = repositories;
            _logger = logProvider.GetLogger(GetType().Name);
        }

        public CommandResult Execute(ICommandInfo commandInfo)
        {
            var readInfo = commandInfo as ReadCommandInfo;

            if (readInfo == null)
                return CommandResult.Fail("CommandInfo does not implement ReadCommandInfo");

            if (readInfo.DataSource == null)
                throw new ClientException("Invalid ReadCommand argument: Data source is not set.");
            
            var genericRepository = _repositories.GetGenericRepository(readInfo.DataSource);
            ReadCommandResult result = genericRepository.ExecuteReadCommand(readInfo);

            if (result.Records != null && !AlreadyFilteredByRowPermissions(readInfo))
            {
                var valid = genericRepository.CheckAllItemsWithinFilter(result.Records, RowPermissionsReadInfo.FilterName);
                if (!valid) 
                    throw new UserException("Insufficient permissions to access some or all of the data requested.", "DataStructure:" + readInfo.DataSource + ".");
            }

            return new CommandResult
            {
                Data = _dataTypeProvider.CreateBasicData(result),
                Message = (result.Records != null ? result.Records.Length.ToString() : result.TotalCount.ToString()) + " row(s) found",
                Success = true
            };
        }

        private bool AlreadyFilteredByRowPermissions(ReadCommandInfo readCommand)
        {
            if (readCommand.Filters != null && readCommand.Filters.Length > 0)
            {
                int lastRowPermissionFilter = -1;
                for (int f = readCommand.Filters.Length - 1; f >= 0; f--)
                    if (GenericFilterHelper.EqualsSimpleFilter(readCommand.Filters[f], RowPermissionsReadInfo.FilterName))
                    {
                        lastRowPermissionFilter = f;
                        break;
                    }

                if (lastRowPermissionFilter >= 0)
                    if (lastRowPermissionFilter == readCommand.Filters.Length - 1)
                    {
                        _logger.Trace(() => string.Format("(DataStructure:{0}) Last filter is '{1}', skipping RowPermissionsRead validation.",
                            readCommand.DataSource, RowPermissionsReadInfo.FilterName));
                        return true;
                    }
                    else
                        _logger.Trace(() => string.Format("(DataStructure:{0}) Warning: Improve performance by moving filter '{1}' to last position, in order to skip RowPermissionsRead validation.",
                            readCommand.DataSource, RowPermissionsReadInfo.FilterName));
            }

            return false;
        }
    }
}
