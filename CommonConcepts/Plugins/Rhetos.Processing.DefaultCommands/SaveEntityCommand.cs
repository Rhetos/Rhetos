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

        public SaveEntityCommand(
            IIndex<string, IWritableRepository> writableRepositories,
            GenericRepositories genericRepositories,
            IPersistenceTransaction persistenceTransaction)
        {
            _writableRepositories = writableRepositories;
            _genericRepositories = genericRepositories;
            _persistenceTransaction = persistenceTransaction;
        }

        public CommandResult Execute(ICommandInfo commandInfo)
        {
            var saveInfo = commandInfo as SaveEntityCommandInfo;

            if (saveInfo == null)
                return CommandResult.Fail("CommandInfo does not implement SaveEntityCommandInfo");

            if (saveInfo.Entity == null)
                throw new ClientException("Invalid SaveEntityCommand argument: Entity is not set.");

            // we need to check delete permissions before actually deleting items
            var genericRepository = _genericRepositories.GetGenericRepository(saveInfo.Entity);
            bool valid = true;
            if (saveInfo.DataToDelete != null) 
                valid = genericRepository.CheckAllItemsWithinFilter(saveInfo.DataToDelete, RowPermissionsWriteInfo.FilterName);

            if (valid)
            {
                var repository = _writableRepositories[saveInfo.Entity];
                repository.Save(saveInfo.DataToInsert, saveInfo.DataToUpdate, saveInfo.DataToDelete, true);

                var insertUpdateItems = saveInfo.DataToInsert;
                if (insertUpdateItems == null) insertUpdateItems = saveInfo.DataToUpdate;
                else if (saveInfo.DataToUpdate != null) insertUpdateItems = insertUpdateItems.Concat(saveInfo.DataToUpdate).ToArray();

                // we rely that this call will only use IDs of the items, because other data might be dirty
                if (insertUpdateItems != null)
                    valid = genericRepository.CheckAllItemsWithinFilter(insertUpdateItems, RowPermissionsWriteInfo.FilterName);
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
    }
}
