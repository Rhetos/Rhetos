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
using Rhetos.Logging;

namespace Rhetos.Processing.DefaultCommands
{
    [Obsolete("Use ReadCommand")]
    [Export(typeof(ICommandImplementation))]
    [ExportMetadata(MefProvider.Implements, typeof(QueryDataSourceCommandInfo))]
    public class QueryDataSourceCommand : ICommandImplementation
    {
        private readonly IDataTypeProvider _dataTypeProvider;
        private readonly GenericRepositories _repositories;
        private readonly ServerCommandsUtility _serverCommandsUtility;

        public QueryDataSourceCommand(
            IDataTypeProvider dataTypeProvider,
            GenericRepositories repositories,
            ServerCommandsUtility serverCommandsUtility)
        {
            _dataTypeProvider = dataTypeProvider;
            _repositories = repositories;
            _serverCommandsUtility = serverCommandsUtility;
        }

        public CommandResult Execute(ICommandInfo commandInfo)
        {
            var info = (QueryDataSourceCommandInfo)commandInfo;

            var genericRepository = _repositories.GetGenericRepository(info.DataSource);
            var readCommandInfo = info.ToReadCommandInfo();
            ReadCommandResult readCommandResult = _serverCommandsUtility.ExecuteReadCommand(readCommandInfo, genericRepository);

            var result = QueryDataSourceCommandResult.FromReadCommandResult(readCommandResult);
            return new CommandResult
            {
                Data = _dataTypeProvider.CreateBasicData(result),
                Message = (result.Records != null ? result.Records.Length.ToString() : result.TotalRecords.ToString()) + " row(s) found",
                Success = true
            };
        }
    }
}
