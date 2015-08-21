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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.CommonConcepts.Test.Mocks
{
    class RepositoryMock<TEntityInterface, TEntity> : IRepository, IQueryableRepository<TEntityInterface>, IWritableRepository<TEntity>
        where TEntityInterface : class, IEntity
        where TEntity : class, TEntityInterface
    {
        IEnumerable<TEntity> _items;

        public RepositoryMock(IEnumerable<TEntity> items = null)
        {
            _items = items;
        }

        public IQueryable<TEntityInterface> Query(object parameter, Type parameterType)
        {
            if (parameterType == typeof(FilterAll))
                return Query();
            throw new NotImplementedException();
        }

        public IQueryable<TEntityInterface> Query()
        {
            return (IQueryable<TEntityInterface>)_items.AsQueryable();
        }

        public List<object> InsertedGroups = new List<object>();
        public List<object> UpdatedGroups = new List<object>();
        public List<object> DeletedGroups = new List<object>();
        public StringBuilder Log = new StringBuilder();

        public void Save(IEnumerable<TEntity> insertedNew, IEnumerable<TEntity> updatedNew, IEnumerable<TEntity> deletedIds, bool checkUserPermissions = false)
        {
            if (insertedNew != null) InsertedGroups.Add(insertedNew);
            if (updatedNew != null) UpdatedGroups.Add(updatedNew);
            if (deletedIds != null) DeletedGroups.Add(deletedIds);

            if (insertedNew == null) insertedNew = new TEntity[] { };
            if (updatedNew == null) updatedNew = new TEntity[] { };
            if (deletedIds == null) deletedIds = new TEntity[] { };

            AppendLog("DELETE", deletedIds);
            AppendLog("UPDATE", updatedNew);
            AppendLog("INSERT", insertedNew);

            foreach (var item in insertedNew)
                if (item.ID == Guid.Empty)
                    item.ID = Guid.NewGuid();

            // Force reading of all instances:
            Guid newGuid = Guid.NewGuid();
            foreach (int test in Enumerable.Range(1, 2))
                foreach (var item in insertedNew.Concat(updatedNew).Concat(deletedIds))
                    if (newGuid == item.ID)
                        throw new ApplicationException("This exception is not reachable");
        }

        private void AppendLog(string message, IEnumerable<TEntity> items)
        {
            if (items.Count() > 0)
            {
                if (Log.Length > 0)
                    Log.Append(", ");

                string report = message + " " + string.Join(", ", items.Select(item => item.ToString()).OrderBy(x => x));

                Console.WriteLine(report);
                Log.Append(report);
            }
        }
    }
}
