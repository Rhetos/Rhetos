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

using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    /// <summary>
    /// This class inserts a new principal in database, intended for option <see cref="RhetosAppOptions.AuthorizationAddUnregisteredPrincipals"/>.
    /// Important feature here is robust concurrency control and error handling,
    /// specifically in a case when executing parallel web requests (possibly on different processes) for the same user.
    /// </summary>
    public class PrincipalWriter
    {
        private readonly ILogger _logger;
        private readonly IQueryableRepository<IPrincipal> _principalRepository;
        private readonly GenericRepository<IPrincipal> _principalGenericRepository;
        private readonly ISqlExecuter _sqlExecuter;
        private readonly ISqlUtility _sqlUtility;

        public PrincipalWriter(
            ILogProvider logProvider,
            INamedPlugins<IRepository> repositories,
            GenericRepository<IPrincipal> principalGenericRepository,
            ISqlExecuter sqlExecuter,
            ISqlUtility sqlUtility)
        {
            _logger = logProvider.GetLogger(GetType().Name);
            _principalRepository = (IQueryableRepository<IPrincipal>)repositories.GetPlugin("Common.Principal");
            _principalGenericRepository = principalGenericRepository;
            _sqlExecuter = sqlExecuter;
            _sqlUtility = sqlUtility;
        }

        private Guid GetPrincipalID(string username)
        {
            return _principalRepository.Query()
                .Where(p => p.Name == username)
                .Select(p => p.ID)
                .SingleOrDefault();
        }

        /// <summary>Insert a principal with provided <see cref="PrincipalInfo.Name"/> to database, and populates ID property of <paramref name="principal"/> parameter.</summary>
        /// <remarks>If another concurrent process created a principal, this method will set the ID of the existing one.</remarks>
        public void SafeInsertPrincipal(ref PrincipalInfo principal)
        {
            string username = principal.Name; // Copy to be used for asynchronous logging.
            _logger.Info(() => $"Adding unregistered principal '{username}'. See {OptionsAttribute.GetConfigurationPath<RhetosAppOptions>()}:{nameof(RhetosAppOptions.AuthorizationAddUnregisteredPrincipals)} in configuration files.");
            var userLock = CreteCustomLock(username.ToUpper());
            bool newCreated = InsertPrincipalOrGetExisting(ref principal);
            if (!newCreated) // If new principal is created, other requests should wait for current transaction to be committed in order to read the new principal.
                ReleaseCustomLock(userLock);
        }

        /// <summary>Populates ID property of provided <paramref name="principal"/>.</summary>
        /// <returns>Whether a new principal is inserted in database.</returns>
        private bool InsertPrincipalOrGetExisting(ref PrincipalInfo principal)
        {
            principal.ID = GetPrincipalID(principal.Name); // Retry reading existing principal after custom lock, maybe another concurrent command created the principal from AuthorizationDataLoader.
            if (principal.ID != default)
                return false;
            try
            {
                var newPrincipal = _principalGenericRepository.CreateInstance();
                newPrincipal.ID = Guid.NewGuid();
                newPrincipal.Name = principal.Name;
                _principalGenericRepository.Insert(newPrincipal);
                principal.ID = newPrincipal.ID;
                return true;
            }
            catch (Exception ex) when (DuplicateNameInDatabase(ex))
            {
                _logger.Warning(() => $"Ignoring concurrent principal creation: {ex.GetType().Name}: {ex.Message}");
                principal.ID = GetPrincipalID(principal.Name); // Check if another concurrent command created the principal, but not from AuthorizationDataLoader (the custom lock was not used).
                if (principal.ID != default)
                    return false;
                else
                    throw new FrameworkException($"Cannot create the principal record for '{principal.Name}'.", ex);
            }
        }

        private bool DuplicateNameInDatabase(Exception ex)
        {
            return _sqlUtility.ExtractSqlException(ex)?.Message?.StartsWith("Cannot insert duplicate key row in object 'Common.Principal' with unique index 'IX_Principal_Name'") == true;
        }

        /// <summary>
        /// Manual database locking is used here in order to avoid deadlocks (may depend on usage of READ_COMMITTED_SNAPSHOT),
        /// see integration test AddUnregisteredPrincipalsParallel.
        /// </summary>
        /// <remarks>
        /// Note that lock resource name is case-sensitive in SQL Server.
        /// </remarks>
        private CustomLockInfo CreteCustomLock(string userName)
        {
            string key = $"AuthorizationDataLoader.{userName}";
            key = CsUtility.LimitWithHash(key, 255); // SQL Server limits key length to 255.

            try
            {
                _sqlExecuter.ExecuteSql(
                    $@"DECLARE @lockResult int;
                    EXEC @lockResult = sp_getapplock {SqlUtility.QuoteText(key)}, 'Exclusive';
                    IF @lockResult < 0 RAISERROR('AuthorizationDataLoader lock.', 16, 10);");
                return new CustomLockInfo(key);
            }
            catch (FrameworkException ex) when (ex.Message.TrimEnd().EndsWith("AuthorizationDataLoader lock."))
            {
                throw new UserException(
                    "Cannot initialize the new user, because the user record is locked by another command that is still running.",
                    ex);
            }
        }

        class CustomLockInfo
        {
            public string Key { get; }

            public CustomLockInfo(string key) => Key = key;
        };

        /// <summary>
        /// Performance optimization for situations when no data was written to database,
        /// so other requests don't need to wait for the current transaction to finish.
        /// </summary>
        private void ReleaseCustomLock(CustomLockInfo lockInfo)
        {
            _sqlExecuter.ExecuteSql($@"EXEC sp_releaseapplock {SqlUtility.QuoteText(lockInfo.Key)};");
        }
    }
}
