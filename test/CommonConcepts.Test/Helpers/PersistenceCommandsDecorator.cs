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

using Rhetos.Dom.DefaultConcepts;
using System.Collections.Generic;

namespace CommonConcepts.Test
{
    public class PersistenceCommandsDecorator : IPersistenceStorageCommandBatch
    {
        private readonly IPersistenceStorageCommandBatch _baseCommandExecuter;
        private readonly Log _persistenceCommandsLog;

        public PersistenceCommandsDecorator(IPersistenceStorageCommandBatch baseCommandExecuter, Log persistenceCommandsLog)
        {
            _baseCommandExecuter = baseCommandExecuter;
            _persistenceCommandsLog = persistenceCommandsLog;
        }

        public int Execute(IList<PersistenceStorageCommand> commands)
        {
            _persistenceCommandsLog.Add(commands);
            return _baseCommandExecuter.Execute(commands);
        }

        public class Log : List<IList<PersistenceStorageCommand>>
        {
        }
    }
}