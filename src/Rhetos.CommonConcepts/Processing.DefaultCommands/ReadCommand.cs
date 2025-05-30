﻿/*
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
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    public class ReadCommand : ICommandImplementation<ReadCommandInfo, ReadCommandResult>
    {
        private readonly ILogger _logger;
        private readonly ServerCommandsUtility _serverCommandsUtility;
        private readonly ApplyFiltersOnClientRead _applyFiltersOnClientRead;
        private readonly CommonConceptsRuntimeOptions _options;

        public ReadCommand(
            ILogProvider logProvider,
            ServerCommandsUtility serverCommandsUtility,
            ApplyFiltersOnClientRead applyFiltersOnClientRead,
            CommonConceptsRuntimeOptions options)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _serverCommandsUtility = serverCommandsUtility;
            _applyFiltersOnClientRead = applyFiltersOnClientRead;
            _options = options;
        }

        public ReadCommandResult Execute(ReadCommandInfo readInfo)
        {
            if (readInfo.DataSource == null)
                throw new ClientException("Invalid ReadCommand argument: Data source is not set.");

            var entityCommandsUtility = _serverCommandsUtility.ForEntity(readInfo.DataSource);

            var result = ReadData(readInfo, entityCommandsUtility.GenericRepository);

            if (result.Records != null && !AlreadyFilteredByRowPermissions(readInfo))
            {
                if (!entityCommandsUtility.UserHasReadRowPermissions(result.Records))
                    throw new UserException(
                        "You are not authorized to access some or all of the data requested.",
                        $"DataStructure:{readInfo.DataSource},Validation:RowPermissionsRead");
            }

            return result;
        }

        /// <summary>
        /// Reads data without row permissions verification.
        /// </summary>
        public ReadCommandResult ReadData(ReadCommandInfo commandInfo, GenericRepository<IEntity> genericRepository)
        {
            if (!commandInfo.ReadRecords && !commandInfo.ReadTotalCount)
                throw new ClientException("Invalid ReadCommand argument: At least one of the properties ReadRecords or ReadTotalCount should be set to true.");

            if (commandInfo.Top < 0)
                throw new ClientException("Invalid ReadCommand argument: Top parameter must not be negative.");

            if (commandInfo.Skip < 0)
                throw new ClientException("Invalid ReadCommand argument: Skip parameter must not be negative.");

            if (commandInfo.DataSource != genericRepository.EntityName)
                throw new FrameworkException(string.Format(
                    "Invalid ExecuteReadCommand arguments: The given ReadCommandInfo ('{0}') does not match the GenericRepository ('{1}').",
                    commandInfo.DataSource, genericRepository.EntityName));

            if (_options.ReadCommandSimpleProperty)
            {
                var notSimpleProperty = commandInfo.Filters?.FirstOrDefault(p => !string.IsNullOrWhiteSpace(p.Property) && CsUtility.GetIdentifierError(p.Property) != null);
                if (notSimpleProperty != null && _options.ReadCommandSimpleProperty)
                    throw new ClientException($"Invalid ReadCommand argument: Only simple properties are supported in generic property filter. ('{notSimpleProperty.Property}')");
            }

            AutoApplyFilters(commandInfo);

            var specificMethod = genericRepository.Reflection.RepositoryReadCommandMethod;
            if (specificMethod != null)
                return (ReadCommandResult)specificMethod.InvokeEx(genericRepository.EntityRepository, commandInfo);
            else
                return GenericRepositoryRead(genericRepository, commandInfo);
        }

        private static ReadCommandResult GenericRepositoryRead(GenericRepository<IEntity> genericRepository, ReadCommandInfo commandInfo)
        {
            bool pagingIsUsed = commandInfo.Top > 0 || commandInfo.Skip > 0;

            object filter = commandInfo.Filters != null && commandInfo.Filters.Length != 0 ? (object)commandInfo.Filters : new FilterAll();
            IEnumerable<IEntity> filtered = genericRepository.Read(filter, filter.GetType(), preferQuery: pagingIsUsed || !commandInfo.ReadRecords);

            IEntity[] resultRecords = null;
            int? totalCount = null;

            if (commandInfo.ReadRecords)
            {
                var sortedAndPaginated = GenericFilterHelper.SortAndPaginate(genericRepository.Reflection.AsQueryable(filtered), commandInfo);
                resultRecords = (IEntity[])genericRepository.Reflection.ToArrayOfEntity(sortedAndPaginated);
            }

            if (commandInfo.ReadTotalCount)
                if (pagingIsUsed)
                    totalCount = SmartCount(filtered);
                else
                    totalCount = resultRecords != null ? resultRecords.Length : SmartCount(filtered);

            return new ReadCommandResult
            {
                Records = resultRecords,
                TotalCount = totalCount
            };
        }

        private void AutoApplyFilters(ReadCommandInfo commandInfo)
        {
            if (_applyFiltersOnClientRead.TryGetValue(commandInfo.DataSource, out List<ApplyFilterWhere> applyFilters))
            {
                commandInfo.Filters ??= Array.Empty<FilterCriteria>();

                var newFilters = applyFilters
                    .Where(applyFilter => applyFilter.Where == null || applyFilter.Where(commandInfo))
                    .Where(applyFilter => !commandInfo.Filters.Any(existingFilter => GenericFilterHelper.EqualsSimpleFilter(existingFilter, applyFilter.FilterName)))
                    .Select(applyFilter => new FilterCriteria { Filter = applyFilter.FilterName })
                    .ToList();

                _logger.Trace(() => "AutoApplyFilters: " + string.Join(", ", newFilters.Select(f => f.Filter)));

                commandInfo.Filters = commandInfo.Filters.Concat(newFilters).ToArray();
            }
        }

        private static int SmartCount(IEnumerable<IEntity> items)
        {
            return items is IQueryable<IEntity> query
                ? query.Count()
                : items.Count();
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
