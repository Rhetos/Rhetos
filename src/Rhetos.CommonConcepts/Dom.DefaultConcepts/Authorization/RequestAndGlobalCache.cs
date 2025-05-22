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

using Microsoft.Extensions.Caching.Memory;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Rhetos.Dom.DefaultConcepts.Authorization
{
    /// <summary>
    /// The current request's cached data (<see cref="_currentRequestCache"/>)
    /// will be inserted into the global cache (<see cref="_globalCaches"/>)
    /// if the current transaction is committed to the database.
    /// </summary>
    public class RequestAndGlobalCache
    {
        private readonly ILogger _logger;
        private readonly RhetosAppOptions _rhetosAppOptions;
        private readonly IPersistenceTransaction _persistenceTransaction;

        private readonly Dictionary<string, object> _currentRequestCache = new();
        private static readonly ConcurrentDictionary<string, MemoryCache> _globalCaches = new();
        private bool _registeredToUpdateGlobalCache;
        private readonly Lazy<MemoryCache> _tenantCache; // If the application is connecting to multiple databases (for example in a multi-tenant solution), this cache is related to the current context.

        public RequestAndGlobalCache(
            ILogProvider logProvider,
            RhetosAppOptions rhetosAppOptions,
            IPersistenceTransaction persistenceTransaction,
            ConnectionString connectionString)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _rhetosAppOptions = rhetosAppOptions;
            _persistenceTransaction = persistenceTransaction;
            _tenantCache = new(() => _globalCaches.GetOrAdd(connectionString, _ => new MemoryCache(new MemoryCacheOptions())), isThreadSafe: false); // Ne need for thread safety overhead, this class is instanced in the context of a single web request.
        }

        /// <returns>
        /// Returns empty IEnumerable if there are no values in cache.
        /// </returns>
        public IEnumerable<T> GetAllDataFromBothCaches<T>(string key) where T : class
        {
            if (_currentRequestCache.TryGetValue(key, out object currentRequestValue) && currentRequestValue != null)
                yield return (T)currentRequestValue;

            object globalValue = _tenantCache.Value.Get(key);
            if (globalValue != null)
                yield return (T)globalValue;
        }

        /// <summary>
        /// Returns from current request cache, or from global cache if it does not exist in the request cache.
        /// </summary>
        /// <returns>
        /// Returns <see langword="null"/> if there is no cached value.
        /// </returns>
        /// <param name="immutable">
        /// Immutable data is cached in global cache only. This helps with cache reuse between parallel requests, but can be used only on data that is not editable at run-time.
        /// </param>
        public T Get<T>(string key, bool immutable = false) where T : class
        {
            if (!immutable && _currentRequestCache.TryGetValue(key, out object currentRequestValue))
                return (T)currentRequestValue;

            object globalValue = _tenantCache.Value.Get(key);
            return (T)globalValue;
        }

        /// <param name="immutable">
        /// Immutable data is cached in global cache only. This helps with cache reuse between parallel requests, but can be used only on data that is not editable at run-time.
        /// </param>
        public void Set<T>(string key, T value, bool immutable = false) where T : class
        {
            if (immutable)
            {
                _tenantCache.Value.Set(key, value, DateTimeOffset.Now.AddYears(1));
            }
            else
            {
                // Updating cache *after* database commit, to make sure that the cache does not contain incorrect data in case that
                // 1. the authorization data was modified in the current transaction (see AuthorizationAddUnregisteredPrincipals options or Rhetos.ActiveDirectorySync package, e.g.),
                // and 2. the current transaction was rolled back.
                // This delayed cache update will unfortunately increase the number of cache misses on parallel requests, but will fix bugs on edge cases as described above.
                _currentRequestCache[key] = value;
                if (!_registeredToUpdateGlobalCache)
                {
                    _persistenceTransaction.AfterClose += UpdateGlobalCacheAfterCommit;
                    _registeredToUpdateGlobalCache = true;
                }
            }
        }

        /// <summary>
        /// Returns from current request cache, or from global cache if it does not exist in the request cache.
        /// </summary>
        /// <param name="immutable">
        /// Immutable data is cached in global cache only. This helps with cache reuse between parallel requests, but can be used only on data that is not editable at run-time.
        /// </param>
        public T GetOrAdd<T>(string key, Func<T> valueCreator, bool immutable = false) where T : class
        {
            T value = Get<T>(key, immutable);
            if (value != null)
                return value;

            _logger.Trace(() => "Cache miss: " + key + ".");
            value = valueCreator();

            if (value != null)
                Set(key, value, immutable);
            else
                _logger.Trace(() => "Not caching null value: " + key + ".");

            return value;
        }

        private void UpdateGlobalCacheAfterCommit()
        {
            foreach (var item in _currentRequestCache)
                _tenantCache.Value.Set(item.Key, item.Value, DateTimeOffset.Now.AddSeconds(_rhetosAppOptions.AuthorizationCacheExpirationSeconds));
            _currentRequestCache.Clear();
        }

        public void RemoveFromBothCaches(string key)
        {
            _currentRequestCache.Remove(key);
            _tenantCache.Value.Remove(key);
        }

        /// <summary>
        /// For unit testing.
        /// </summary>
        public static void ClearGlobalCacheAll()
        {
            foreach (var cache in _globalCaches.Values)
                cache.Clear();
        }
    }
}
