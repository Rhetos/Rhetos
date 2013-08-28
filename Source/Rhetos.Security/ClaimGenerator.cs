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
using Rhetos.Logging;
using Rhetos.Persistence;
using System.Linq;
using Rhetos.Processing;
using Autofac.Features.Indexed;

namespace Rhetos.Security
{
    public class ClaimGenerator : IClaimGenerator
    {
        private readonly IPluginsContainer<IClaimProvider> _contextPermissionsRepository;
        private readonly IDslModel _dslModel;
        private readonly Lazy<Type> _claimType;
        private readonly IsGeneratedToken _isGeneratedToken = new IsGeneratedToken();
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly Lazy<IClaimLoader> _claimLoader;
        private readonly IIndex<string, IWritableRepository> _writableRepositories;

        public ClaimGenerator(
            IPluginsContainer<IClaimProvider> contextPermissionsRepository,
            IDslModel dslModel,
            IDomainObjectModel domainObjectModel,
            ILogProvider logProvider,
            Lazy<IClaimLoader> claimLoader,
            IIndex<string, IWritableRepository> writableRepositories)
        {
            _contextPermissionsRepository = contextPermissionsRepository;
            _dslModel = dslModel;
            _claimType = new Lazy<Type>(() => 
                {
                    try
                    {
                        return domainObjectModel.GetType("Common.Claim");
                    }
                    catch (Exception ex)
                    {
                        throw new FrameworkException(ex.Message + " Probably missing package CommonConcepts.", ex);
                    }
                });
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("ClaimGenerator");
            _claimLoader = claimLoader;
            _writableRepositories = writableRepositories;
        }

        private class IsGeneratedToken
        {
            public bool IsGenerated = false;
        }

        public void GenerateClaims()
        {
            lock (_isGeneratedToken)
            {
                if (_isGeneratedToken.IsGenerated)
                    return;

                _isGeneratedToken.IsGenerated = true;
                var stopwatch = Stopwatch.StartNew();

                var newClaims = CreateClaims();
                var oldClaims = _claimLoader.Value.LoadClaims();

                IEqualityComparer<IClaim> comparer = new ClaimComparer();

                IEnumerable<IClaim> delete = oldClaims.Except(newClaims, comparer);
                IEnumerable<IClaim> insert = newClaims.Except(oldClaims, comparer);

                if (delete.Any())
                    _logger.Info(() => "Deleting claims: " + string.Join(", ", delete.Select(claim => claim.ClaimResource + "." + claim.ClaimRight)) + ".");
                if (insert.Any())
                    _logger.Info(() => "Inserting claims: " + string.Join(", ", insert.Select(claim => claim.ClaimResource + "." + claim.ClaimRight)) + ".");

                IWritableRepository claimRepository = _writableRepositories["Common.Claim"];
                claimRepository.Save(insert, null, delete);

                _performanceLogger.Write(stopwatch, "ClaimGenerator.GenerateClaims");
            }
        }

        private IEnumerable<IClaim> CreateClaims()
        {
            List<IClaim> claims = new List<IClaim>();

            foreach (var provider in _contextPermissionsRepository.GetPlugins())
                claims.AddRange(provider.GetAllClaims(_dslModel, CreateClaim));

            return claims.Distinct(new ClaimComparer());
        }

        private IClaim CreateClaim(string resource, string claimRight)
        {
            IClaim claim = (IClaim)Activator.CreateInstance(_claimType.Value);
            claim.ClaimResource = resource;
            claim.ClaimRight = claimRight;

            return claim;
        }
    }
}