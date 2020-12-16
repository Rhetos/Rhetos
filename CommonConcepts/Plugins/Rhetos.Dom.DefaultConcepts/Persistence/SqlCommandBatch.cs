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
using System.Linq;
using System.Collections.Generic;
using System.Data.Common;
using Rhetos.Persistence;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
	public class SqlCommandBatch : IPersistenceStorageCommandBatch
	{
		private int _batchNumber;

		private List<Command> _commands;

		private IPersistenceTransaction _persistenceTransaction;

		private IPersistenceStorageObjectMappings _persistenceMappingConfiguration;

		private Action<int, DbCommand> _afterCommandExecution;

		public SqlCommandBatch(
			IPersistenceTransaction persistenceTransaction,
			IPersistenceStorageObjectMappings persistenceMappingConfiguration,
			int batchNumber,
			Action<int, DbCommand> afterCommandExecution)
		{
			_batchNumber = batchNumber;
			_commands = new List<Command>();
			_persistenceTransaction = persistenceTransaction;
			_persistenceMappingConfiguration = persistenceMappingConfiguration;
			_afterCommandExecution = afterCommandExecution;
		}

		public IPersistenceStorageCommandBatch Add<T>(T entity, PersistenceStorageCommandType commandType) where T : class, IEntity
		{	
			_commands.Add(new Command
			{
				Entity = entity,
				EntityType = typeof(T),
				CommandType = commandType
			});

			return this;
		}

		public int Execute()
		{
			var numberOfAffectedRows = 0;

			var commandParameters = new List<DbParameter>();
			var commandTextBuilder = new StringBuilder();
			using (var command = _persistenceTransaction.Connection.CreateCommand())
			{
				command.Transaction = _persistenceTransaction.Transaction;

				var numberOfBatchedCommand = 0;
				for (int i = 0; i < _commands.Count; i++)
				{
					AppendCommand(_commands[i], commandParameters, commandTextBuilder);
					numberOfBatchedCommand++;

					if (numberOfBatchedCommand == _batchNumber || i == _commands.Count - 1)
					{
						numberOfAffectedRows += ExecuteNonQueryAndClearCommand(command, commandParameters, commandTextBuilder);
						numberOfBatchedCommand = 0;
					}
				}
				_commands.Clear();
			}

			return numberOfAffectedRows;
		}

		private int ExecuteNonQueryAndClearCommand(DbCommand command, List<DbParameter> commandParameters, StringBuilder commandTextBuilder)
		{
			var numberOfAffectedRows = 0;
			if (commandTextBuilder.Length != 0)
			{
				command.CommandText = commandTextBuilder.ToString();
				foreach(var parameter in commandParameters)
					command.Parameters.Add(parameter);
				numberOfAffectedRows = command.ExecuteNonQuery();
				_afterCommandExecution?.Invoke(numberOfAffectedRows, command);
			}
			command.CommandText = "";
			command.Parameters.Clear();
			commandParameters.Clear();
			commandTextBuilder.Clear();
			return numberOfAffectedRows;
		}

		private void AppendCommand(Command comand, List<DbParameter> commandParameters, StringBuilder commandTextBuilder)
		{
			if (comand.CommandType == PersistenceStorageCommandType.Insert)
			{
				AppendInsertCommand(commandParameters, commandTextBuilder, comand.Entity, _persistenceMappingConfiguration.GetMapping(comand.EntityType));
			}
			if (comand.CommandType == PersistenceStorageCommandType.Update)
			{
				AppendUpdateCommand(commandParameters, commandTextBuilder, comand.Entity, _persistenceMappingConfiguration.GetMapping(comand.EntityType));
			}
			if (comand.CommandType == PersistenceStorageCommandType.Delete)
			{
				AppendDeleteCommand(commandParameters, commandTextBuilder, comand.Entity, _persistenceMappingConfiguration.GetMapping(comand.EntityType));
			}
		}

		private void AppendInsertCommand(List<DbParameter> commandParameters, StringBuilder commandTextBuilder, IEntity entity, IPersistenceStorageObjectMapper mapper)
		{
			var parameters = mapper.GetParameters(entity);
			InitializeAndAppendParameters(commandParameters, parameters);
			AppendInsertCommandTextForType(parameters, mapper.GetTableName(), commandTextBuilder);
		}

		private void InitializeAndAppendParameters(List<DbParameter> commandParameters, Dictionary<string, DbParameter> parameters)
		{
			var index = commandParameters.Count;
			foreach (var item in parameters)
			{
				item.Value.ParameterName = "@" + index;
				index++;
			}
			commandParameters.AddRange(parameters.Select(x => x.Value));
		}

		private void AppendUpdateCommand(List<DbParameter> commandParameters, StringBuilder commandTextBuilder, IEntity entity, IPersistenceStorageObjectMapper mapper)
		{
			var parameters = mapper.GetParameters(entity);
			InitializeAndAppendParameters(commandParameters, parameters);
			if (parameters.Count() > 1) // If entity has other properties besides ID.
				AppendUpdateCommandTextForType(parameters, mapper.GetTableName(), commandTextBuilder);
			else
				AppendEmptyUpdateCommandTextForType(parameters, mapper.GetTableName(), commandTextBuilder);
		}

		private void AppendDeleteCommand(List<DbParameter> commandParameters, StringBuilder commandTextBuilder, IEntity entity, IPersistenceStorageObjectMapper mapper)
		{
			var entityName = mapper.GetTableName();
			commandTextBuilder.Append($@"DELETE FROM {entityName} WHERE ID = '{entity.ID}';");
		}

		private void AppendInsertCommandTextForType(Dictionary<string, DbParameter> parameters, string tableFullName, StringBuilder commandTextBuilder)
		{
			commandTextBuilder.Append("INSERT INTO " + tableFullName + " (");
			foreach (var keyValue in parameters)
			{
				commandTextBuilder.Append(keyValue.Key + ", ");
			}
			commandTextBuilder.Length = commandTextBuilder.Length - 2;

			commandTextBuilder.Append(") VALUES (");

			foreach (var keyValue in parameters)
			{
				commandTextBuilder.Append(keyValue.Value.ParameterName + ", ");
			}
			commandTextBuilder.Length = commandTextBuilder.Length - 2;
			commandTextBuilder.Append(");");
		}

		private void AppendUpdateCommandTextForType(Dictionary<string, DbParameter> parameters, string tableFullName, StringBuilder commandTextBuilder)
		{
			commandTextBuilder.Append("UPDATE " + tableFullName + " SET ");
			foreach (var keyValue in parameters)
			{
				if (keyValue.Key != "ID")
				{
					commandTextBuilder.Append(keyValue.Key + " = " + keyValue.Value.ParameterName + ", ");
				}
			}
			commandTextBuilder.Length = commandTextBuilder.Length - 2;
			commandTextBuilder.Append(" WHERE ID = " + parameters["ID"] + ";");
		}

		/// <summary>
		/// In rare case when updating entity that has only ID property, the update command has no business value and probably will not occur in practice.
		/// For consistency, this command still executes an SQL query. It provides consistent error handling in case the record does not
		/// exists in database, to match the behavior of other regular entities.
		/// </summary>
		private void AppendEmptyUpdateCommandTextForType(Dictionary<string, DbParameter> parameters, string tableFullName, StringBuilder commandTextBuilder)
		{
			commandTextBuilder.Append("UPDATE " + tableFullName + " SET ID = ID WHERE ID = " + parameters["ID"] + ";");
		}

		private class Command
		{
			public IEntity Entity { get; set; }
			public Type EntityType { get; set; }
			public PersistenceStorageCommandType CommandType { get; set; }
		}
	}
}
