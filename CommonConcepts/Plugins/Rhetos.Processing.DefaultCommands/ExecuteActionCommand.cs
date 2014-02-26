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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Autofac.Features.Indexed;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    [ExportMetadata(MefProvider.Implements, typeof(ExecuteActionCommandInfo))]
    public class ExecuteActionCommand : ICommandImplementation
    {
        private readonly IIndex<string, IActionRepository> _actionIndex;

        public ExecuteActionCommand(IIndex<string, IActionRepository> actionIndex)
        {
            _actionIndex = actionIndex;
        }

        public CommandResult Execute(ICommandInfo commandInfo)
        {
            var info = (ExecuteActionCommandInfo)commandInfo;

            string actionName = info.Action.GetType().FullName;
            IActionRepository actionRepository = _actionIndex[actionName];
            actionRepository.Execute(info.Action);

            return new CommandResult
            {
                Data = null,
                Message = "Command executed",
                Success = true
            };
        }
    }
}