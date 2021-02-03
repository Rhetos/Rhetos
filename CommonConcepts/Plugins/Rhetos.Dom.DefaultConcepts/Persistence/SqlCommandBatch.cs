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

using Rhetos.Persistence;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
	/// <summary>
	/// Helper class for saving data from entities (POCO classes) directly to database.
	/// </summary>
	public class SqlCommandBatch : IPersistenceStorageCommandBatch
	{
		private readonly IPersistenceTransaction _persistenceTransaction;
		private readonly IPersistenceStorageObjectMappings _persistenceMappingConfiguration;

		public SqlCommandBatch(
			IPersistenceTransaction persistenceTransaction,
			IPersistenceStorageObjectMappings persistenceMappingConfiguration)
		{
			_persistenceTransaction = persistenceTransaction;
			_persistenceMappingConfiguration = persistenceMappingConfiguration;
		}

		public int Execute(IList<PersistenceStorageCommand> commands)
		{
			if (commands.Count == 0)
				return 0;

			var commandParameters = new List<DbParameter>();
			var commandTextBuilder = new StringBuilder();
			foreach (var command in commands)
				AppendCommand(command, commandParameters, commandTextBuilder);

			using (var dbCommand = _persistenceTransaction.Connection.CreateCommand())
			{
				dbCommand.Transaction = _persistenceTransaction.Transaction;

				dbCommand.CommandText = commandTextBuilder.ToString();
				foreach (var parameter in commandParameters)
					dbCommand.Parameters.Add(parameter);
				return dbCommand.ExecuteNonQuery();
			}
		}

		private void AppendCommand(PersistenceStorageCommand command, List<DbParameter> commandParameters, StringBuilder commandTextBuilder)
		{
			if (command.CommandType == PersistenceStorageCommandType.Insert)
			{
				AppendInsertCommand(commandParameters, commandTextBuilder, command.Entity, _persistenceMappingConfiguration.GetMapping(command.EntityType));
			}
			if (command.CommandType == PersistenceStorageCommandType.Update)
			{
				AppendUpdateCommand(commandParameters, commandTextBuilder, command.Entity, _persistenceMappingConfiguration.GetMapping(command.EntityType));
			}
			if (command.CommandType == PersistenceStorageCommandType.Delete)
			{
				AppendDeleteCommand(commandTextBuilder, command.Entity, _persistenceMappingConfiguration.GetMapping(command.EntityType));
			}
		}

		private void AppendInsertCommand(List<DbParameter> commandParameters, StringBuilder commandTextBuilder, IEntity entity, IPersistenceStorageObjectMapper mapper)
		{
			var parameters = mapper.GetParameters(entity);
			InitializeAndAppendParameters(commandParameters, parameters);
			AppendInsertCommandTextForType(parameters, mapper.GetTableName(), commandTextBuilder);
		}

		private void InitializeAndAppendParameters(List<DbParameter> commandParameters, PersistenceStorageObjectParameter[] parameters)
		{
			var index = commandParameters.Count;
			foreach (var item in parameters)
			{
				item.DbParameter.ParameterName = "@" + index;
				index++;
			}
			commandParameters.AddRange(parameters.Select(x => x.DbParameter));
		}

		private void AppendUpdateCommand(List<DbParameter> commandParameters, StringBuilder commandTextBuilder, IEntity entity, IPersistenceStorageObjectMapper mapper)
		{
			var parameters = mapper.GetParameters(entity);
			InitializeAndAppendParameters(commandParameters, parameters);
			if (parameters.Length > 1) // If entity has other properties besides ID.
				AppendUpdateCommandTextForType(parameters, mapper.GetTableName(), commandTextBuilder);
			else
				AppendEmptyUpdateCommandTextForType(parameters, mapper.GetTableName(), commandTextBuilder);
		}

		private void AppendDeleteCommand(StringBuilder commandTextBuilder, IEntity entity, IPersistenceStorageObjectMapper mapper)
		{
			var entityName = mapper.GetTableName();
			commandTextBuilder.Append($@"DELETE FROM {entityName} WHERE ID = '{entity.ID}';");
		}

		private void AppendInsertCommandTextForType(PersistenceStorageObjectParameter[] parameters, string tableFullName, StringBuilder commandTextBuilder)
		{
			commandTextBuilder.Append("INSERT INTO " + tableFullName + " (");
			foreach (var keyValue in parameters)
			{
				commandTextBuilder.Append(keyValue.PropertyName + ", ");
			}
			commandTextBuilder.Length -= 2;

			commandTextBuilder.Append(") VALUES (");

			foreach (var keyValue in parameters)
			{
				commandTextBuilder.Append(keyValue.DbParameter.ParameterName + ", ");
			}
			commandTextBuilder.Length -= 2;
			commandTextBuilder.Append(");");
		}

		private void AppendUpdateCommandTextForType(PersistenceStorageObjectParameter[] parameters, string tableFullName, StringBuilder commandTextBuilder)
		{
			commandTextBuilder.Append("UPDATE " + tableFullName + " SET ");
			foreach (var keyValue in parameters)
			{
				if (keyValue.PropertyName != "ID")
				{
					commandTextBuilder.Append(keyValue.PropertyName + " = " + keyValue.DbParameter.ParameterName + ", ");
				}
			}
			commandTextBuilder.Length -= 2;
			commandTextBuilder.Append(" WHERE ID = " + parameters.First().DbParameter + ";");
		}

		/// <summary>
		/// In rare case when updating entity that has only ID property, the update command has no business value and probably will not occur in practice.
		/// For consistency, this command still executes an SQL query. It provides consistent error handling in case the record does not
		/// exists in database, to match the behavior of other regular entities.
		/// </summary>
		private void AppendEmptyUpdateCommandTextForType(PersistenceStorageObjectParameter[] parameters, string tableFullName, StringBuilder commandTextBuilder)
		{
			commandTextBuilder.Append("UPDATE " + tableFullName + " SET ID = ID WHERE ID = " + parameters.First().DbParameter + ";");
		}
	}
}
