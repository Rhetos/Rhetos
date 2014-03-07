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

namespace Rhetos.CommonConcepts.Test.Mocks
{
    class NHibernateSessionStub : NHibernate.ISession
    {
        public NHibernate.EntityMode ActiveEntityMode
        {
            get { throw new NotImplementedException(); }
        }

        public NHibernate.ITransaction BeginTransaction(System.Data.IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ITransaction BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public NHibernate.CacheMode CacheMode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void CancelQuery()
        {
        }

        public void Clear()
        {
        }

        public System.Data.IDbConnection Close()
        {
            throw new NotImplementedException();
        }

        public System.Data.IDbConnection Connection
        {
            get { throw new NotImplementedException(); }
        }

        public bool Contains(object obj)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ICriteria CreateCriteria(string entityName, string alias)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ICriteria CreateCriteria(string entityName)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ICriteria CreateCriteria(Type persistentClass, string alias)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ICriteria CreateCriteria(Type persistentClass)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ICriteria CreateCriteria<T>(string alias) where T : class
        {
            throw new NotImplementedException();
        }

        public NHibernate.ICriteria CreateCriteria<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public NHibernate.IQuery CreateFilter(object collection, string queryString)
        {
            throw new NotImplementedException();
        }

        public NHibernate.IMultiCriteria CreateMultiCriteria()
        {
            throw new NotImplementedException();
        }

        public NHibernate.IMultiQuery CreateMultiQuery()
        {
            throw new NotImplementedException();
        }

        public NHibernate.IQuery CreateQuery(string queryString)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ISQLQuery CreateSQLQuery(string queryString)
        {
            throw new NotImplementedException();
        }

        public bool DefaultReadOnly
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public int Delete(string query, object[] values, NHibernate.Type.IType[] types)
        {
            throw new NotImplementedException();
        }

        public int Delete(string query, object value, NHibernate.Type.IType type)
        {
            throw new NotImplementedException();
        }

        public int Delete(string query)
        {
            throw new NotImplementedException();
        }

        public void Delete(string entityName, object obj)
        {
        }

        public void Delete(object obj)
        {
        }

        public void DisableFilter(string filterName)
        {
        }

        public System.Data.IDbConnection Disconnect()
        {
            throw new NotImplementedException();
        }

        public NHibernate.IFilter EnableFilter(string filterName)
        {
            throw new NotImplementedException();
        }

        public void Evict(object obj)
        {
        }

        public void Flush()
        {
        }

        public NHibernate.FlushMode FlushMode
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public T Get<T>(object id, NHibernate.LockMode lockMode)
        {
            throw new NotImplementedException();
        }

        public T Get<T>(object id)
        {
            throw new NotImplementedException();
        }

        public object Get(string entityName, object id)
        {
            throw new NotImplementedException();
        }

        public object Get(Type clazz, object id, NHibernate.LockMode lockMode)
        {
            throw new NotImplementedException();
        }

        public object Get(Type clazz, object id)
        {
            throw new NotImplementedException();
        }

        public NHibernate.LockMode GetCurrentLockMode(object obj)
        {
            throw new NotImplementedException();
        }

        public NHibernate.IFilter GetEnabledFilter(string filterName)
        {
            throw new NotImplementedException();
        }

        public string GetEntityName(object obj)
        {
            throw new NotImplementedException();
        }

        public object GetIdentifier(object obj)
        {
            throw new NotImplementedException();
        }

        public NHibernate.IQuery GetNamedQuery(string queryName)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ISession GetSession(NHibernate.EntityMode entityMode)
        {
            throw new NotImplementedException();
        }

        public NHibernate.Engine.ISessionImplementor GetSessionImplementation()
        {
            throw new NotImplementedException();
        }

        public bool IsConnected
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsDirty()
        {
            throw new NotImplementedException();
        }

        public bool IsOpen
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsReadOnly(object entityOrProxy)
        {
            throw new NotImplementedException();
        }

        public void Load(object obj, object id)
        {
        }

        public object Load(string entityName, object id)
        {
            throw new NotImplementedException();
        }

        public T Load<T>(object id)
        {
            throw new NotImplementedException();
        }

        public T Load<T>(object id, NHibernate.LockMode lockMode)
        {
            throw new NotImplementedException();
        }

        public object Load(Type theType, object id)
        {
            throw new NotImplementedException();
        }

        public object Load(string entityName, object id, NHibernate.LockMode lockMode)
        {
            throw new NotImplementedException();
        }

        public object Load(Type theType, object id, NHibernate.LockMode lockMode)
        {
            throw new NotImplementedException();
        }

        public void Lock(string entityName, object obj, NHibernate.LockMode lockMode)
        {
        }

        public void Lock(object obj, NHibernate.LockMode lockMode)
        {
        }

        public T Merge<T>(string entityName, T entity) where T : class
        {
            throw new NotImplementedException();
        }

        public T Merge<T>(T entity) where T : class
        {
            throw new NotImplementedException();
        }

        public object Merge(string entityName, object obj)
        {
            throw new NotImplementedException();
        }

        public object Merge(object obj)
        {
            throw new NotImplementedException();
        }

        public void Persist(string entityName, object obj)
        {
        }

        public void Persist(object obj)
        {
        }

        public NHibernate.IQueryOver<T, T> QueryOver<T>(string entityName, System.Linq.Expressions.Expression<Func<T>> alias) where T : class
        {
            throw new NotImplementedException();
        }

        public NHibernate.IQueryOver<T, T> QueryOver<T>(string entityName) where T : class
        {
            throw new NotImplementedException();
        }

        public NHibernate.IQueryOver<T, T> QueryOver<T>(System.Linq.Expressions.Expression<Func<T>> alias) where T : class
        {
            throw new NotImplementedException();
        }

        public NHibernate.IQueryOver<T, T> QueryOver<T>() where T : class
        {
            throw new NotImplementedException();
        }

        public void Reconnect(System.Data.IDbConnection connection)
        {
        }

        public void Reconnect()
        {
        }

        public void Refresh(object obj, NHibernate.LockMode lockMode)
        {
        }

        public void Refresh(object obj)
        {
        }

        public void Replicate(string entityName, object obj, NHibernate.ReplicationMode replicationMode)
        {
        }

        public void Replicate(object obj, NHibernate.ReplicationMode replicationMode)
        {
        }

        public object Save(string entityName, object obj)
        {
            throw new NotImplementedException();
        }

        public void Save(object obj, object id)
        {
        }

        public object Save(object obj)
        {
            throw new NotImplementedException();
        }

        public void SaveOrUpdate(string entityName, object obj)
        {
        }

        public void SaveOrUpdate(object obj)
        {
        }

        public object SaveOrUpdateCopy(object obj, object id)
        {
            throw new NotImplementedException();
        }

        public object SaveOrUpdateCopy(object obj)
        {
            throw new NotImplementedException();
        }

        public NHibernate.ISessionFactory SessionFactory
        {
            get { throw new NotImplementedException(); }
        }

        public NHibernate.ISession SetBatchSize(int batchSize)
        {
            throw new NotImplementedException();
        }

        public void SetReadOnly(object entityOrProxy, bool readOnly)
        {
        }

        public NHibernate.Stat.ISessionStatistics Statistics
        {
            get { throw new NotImplementedException(); }
        }

        public NHibernate.ITransaction Transaction
        {
            get { throw new NotImplementedException(); }
        }

        public void Update(string entityName, object obj)
        {
        }

        public void Update(object obj, object id)
        {
        }

        public void Update(object obj)
        {
        }

        public void Dispose()
        {
        }
    }
}
