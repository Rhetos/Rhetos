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

using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Security;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IServerInitializer))]
    public class ClaimGenerator : IServerInitializer
    {
        private readonly IPluginsContainer<IClaimProvider> _claimProviders;
        private readonly IDslModel _dslModel;
        private readonly ILogger _performanceLogger;
        /// <summary>Special logger for keeping track of inserted/updated/deleted claims.</summary>
        private readonly ILogger _claimsLogger;
        private readonly GenericRepository<ICommonClaim> _claimRepository;

        public ClaimGenerator(
            IPluginsContainer<IClaimProvider> claimProviders,
            IDslModel dslModel,
            ILogProvider logProvider,
            GenericRepository<ICommonClaim> claimRepository)
        {
            _claimProviders = claimProviders;
            _dslModel = dslModel;
            _performanceLogger = logProvider.GetLogger("Performance");
            _claimsLogger = logProvider.GetLogger("ClaimGenerator Claims");
            _claimRepository = claimRepository;
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }

        public void Initialize()
        {
            var stopwatch = Stopwatch.StartNew();

            var newClaims = GetNewActiveClaims();
            _performanceLogger.Write(stopwatch, "ClaimGenerator.Generate: Generate new claims.");

            _claimRepository.InsertOrUpdateOrDeleteOrDeactivate(
                newClaims,
                new ClaimComparer(),
                (x, y) => x.ClaimResource == y.ClaimResource && x.ClaimRight == y.ClaimRight && x.Active == y.Active,
                new FilterAll(),
                (dest, source) =>
                {
                    dest.ClaimResource = source.ClaimResource;
                    dest.ClaimRight = source.ClaimRight;
                    dest.Active = source.Active;
                },
                new DeactivateInsteadOfDelete(), // This is a filter on Common.Claim's repository, see ClaimRepositoryCodeGenerator.
                LogSummary);
            
            _performanceLogger.Write(stopwatch, "ClaimGenerator.Generate: Save claims.");
        }

        protected IEnumerable<ICommonClaim> GetNewActiveClaims()
        {
            var securityClaims = new List<Claim>();

            foreach (var provider in _claimProviders.GetPlugins())
                securityClaims.AddRange(provider.GetAllClaims(_dslModel));

            securityClaims = securityClaims.Distinct().ToList();
            ValidateClaims(securityClaims);

            return _claimRepository.CreateList(securityClaims, (securityClaim, commonClaim) =>
            {
                commonClaim.ClaimResource = securityClaim.Resource;
                commonClaim.ClaimRight = securityClaim.Right;
            });
        }

        protected void ValidateClaims(IList<Claim> newClaims)
        {
            var duplicates = newClaims.GroupBy(claim => claim.FullName.ToLower())
                .Where(group => group.Count() > 1)
                .FirstOrDefault();
            if (duplicates != null)
                throw new DslSyntaxException(string.Format("Multiple claims have the same signature '{0}': 1. {1}/{2}, 2. {3}/{4}.",
                    duplicates.Key,
                    duplicates.ElementAt(0).Resource, duplicates.ElementAt(0).Right,
                    duplicates.ElementAt(1).Resource, duplicates.ElementAt(1).Right));
        }

        private void LogSummary(ref IEnumerable<ICommonClaim> toInsert, ref IEnumerable<ICommonClaim> toUpdate, ref IEnumerable<ICommonClaim> toDelete)
        {
            Log("Inserting claims", toInsert);
            Log("Updating claims", toUpdate);
            Log("Deleting claims", toDelete);
        }

        private void Log(string title, IEnumerable<ICommonClaim> claims)
        {
            const int groupSize = 1000;
            var groups = claims.Select((claim, index) => new { claim, index })
                .GroupBy(ci => ci.index / groupSize, ci => ci.claim)
                .OrderBy(g => g.Key).ToList();

            foreach (var group in groups)
                _claimsLogger.Trace(() => title + " " + string.Join(", ", group.Select(claim => claim.ClaimResource + "." + claim.ClaimRight)) + ".");
        }

        internal class ClaimComparer : IComparer<ICommonClaim>
        {
            public int Compare(ICommonClaim x, ICommonClaim y)
            {
                return Claim.EquivalentComparer(x.ClaimResource, x.ClaimRight, y.ClaimResource, y.ClaimRight);
            }
        }
    }
}