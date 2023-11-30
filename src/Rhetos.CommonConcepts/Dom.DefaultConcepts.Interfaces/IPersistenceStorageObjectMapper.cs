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

using System;
using System.Collections.Generic;
using System.Data.Common;

namespace Rhetos.Dom.DefaultConcepts
{
	public interface IPersistenceStorageObjectMapper
	{
        PersistenceStorageObjectParameter[] GetParameters(IEntity entity);

		IEnumerable<Guid> GetDependencies(IEntity entity);

		string GetTableName();
	}

    public struct PersistenceStorageObjectParameter
    {
        public string PropertyName { get; }

        public DbParameter DbParameter { get; }

        public PersistenceStorageObjectParameter(string propertyName, DbParameter dbParameter)
        {
            PropertyName = propertyName;
            DbParameter = dbParameter;
        }
    }
}
