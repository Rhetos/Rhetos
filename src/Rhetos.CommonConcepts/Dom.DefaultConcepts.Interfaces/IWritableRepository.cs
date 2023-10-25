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

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IWritableRepository<in TEntity> : IRepository
        where TEntity : class
    {
        void Save(IEnumerable<TEntity> insertedNew, IEnumerable<TEntity> updatedNew, IEnumerable<TEntity> deletedIds, bool checkUserPermissions = false);
    }

    public static class WritableRepositoryExtensions
    {
        public static void Insert<TEntity>(this IWritableRepository<TEntity> repository, IEnumerable<TEntity> insertNew, bool checkUserPermissions = false)
            where TEntity : class
        {
            repository.Save(insertNew, null, null, checkUserPermissions);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public static void Insert<TEntity>(this IWritableRepository<TEntity> repository, params TEntity[] insertNew)
            where TEntity : class
        {
            repository.Save(insertNew, null, null, false);
        }

        public static void Update<TEntity>(this IWritableRepository<TEntity> repository, IEnumerable<TEntity> updateNew, bool checkUserPermissions = false)
            where TEntity : class
        {
            repository.Save(null, updateNew, null, checkUserPermissions);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public static void Update<TEntity>(this IWritableRepository<TEntity> repository, params TEntity[] updateNew)
            where TEntity : class
        {
            repository.Save(null, updateNew, null, false);
        }

        public static void Delete<TEntity>(this IWritableRepository<TEntity> repository, IEnumerable<TEntity> deleteIds, bool checkUserPermissions = false)
            where TEntity : class
        {
            repository.Save(null, null, deleteIds, checkUserPermissions);
        }

        /// <summary>checkUserPermissions is set to false.</summary>
        public static void Delete<TEntity>(this IWritableRepository<TEntity> repository, params TEntity[] deleteIds)
            where TEntity : class
        {
            repository.Save(null, null, deleteIds, false);
        }
    }
}
