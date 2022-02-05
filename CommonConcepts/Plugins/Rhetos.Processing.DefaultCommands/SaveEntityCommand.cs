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
using Rhetos.Persistence;
using System;
using System.ComponentModel.Composition;
using System.Linq;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    public class SaveEntityCommand : ICommandImplementation<SaveEntityCommandInfo, object>
    {
        private readonly GenericRepositories _genericRepositories;
        private readonly IPersistenceTransaction _persistenceTransaction;
        private readonly ServerCommandsUtility _serverCommandsUtility;

        public SaveEntityCommand(
            GenericRepositories genericRepositories,
            IPersistenceTransaction persistenceTransaction,
            ServerCommandsUtility serverCommandsUtility)
        {
            _genericRepositories = genericRepositories;
            _persistenceTransaction = persistenceTransaction;
            _serverCommandsUtility = serverCommandsUtility;
        }

        public object Execute(SaveEntityCommandInfo saveInfo)
        {
            if (saveInfo.Entity == null)
                throw new ClientException("Invalid SaveEntityCommand argument: Entity is not set.");

            // We need to check delete permissions before actually deleting items 
            // and update items before AND after they are updated.
            var genericRepository = _genericRepositories.GetGenericRepository(saveInfo.Entity);

            var updateDeleteItems = ConcatenateNullable(saveInfo.DataToDelete, saveInfo.DataToUpdate);
            if (updateDeleteItems != null)
                if (!_serverCommandsUtility.CheckAllItemsWithinFilter(updateDeleteItems, typeof(Common.RowPermissionsWriteItems), genericRepository))
                {
                    _persistenceTransaction.DiscardOnDispose();
                    Guid? missingId;
                    if (_serverCommandsUtility.MissingItemId(saveInfo.DataToDelete, genericRepository, out missingId))
                        throw new ClientException($"Deleting a record that does not exist in database. DataStructure={saveInfo.Entity}, ID={missingId}");
                    else if (_serverCommandsUtility.MissingItemId(saveInfo.DataToUpdate, genericRepository, out missingId))
                        throw new ClientException($"Updating a record that does not exist in database. DataStructure={saveInfo.Entity}, ID={missingId}");
                    else
                        throw new UserException("You are not authorized to write some or all of the provided data. Insufficient permissions to modify the existing data.", $"DataStructure:{saveInfo.Entity},Validation:RowPermissionsWrite");
                }

            genericRepository.Save(saveInfo.DataToInsert, saveInfo.DataToUpdate, saveInfo.DataToDelete, true);

            var insertUpdateItems = ConcatenateNullable(saveInfo.DataToInsert, saveInfo.DataToUpdate);
            // We rely that this call will only use IDs of the items, because other data might be dirty.
            if (insertUpdateItems != null)
                if (!_serverCommandsUtility.CheckAllItemsWithinFilter(insertUpdateItems, typeof(Common.RowPermissionsWriteItems), genericRepository))
                {
                    _persistenceTransaction.DiscardOnDispose();
                    throw new UserException("You are not authorized to write some or all of the provided data. Insufficient permissions to apply the new data.", $"DataStructure:{saveInfo.Entity}.");
                }

            return null;
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
