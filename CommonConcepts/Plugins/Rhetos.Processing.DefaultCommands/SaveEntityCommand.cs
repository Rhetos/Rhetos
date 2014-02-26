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

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    [ExportMetadata(MefProvider.Implements, typeof(SaveEntityCommandInfo))]
    public class SaveEntityCommand : ICommandImplementation
    {
        private readonly IIndex<string, IWritableRepository> _writableRepositories;

        public SaveEntityCommand(IIndex<string, IWritableRepository> writableRepositories)
        {
            _writableRepositories = writableRepositories;
        }

        public CommandResult Execute(ICommandInfo info)
        {
            var saveInfo = info as SaveEntityCommandInfo;

            if (saveInfo == null)
                return CommandResult.Fail("CommandInfo does not implement SaveEntityCommandInfo");

            var repository = _writableRepositories[saveInfo.Entity];
            repository.Save(saveInfo.DataToInsert, saveInfo.DataToUpdate, saveInfo.DataToDelete, true);

            return new CommandResult
            {
                Message = "Comand executed",
                Success = true
            };
        }
    }
}
