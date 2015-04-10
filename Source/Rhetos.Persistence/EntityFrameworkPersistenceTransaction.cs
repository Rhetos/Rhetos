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

using Rhetos.Logging;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Persistence
{
    public class EntityFrameworkPersistenceTransaction : IPersistenceTransaction
    {
        private readonly ILogger _logger;
        private readonly DbContext _dbContext;

        private bool _disposed;
        private bool _discard;

        public EntityFrameworkPersistenceTransaction(ILogProvider logProvider, DbContext dbContext)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _dbContext = dbContext;
        }

        public void DiscardChanges()
        {
            _discard = true;
        }

        public event Action BeforeClose;

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_discard)
                    Rollback();
                else
                    Commit();
            }

            BeforeClose = null;
            _disposed = true;
        }

        public void CommitAndReconnect()
        {
            if (_disposed)
                throw new FrameworkException("Trying to commit and reconnect a disposed persistence transaction.");
            if (_discard)
                throw new FrameworkException("Trying to commit and reconnect a discarded persistence transaction.");

            Commit();
        }

        private void Commit()
        {
            if (BeforeClose != null)
            {
                BeforeClose();
                BeforeClose = null;
            }

            _dbContext.SaveChanges();
        }

        private void Rollback()
        {
            BeforeClose = null;
            //try
            {
                var tran = _dbContext.Database.CurrentTransaction;
                if (tran != null)
                    tran.Rollback();
            }
            //catch (TransactionException)
            //{
            //}
        }

        public void ClearCache()
        {
            if (_disposed)
                throw new FrameworkException("Trying to clear cache on a disposed persistence transaction.");

            foreach (var item in _dbContext.ChangeTracker.Entries().ToList())
                ((IObjectContextAdapter)_dbContext).ObjectContext.Detach(item.Entity);
        }

        public void ClearCache(object item)
        {
            if (_disposed)
                throw new FrameworkException("Trying to clear an item from the cache on a disposed persistence transaction.");

            ((IObjectContextAdapter)_dbContext).ObjectContext.Detach(item);
        }
    }
}
