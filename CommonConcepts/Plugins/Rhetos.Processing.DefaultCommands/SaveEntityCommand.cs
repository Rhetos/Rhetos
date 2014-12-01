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
using Autofac.Features.Indexed;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System.Diagnostics.Contracts;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    [ExportMetadata(MefProvider.Implements, typeof(SaveEntityCommandInfo))]
    public class SaveEntityCommand : ICommandImplementation
    {
        private readonly IIndex<string, IWritableRepository> _writableRepositories;
        private readonly GenericRepositories _genericRepositories;
        private readonly IPersistenceTransaction _persistenceTransaction;
        private readonly ServerCommandsUtility _serverCommandsUtility;

        public SaveEntityCommand(
            IIndex<string, IWritableRepository> writableRepositories,
            GenericRepositories genericRepositories,
            IPersistenceTransaction persistenceTransaction,
            ServerCommandsUtility serverCommandsUtility)
        {
            _writableRepositories = writableRepositories;
            _genericRepositories = genericRepositories;
            _persistenceTransaction = persistenceTransaction;
            _serverCommandsUtility = serverCommandsUtility;
        }

        public CommandResult Execute(ICommandInfo commandInfo)
        {
            var saveInfo = commandInfo as SaveEntityCommandInfo;

            if (saveInfo == null)
                return CommandResult.Fail("CommandInfo does not implement SaveEntityCommandInfo");

            if (saveInfo.Entity == null)
                throw new ClientException("Invalid SaveEntityCommand argument: Entity is not set.");

            // We need to check delete permissions before actually deleting items 
            // and update items before AND after they are updated.
            var genericRepository = _genericRepositories.GetGenericRepository(saveInfo.Entity);
            bool valid = true;

            var updateDeleteItems = ConcatenateNullable(saveInfo.DataToDelete, saveInfo.DataToUpdate);
            if (updateDeleteItems != null)
                valid = _serverCommandsUtility.CheckAllItemsWithinFilter(updateDeleteItems, RowPermissionsWriteInfo.FilterName, genericRepository);

            if (valid)
            {
                var repository = _writableRepositories[saveInfo.Entity];
                repository.Save(saveInfo.DataToInsert, saveInfo.DataToUpdate, saveInfo.DataToDelete, true);

                var insertUpdateItems = ConcatenateNullable(saveInfo.DataToInsert, saveInfo.DataToUpdate);
                // We rely that this call will only use IDs of the items, because other data might be dirty.
                if (insertUpdateItems != null)
                    valid = _serverCommandsUtility.CheckAllItemsWithinFilter(insertUpdateItems, RowPermissionsWriteInfo.FilterName, genericRepository);
            }

            if (!valid)
            {
                _persistenceTransaction.DiscardChanges();
                throw new UserException("Insufficient permissions to write some or all of the data.", "DataStructure:" + saveInfo.Entity + ".");
            }
            
            return new CommandResult
            {
                Message = "Comand executed",
                Success = true
            };
        }

        private static IEntity[] ConcatenateNullable(IEntity[] a, IEntity[] b)
        {
            if (a != null && a.Length > 0)
                if (b != null && b.Length > 0)
                    return a.Concat(b).ToArray();
                else
                    return a;
            else
                if (b != null && b.Length > 0)
                    return b;
                else
                    return null;
        }
    }
}
