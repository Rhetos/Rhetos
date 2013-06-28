/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.ServiceModel;
using Rhetos;
using Rhetos.Dom;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using Rhetos.XmlSerialization;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;

namespace Rhetos.Security.Service
{
    public class RhetosServerProxy : ClientBase<IServerApplication>, IServerApplication
    {
        private readonly IDomainObjectModel _domainObjectModel;

        public RhetosServerProxy(IDomainObjectModel domainObjectModel)
        {
            _domainObjectModel = domainObjectModel;
        }

        public ServerProcessingResult Execute(ServerCommandInfo[] commands)
        {
            var result = Channel.Execute(commands);

            if (!result.Success)
            {
                if (!string.IsNullOrEmpty(result.UserMessage))
                    throw new ApplicationException(result.UserMessage, new InvalidOperationException(result.SystemMessage));
                throw new InvalidOperationException("Command execution failed:" + Environment.NewLine + result.SystemMessage);
            }

            return result;
        }

        public ServerProcessingResult Execute(ICommandInfo commandInfo)
        {
            return Execute(new[] { ToServerCommand(commandInfo) });
        }


        public object[] ReadData(string dataSource, FilterCriteria[] genericFilter)
        {
            return ExecuteQueryCommand(new QueryDataSourceCommandInfo
                {
                    DataSource = dataSource,
                    GenericFilter = genericFilter
                });
        }

        public object[] ReadData(string dataSource) 
        {
            return ExecuteQueryCommand(new QueryDataSourceCommandInfo {DataSource = dataSource});
        }

        private object[] ExecuteQueryCommand(QueryDataSourceCommandInfo commandInfo)
        {
            var result = Execute(commandInfo);
            var queryResult = XmlUtility.DeserializeFromXml<QueryDataSourceCommandResult>(result.ServerCommandResults[0].Data);
            return queryResult.Records;
        }

        private static ServerCommandInfo ToServerCommand(ICommandInfo commandInfo)
        {
            return new ServerCommandInfo
            {
                CommandName = commandInfo.GetType().Name,
                Data = XmlUtility.SerializeToXml(commandInfo),
            };
        }
    }
}