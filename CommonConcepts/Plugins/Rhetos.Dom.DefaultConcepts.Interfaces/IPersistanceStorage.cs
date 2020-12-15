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
using System.Data.Common;

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IPersistanceStorage
    {
        Action<int, DbCommand> AfterCommandExecution { get; set; }

        IPersistanceStorageCommandBatch StartBatch();

        int Insert<TEntity>(IEnumerable<TEntity> toInsert) where TEntity : IEntity;

        int Update<TEntity>(IEnumerable<TEntity> toUpdate) where TEntity : IEntity;

        int Delete<TEntity>(IEnumerable<TEntity> toDelete) where TEntity : IEntity;
    }

    public static class IPersistanceStorageExtensions
    {
        public static int Insert<TEntity>(this IPersistanceStorage persistanceStorage, TEntity toInsert) where TEntity : IEntity
        {
            return persistanceStorage.Insert(toInsert.Yield());
        }

        public static int Update<TEntity>(this IPersistanceStorage persistanceStorage, TEntity toUpdate) where TEntity : IEntity
        {
            return persistanceStorage.Update(toUpdate.Yield());
        }

        public static int Delete<TEntity>(this IPersistanceStorage persistanceStorage, TEntity toDelete) where TEntity : IEntity
        {
            return persistanceStorage.Delete(toDelete.Yield());
        }

        private static IEnumerable<T> Yield<T>(this T item)
        {
            yield return item;
        }
    }
}
