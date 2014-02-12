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
using Rhetos.Security;
using System.ComponentModel.Composition;
using Rhetos.Processing.DefaultCommands;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IServerInitializer))]
    public class ClaimGenerator : IServerInitializer
    {
        private readonly IPluginsContainer<IClaimProvider> _claimProviders;
        private readonly IDslModel _dslModel;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly IClaimRepository _claimRepository;

        public ClaimGenerator(
            IPluginsContainer<IClaimProvider> claimProviders,
            IDslModel dslModel,
            ILogProvider logProvider,
            IClaimRepository claimRepository)
        {
            _claimProviders = claimProviders;
            _dslModel = dslModel;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("ClaimGenerator");
            _claimRepository = claimRepository;
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }

        public void Initialize()
        {
            var stopwatch = Stopwatch.StartNew();

            var oldClaims = _claimRepository.LoadClaims();
            _performanceLogger.Write(stopwatch, "ClaimGenerator.Generate: Read old claims.");

            var newClaims = GetAllClaims();
            _performanceLogger.Write(stopwatch, "ClaimGenerator.Generate: Generate new claims.");

            IList<Claim> insert;
            IList<ICommonClaim> update, delete;
            DiffClaims(oldClaims, newClaims, out insert, out update, out delete);
            _performanceLogger.Write(stopwatch, "ClaimGenerator.Generate: Diff claims.");

            _claimRepository.SaveClaims(insert, update, delete);
            _performanceLogger.Write(stopwatch, "ClaimGenerator.Generate: Save claims.");
        }

        protected IList<Claim> GetAllClaims()
        {
            var claims = new List<Claim>();

            foreach (var provider in _claimProviders.GetPlugins())
                claims.AddRange(provider.GetAllClaims(_dslModel));

            return claims.Distinct().ToList();
        }

        protected void DiffClaims(IList<ICommonClaim> oldClaims, IList<Claim> newClaims, out IList<Claim> insert, out IList<ICommonClaim> update, out IList<ICommonClaim> delete)
        {
            var duplicates = newClaims.GroupBy(claim => claim.FullName.ToLower())
                .Where(group => group.Count() > 1)
                .FirstOrDefault();
            if (duplicates != null)
                throw new DslSyntaxException(string.Format("Multiple claims have the same signature '{0}': 1. {1}/{2}, 2. {3}/{4}.",
                    duplicates.Key,
                    duplicates.ElementAt(0).Resource, duplicates.ElementAt(0).Right,
                    duplicates.ElementAt(1).Resource, duplicates.ElementAt(1).Right));

            var newClaimsIndex = newClaims.ToDictionary(c => c);
            var oldClaimsIndex = oldClaims.ToDictionary(c => new Claim(c.ClaimResource, c.ClaimRight));

            insert = newClaims.Where(nc => !oldClaimsIndex.ContainsKey(nc)).ToList();
            delete = oldClaimsIndex.Where(oci => !newClaimsIndex.ContainsKey(oci.Key)).Select(oci => oci.Value).ToList();

            var forUpdate = oldClaimsIndex.Select(oci =>
                {
                    Claim newClaim = null;
                    newClaimsIndex.TryGetValue(oci.Key, out newClaim);
                    return new { old = oci, newClaim };
                })
                .Where(pair => pair.newClaim != null && !pair.old.Key.Same(pair.newClaim))
                .ToList();
            foreach (var pair in forUpdate)
            {
                pair.old.Value.ClaimResource = pair.newClaim.Resource;
                pair.old.Value.ClaimRight = pair.newClaim.Right;
            }
            update = forUpdate.Select(pair => pair.old.Value).ToList();

            var insertRef = insert;
            var updateRef = update;
            var deleteRef = delete;
            if (insert.Any())
                _logger.Info(() => "Inserting claims: " + string.Join(", ", insertRef.Select(claim => claim.FullName)) + ".");
            if (update.Any())
                _logger.Info(() => "Updating claims case: " + string.Join(", ", updateRef.Select(claim => claim.ClaimResource + "." + claim.ClaimRight)) + ".");
            if (delete.Any())
                _logger.Info(() => "Deleting claims: " + string.Join(", ", deleteRef.Select(claim => claim.ClaimResource + "." + claim.ClaimRight)) + ".");
        }
    }
}