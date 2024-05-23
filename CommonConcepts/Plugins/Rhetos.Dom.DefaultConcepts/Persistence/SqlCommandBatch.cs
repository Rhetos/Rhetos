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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Data.SqlClient;
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
        private readonly DatabaseOptions _databaseOptions;
        private readonly CommonConceptsRuntimeOptions _commonConceptsRuntimeOptions;

        public SqlCommandBatch(
			IPersistenceTransaction persistenceTransaction,
			IPersistenceStorageObjectMappings persistenceMappingConfiguration,
			DatabaseOptions databaseOptions,
			CommonConceptsRuntimeOptions commonConceptsRuntimeOptions)
		{
			_persistenceTransaction = persistenceTransaction;
			_persistenceMappingConfiguration = persistenceMappingConfiguration;
            _databaseOptions = databaseOptions;
            _commonConceptsRuntimeOptions = commonConceptsRuntimeOptions;
        }

		public int Execute(PersistenceStorageCommandType commandType, Type entityType, IReadOnlyCollection<IEntity> entities)
		{
			if (entities.Count == 0)
				return 0;

			var commandParameters = new List<DbParameter>();
			var commandTextBuilder = new StringBuilder();

			AppendSqlCommands(commandType, entityType, entities, commandParameters, commandTextBuilder);

			using (var dbCommand = _persistenceTransaction.Connection.CreateCommand())
			{
				dbCommand.Transaction = _persistenceTransaction.Transaction;
				dbCommand.CommandTimeout = _databaseOptions.SqlCommandTimeout;
				dbCommand.CommandText = commandTextBuilder.ToString();
				foreach (var parameter in commandParameters)
					dbCommand.Parameters.Add(parameter);
				return dbCommand.ExecuteNonQuery();
			}
		}

        private void AppendSqlCommands(PersistenceStorageCommandType commandType, Type entityType, IReadOnlyCollection<IEntity> entities, List<DbParameter> commandParameters, StringBuilder commandTextBuilder)
        {
            if (commandType == PersistenceStorageCommandType.Insert)
            {
				foreach (var entity in entities)
					AppendInsertCommand(commandParameters, commandTextBuilder, entity, _persistenceMappingConfiguration.GetMapping(entityType), entity == entities.First(), entity == entities.Last());
            }
            else if (commandType == PersistenceStorageCommandType.Update)
            {
                foreach (var entity in entities)
                    AppendUpdateCommand(commandParameters, commandTextBuilder, entity, _persistenceMappingConfiguration.GetMapping(entityType));
            }
            else if (commandType == PersistenceStorageCommandType.Delete)
            {
                foreach (var entity in entities)
                    AppendDeleteCommand(commandParameters, commandTextBuilder, entity, _persistenceMappingConfiguration.GetMapping(entityType), entity == entities.First(), entity == entities.Last());
            }
            else
                throw new ArgumentException($"Unexpected command type '{commandType}'.");
        }

		private void AppendInsertCommand(List<DbParameter> commandParameters, StringBuilder commandTextBuilder, IEntity entity, IPersistenceStorageObjectMapper mapper, bool isFirst, bool isLast)
		{
			var parameters = mapper.GetParameters(entity);
			InitializeAndAppendParameters(commandParameters, parameters);

			if (_commonConceptsRuntimeOptions.SqlCommandBatchSeparateQueries)
			{
				AppendInsertPartColumns(parameters, mapper.GetTableName(), commandTextBuilder);
				AppendInsertPartValues(parameters, commandTextBuilder);
                commandTextBuilder.Append(';');
            }
			else
			{
                // Grouping multiple records into a single query "INSERT INTO .. VALUES (1..), (2..), (3..), ..;"
                // resulted with 30x performance improvements on when inserting 10000 records into a test table
                // (MS SQL, Common.Role, with Logging and 2 cascade-delete details),
                // with CommonConceptsRuntimeOptions.SaveSqlCommandBatchSize set to 20,
                // compared to a query for each record "INSERT INTO .. VALUES (1..); INSERT INTO .. VALUES (2..); INSERT INTO .. (VALUES 3..); ..."
                // when CommonConceptsRuntimeOptions.SqlCommandBatchSeparateQueries is set to true.

                if (isFirst)
					AppendInsertPartColumns(parameters, mapper.GetTableName(), commandTextBuilder);
				else
					commandTextBuilder.Append(',');

                AppendInsertPartValues(parameters, commandTextBuilder);

                if (isLast)
                    commandTextBuilder.Append(';');
            }
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

		private void AppendDeleteCommand(List<DbParameter> commandParameters, StringBuilder commandTextBuilder, IEntity entity, IPersistenceStorageObjectMapper mapper, bool isFirst, bool isLast)
		{
            var parameter = new PersistenceStorageObjectParameter("ID", new SqlParameter("", System.Data.SqlDbType.UniqueIdentifier) { Value = entity.ID });
            InitializeAndAppendParameters(commandParameters, new[] { parameter });
            var entityName = mapper.GetTableName();

            if (_commonConceptsRuntimeOptions.SqlCommandBatchSeparateQueries || (isFirst == true && isLast == true))
			{
                // Sending a parameter instead of the GUID constant may improve DB performance because of execution plan reuse,
                // but on the test that deletes 10000 records from a test table (MS SQL, Common.Role, with Logging and 2 cascade-delete details),
                // literal value performed more then 2x faster.
                // For more records with "WHERE ID IN", parametrized values were faster.

                commandTextBuilder.Append($@"DELETE FROM {entityName} WHERE ID = {parameter.DbParameter};");
            }
			else
			{
                if (isFirst)
					commandTextBuilder.Append($"DELETE FROM {entityName} WHERE ID IN (");
				else
                    commandTextBuilder.Append(", ");

                commandTextBuilder.Append(parameter.DbParameter);

                if (isLast)
                    commandTextBuilder.Append($");");
            }
		}

        private void AppendInsertPartColumns(PersistenceStorageObjectParameter[] parameters, string tableFullName, StringBuilder commandTextBuilder)
        {
            commandTextBuilder.Append("INSERT INTO " + tableFullName + " (");
            foreach (var keyValue in parameters)
            {
                commandTextBuilder.Append(keyValue.PropertyName + ", ");
            }
            commandTextBuilder.Length -= 2;

            commandTextBuilder.Append(") VALUES ");
        }

        private void AppendInsertPartValues(PersistenceStorageObjectParameter[] parameters, StringBuilder commandTextBuilder)
        {
            commandTextBuilder.Append('(');

            foreach (var keyValue in parameters)
            {
                commandTextBuilder.Append(keyValue.DbParameter.ParameterName + ", ");
            }
            commandTextBuilder.Length -= 2;
            commandTextBuilder.Append(')');
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
