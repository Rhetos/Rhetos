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
using System;
using System.Collections.Generic;

namespace CommonConcepts.Test
{
    public class PersistenceCommandsMock : IPersistenceStorageCommandBatch
    {
        private readonly IPersistenceStorageCommandBatch _baseCommandExecuter;

        public Log CommandsLog { get; }

        public PersistenceCommandsMock(IPersistenceStorageCommandBatch baseCommandExecuter, Log persistenceCommandsLog)
        {
            _baseCommandExecuter = baseCommandExecuter;
            CommandsLog = persistenceCommandsLog;
        }

        public int Execute(PersistenceStorageCommandType commandType, Type entityType, IReadOnlyCollection<IEntity> entities)
        {
            CommandsLog.Add((commandType, entityType, entities));
            return _baseCommandExecuter?.Execute(commandType, entityType, entities) ?? 0;
        }

        public class Log : List<(PersistenceStorageCommandType commandType, Type entityType, IReadOnlyCollection<IEntity> entities)>
        {
        }
    }
}