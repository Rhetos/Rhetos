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
using System.Diagnostics;
using Rhetos.Utilities;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Factory;
using Rhetos.Logging;
using Rhetos.Persistence;
using System.Linq;
using Rhetos.Processing;

namespace Rhetos.Security
{
    public class ClaimGenerator : IClaimGenerator
    {
        private readonly IPluginsContainer<IClaimProvider> _contextPermissionsRepository;
        private readonly IDslModel _dslModel;
        private readonly ITypeFactory _typeFactory;
        private readonly IDomainObjectModel _domainObjectModel;
        private readonly IPersistenceEngine _persistenceEngine;
        private readonly Lazy<Type> _claimType;
        private readonly IsGeneratedToken _isGeneratedToken = new IsGeneratedToken();
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;

        public ClaimGenerator(
            IPluginsContainer<IClaimProvider> contextPermissionsRepository,
            IDslModel dslModel,
            ITypeFactory typeFactory,
            IDomainObjectModel domainObjectModel,
            IPersistenceEngine persistenceEngine,
            ILogProvider logProvider)
        {
            _contextPermissionsRepository = contextPermissionsRepository;
            _dslModel = dslModel;
            _typeFactory = typeFactory;
            _domainObjectModel = domainObjectModel;
            _persistenceEngine = persistenceEngine;
            _claimType = new Lazy<Type>(() => 
                {
                    try
                    {
                        return domainObjectModel.ResolveType("Common.Claim");
                    }
                    catch (Exception ex)
                    {
                        throw new FrameworkException(ex.Message + " Probably missing package CommonConcepts.", ex);
                    }
                });
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("ClaimGenerator");
        }

        private class IsGeneratedToken
        {
            public bool IsGenerated = false;
        }

        public void Reset()
        {
            lock (_isGeneratedToken)
            {
                _isGeneratedToken.IsGenerated = false;
                
            }
        }

        public void GenerateClaims()
        {
            lock (_isGeneratedToken)
            {
                if (_isGeneratedToken.IsGenerated)
                    return;

                _isGeneratedToken.IsGenerated = true;
                var stopwatch = Stopwatch.StartNew();

                using (var tran = _persistenceEngine.BeginTransaction(new NullUserInfo()))
                using (var inner = _typeFactory.CreateInnerTypeFactory())
                {
                    inner.RegisterInstance(tran.NHibernateSession);
                    inner.RegisterInstance<IUserInfo>(new NullUserInfo());
                    inner.RegisterInstance<ISqlExecuter>(new NullSqlExecuter());
                    inner.RegisterInstance(inner);

                    var claims = CreateClaims(inner);

                    var oldClaims = inner.Resolve<IClaimLoader>().LoadClaims();

                    IEqualityComparer<IClaim> comparer = new ClaimComparer();

                    IEnumerable<IClaim> delete = oldClaims.Except(claims, comparer);
                    IEnumerable<IClaim> insert = claims.Except(oldClaims, comparer);

                    if (delete.Any())
                        _logger.Info(() => "Deleting claims: " + string.Join(", ", delete.Select(claim => claim.ClaimResource + "." + claim.ClaimRight)) + ".");
                    if (insert.Any())
                        _logger.Info(() => "Inserting claims: " + string.Join(", ", insert.Select(claim => claim.ClaimResource + "." + claim.ClaimRight)) + ".");

                    var claimRepos = (IWritableRepository)inner.CreateInstance(Type.GetType("Common._Helper.Claim_Repository, ServerDom"));
                    claimRepos.Save(insert, null, delete);

                    tran.ApplyChanges();
                }

                _performanceLogger.Write(stopwatch, "ClaimGenerator.GenerateClaims");
            }
        }

        private IEnumerable<IClaim> CreateClaims(ITypeFactory inner)
        {
            List<IClaim> claims = new List<IClaim>();

            foreach (var provider in _contextPermissionsRepository.GetPlugins())
            {
                claims.AddRange(provider.GetAllClaims(_dslModel, CreateClaim));
            }

            return claims.Distinct(new ClaimComparer());
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