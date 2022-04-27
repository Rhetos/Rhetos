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
using System.ComponentModel.Composition;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    public class SaveEntityCommand : ICommandImplementation<SaveEntityCommandInfo, object>
    {
        private readonly IPersistenceTransaction _persistenceTransaction;
        private readonly ServerCommandsUtility _serverCommandsUtility;

        public SaveEntityCommand(
            IPersistenceTransaction persistenceTransaction,
            ServerCommandsUtility serverCommandsUtility)
        {
            _persistenceTransaction = persistenceTransaction;
            _serverCommandsUtility = serverCommandsUtility;
        }

        public object Execute(SaveEntityCommandInfo saveInfo)
        {
            if (saveInfo.Entity == null)
                throw new ClientException("Invalid SaveEntityCommand argument: Entity is not set.");

            var entityUtility = _serverCommandsUtility.ForEntity(saveInfo.Entity);

            // We need to check permissions for *deleted* items before actually deleting them,
            // *updated* items both before AND after they are updated,
            // and *insert* items after the insert.

            if (!entityUtility.UserHasWriteRowPermissionsBeforeSave(saveInfo.DataToDelete, saveInfo.DataToUpdate))
            {
                _persistenceTransaction.DiscardOnDispose();
                throw new UserException("You are not authorized to write some or all of the provided data. Insufficient permissions to modify the existing data.", $"DataStructure:{saveInfo.Entity},Validation:RowPermissionsWrite");
            }

            entityUtility.GenericRepository.Save(saveInfo.DataToInsert, saveInfo.DataToUpdate, saveInfo.DataToDelete, true);

            if (!entityUtility.UserHasWriteRowPermissionsAfterSave(saveInfo.DataToInsert, saveInfo.DataToUpdate))
            {
                _persistenceTransaction.DiscardOnDispose();
                throw new UserException("You are not authorized to write some or all of the provided data. Insufficient permissions to apply the new data.", $"DataStructure:{saveInfo.Entity}.");
            }

            return null;
        }
    }
}
