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

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    [ExportMetadata(MefProvider.Implements, typeof(QueryDataSourceCommandInfo))]
    public class QueryDataSourceCommand : ICommandImplementation
    {
        private readonly IDataTypeProvider _dataTypeProvider;
        private readonly IIndex<string, IQueryDataSourceCommandImplementation> _repositories;

        public QueryDataSourceCommand(
            IDataTypeProvider dataTypeProvider,
            IIndex<string, IQueryDataSourceCommandImplementation> repositories)
        {
            _dataTypeProvider = dataTypeProvider;
            _repositories = repositories;
        }

        public CommandResult Execute(ICommandInfo commandInfo)
        {
            var info = (QueryDataSourceCommandInfo) commandInfo;

            var repository = _repositories[info.DataSource];
            var result = repository.QueryData(info);

            return new CommandResult
            {
                Data = _dataTypeProvider.CreateBasicData(result),
                Message = result.Records.Length + " row(s) found",
                Success = true
            };
        }
    }
}
