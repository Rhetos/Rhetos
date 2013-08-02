/*
    Copyright (C) 2013 Omega software d.o.o.

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
using System.Configuration;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using Rhetos.Utilities;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Factory;
using Rhetos.Logging;
using Rhetos.Persistence;
using Rhetos.Processing;
using Rhetos.Processing.DefaultCommands;
using Autofac.Features.Indexed;

namespace Rhetos.Security
{
    public class AuthorizationManager : IAuthorizationManager
    {
        private readonly IPrincipalProvider _principalProvider;
        private readonly ITypeFactory _typeFactory;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly IPersistenceEngine _persistenceEngine;
        private readonly IPluginsContainer<IClaimProvider> _contextPermissionsRepository;
        private readonly ILogger _logger;
        private readonly ILogger _performanceLogger;
        private readonly bool _allowBuiltinAdminOverride;
        private readonly Lazy<Type> _claimType;

        public AuthorizationManager(
            IPluginsContainer<IClaimProvider> contextPermissionsRepository, 
            IPrincipalProvider principalProvider,
            ITypeFactory typeFactory, 
            ILogProvider logProvider,
            IDomainObjectModel domainObjectModel,
            IPersistenceEngine persistenceEngine)
        {
            _principalProvider = principalProvider;
            _typeFactory = typeFactory;
            _domainObjectModel = domainObjectModel;
            _persistenceEngine = persistenceEngine;
            _contextPermissionsRepository = contextPermissionsRepository;
            _logger = logProvider.GetLogger("AuthorizationManager");
            _performanceLogger = logProvider.GetLogger("Performance");

            _allowBuiltinAdminOverride = FromConfigallowBuiltinAdminOverride();

            _claimType = new Lazy<Type>(() => domainObjectModel.ResolveType("Common.Claim"));
        }

        private static bool FromConfigallowBuiltinAdminOverride()
        {
            if (ConfigurationManager.AppSettings["BuiltinAdminOverride"] != null)
            {
                bool allow;
                if (bool.TryParse(ConfigurationManager.AppSettings["BuiltinAdminOverride"], out allow))
                    return allow;
                
                throw new FrameworkException("Invalid setting of BuiltinAdminOverride in configuration file. Allowed values are True and False.");
            }
            return false;
        }

        public string Authorize(IEnumerable<ICommandInfo> commandInfos)
        {
            var sw = Stopwatch.StartNew();
            IEnumerable<IClaim> requiredClaims = GetRequiredClaims(commandInfos);
            _performanceLogger.Write(sw, "AuthorizationManager.Authorize requiredClaims");

            var claimsAuthorization = requiredClaims.Zip(GetAuthorizations(requiredClaims), (claim, authorized) => new { claim, authorized });
            var unauthorized = claimsAuthorization.FirstOrDefault(ca => ca.authorized == false);
            _performanceLogger.Write(sw, "AuthorizationManager.Authorize unauthorizedClaim");

            if (unauthorized != null)
            {
                _logger.Trace(() => string.Format("User {0} does not posses claim {1}.{2}.", _principalProvider.Identity, unauthorized.claim.ClaimResource, unauthorized.claim.ClaimRight));

                return string.Format(
                    "You are not authorized for '{0}' on resource '{1}'. User '{2}'.",
                    unauthorized.claim.ClaimRight, unauthorized.claim.ClaimResource, _principalProvider.Identity);
            }

            return null;
        }

        public bool[] GetAuthorizations(IEnumerable<IClaim> requiredClaims)
        {
            var sw = Stopwatch.StartNew();
            if (_allowBuiltinAdminOverride && _principalProvider.IsBuiltInAdministrator())
            {
                _logger.Trace(() => string.Format("User {0} has builtin administrator privileges.", _principalProvider.Identity));
                return Enumerable.Repeat(true, requiredClaims.Count()).ToArray();
            }

            IEnumerable<string> membership = _principalProvider.GetIdentityMembership();

            // Force-register the object model to TypeFactory.
            var objectModel = _domainObjectModel.ObjectModel;

            using (var tran = _persistenceEngine.BeginTransaction(new NullUserInfo()))
            using (var inner = _typeFactory.CreateInnerTypeFactory())
            {
                inner.RegisterInstance(tran.NHibernateSession);
                inner.RegisterInstance<IUserInfo>(new NullUserInfo());
                inner.RegisterInstance<ISqlExecuter>(new NullSqlExecuter());
                inner.RegisterInstance(inner);

                IPermission[] rawData = inner.Resolve<IPermissionLoader>().LoadPermissions(requiredClaims, membership);

                HashSet<string> claimsWithRight = new HashSet<string>();
                foreach (IPermission permission in rawData)
                    if (permission.IsAuthorized.Value)
                        claimsWithRight.Add(permission.ClaimResource + "." + permission.ClaimRight);
                foreach (IPermission permission in rawData)
                    if (!permission.IsAuthorized.Value && claimsWithRight.Contains(permission.ClaimResource + "." + permission.ClaimRight)) 
                        claimsWithRight.Remove(permission.ClaimResource + "." + permission.ClaimRight);

                var authorizations = requiredClaims.Select(requiredClaim => claimsWithRight.Contains(requiredClaim.ClaimResource + "." + requiredClaim.ClaimRight)).ToArray();
                _performanceLogger.Write(sw, "AuthorizationManager.GetAuthorizations");
                return authorizations;
            }
        }

        private IEnumerable<IClaim> GetRequiredClaims(IEnumerable<ICommandInfo> commandInfos)
        {
            List<IClaim> requiredPermissions = new List<IClaim>();
            foreach (ICommandInfo commandInfo in commandInfos)
            {
                var providers = _contextPermissionsRepository.GetImplementations(commandInfo.GetType());
                foreach (var provider in providers)
                    requiredPermissions.AddRange(provider.GetRequiredClaims(commandInfo, CreateClaim));
            }
            return requiredPermissions;
        }

        private IClaim CreateClaim(string resource, string claimRight)
        {
            IClaim claim = _typeFactory.CreateInstance<IClaim>(_claimType.Value);
            claim.ClaimResource = resource;
            claim.ClaimRight = claimRight;

            return claim;
        }
    }
}