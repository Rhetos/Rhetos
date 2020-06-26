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

using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Logging;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    [ExportMetadata(MefProvider.Implements, typeof(ReadCommandInfo))]
    public class ReadCommand : ICommandImplementation
    {
        private readonly IDataTypeProvider _dataTypeProvider;
        private readonly GenericRepositories _repositories;
        private readonly ILogger _logger;
        private readonly ServerCommandsUtility _serverCommandsUtility;

        public ReadCommand(
            IDataTypeProvider dataTypeProvider,
            GenericRepositories repositories,
            ILogProvider logProvider,
            ServerCommandsUtility serverCommandsUtility)
        {
            _dataTypeProvider = dataTypeProvider;
            _repositories = repositories;
            _logger = logProvider.GetLogger(GetType().Name);
            _serverCommandsUtility = serverCommandsUtility;
        }

        public CommandResult Execute(ICommandInfo commandInfo)
        {
            var readInfo = (ReadCommandInfo)commandInfo;

            if (readInfo.DataSource == null)
                throw new ClientException("Invalid ReadCommand argument: Data source is not set.");
            
            var genericRepository = _repositories.GetGenericRepository(readInfo.DataSource);
            ReadCommandResult result = _serverCommandsUtility.ExecuteReadCommand(readInfo, genericRepository);

            if (result.Records != null && !AlreadyFilteredByRowPermissions(readInfo))
            {
                var valid = _serverCommandsUtility.CheckAllItemsWithinFilter(result.Records, RowPermissionsReadInfo.FilterName, genericRepository);
                if (!valid)
                    throw new UserException("You are not authorized to access some or all of the data requested.", $"DataStructure:{readInfo.DataSource}.");
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
                return readCommand.Filters.Any(x => GenericFilterHelper.EqualsSimpleFilter(x, RowPermissionsReadInfo.FilterName));
            }

            return false;
        }
    }
}
