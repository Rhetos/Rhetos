﻿/*
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
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IServerInitializer))]
    public class KeepSynchronizedRecomputeOnDeploy : IServerInitializer
    {
        private readonly GenericRepositories _genericRepositories;
        private readonly ILogger _performanceLogger;
        private readonly ILogger _logger;
        private readonly CurrentKeepSynchronizedMetadata _currentKeepSynchronizedMetadata;
        private readonly DbUpdateOptions _dbUpdateOptions;
        private readonly IDslModel _dslModel;

        public KeepSynchronizedRecomputeOnDeploy(
            GenericRepositories genericRepositories,
            ILogProvider logProvider,
            CurrentKeepSynchronizedMetadata currentKeepSynchronizedMetadata,
            DbUpdateOptions dbUpdateOptions,
            IDslModel dslModel)
        {
            _genericRepositories = genericRepositories;
            _performanceLogger = logProvider.GetLogger("Performance." + GetType().Name);
            _logger = logProvider.GetLogger("KeepSynchronizedRecomputeOnDeploy");
            _currentKeepSynchronizedMetadata = currentKeepSynchronizedMetadata;
            _dbUpdateOptions = dbUpdateOptions;
            _dslModel = dslModel;
        }

        // Called at deployment time
        public void Initialize()
        {
            var sw = Stopwatch.StartNew();
            
            var keepSyncRepos = _genericRepositories.GetGenericRepository<IKeepSynchronizedMetadata>();
            var oldItems = keepSyncRepos.Load();
            var skipRecomputeDbMetadata = new HashSet<string>(oldItems.Where(item => item.Context == "NORECOMPUTE").Select(GetComputationKey));

            var skipRecomputeDslConcept = new HashSet<string>(_dslModel.FindByType<SkipRecomputeOnDeployInfo>().Select(GetComputationKey));

            bool skipRecomputeDeployParameter = _dbUpdateOptions.SkipRecompute;

            var (toInsert, toUpdate, toDelete) = ComputedFromHelper.Diff(oldItems, _currentKeepSynchronizedMetadata, new SameRecord(), SameValue).PrepareForSaving();

            _performanceLogger.Write(sw, () => "Load metadata.");

            foreach (var compute in toInsert.Concat(toUpdate))
            {
                if (skipRecomputeDeployParameter)
                {
                    _logger.Warning(() => $"Skipped recomputing {compute.Target} from {compute.Source} due to deployment parameter.");
                }
                else if (skipRecomputeDslConcept.Contains(GetComputationKey(compute)))
                {
                    _logger.Warning(() => $"Skipped recomputing {compute.Target} from {compute.Source} due to DSL concept.");
                }
                else if (skipRecomputeDbMetadata.Contains(GetComputationKey(compute)))
                {
                    _logger.Warning(() => $"Skipped recomputing {compute.Target} from {compute.Source} due to database metadata.");
                }
                else
                {
                    _logger.Info(() => $"Recomputing {compute.Target} from {compute.Source}.");
                    _genericRepositories.GetGenericRepository(compute.Target).RecomputeFrom(compute.Source);
                    _performanceLogger.Write(sw, () => $"{compute.Target} from {compute.Source}.");
                }
            }

            keepSyncRepos.Save(toInsert, toUpdate, toDelete);
            _performanceLogger.Write(sw, () => "Save metadata.");
        }

        private sealed class SameRecord : IComparer<IKeepSynchronizedMetadata>
        {
            public int Compare(IKeepSynchronizedMetadata x, IKeepSynchronizedMetadata y)
            {
                return string.Compare(GetComputationKey(x), GetComputationKey(y), StringComparison.Ordinal);
            }
        }

        private bool SameValue(IKeepSynchronizedMetadata x, IKeepSynchronizedMetadata y)
        {
            return x.Source == y.Source && x.Target == y.Target && x.Context == y.Context;
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }

        private static string GetComputationKey(IKeepSynchronizedMetadata ks)
        {
            return ks.Source + "/" + ks.Target;
        }

        private static string GetComputationKey(SkipRecomputeOnDeployInfo info)
        {
            var source = info.EntityComputedFrom.Source;
            var target = info.EntityComputedFrom.Target;
            return $"{source.Module.Name}.{source.Name}/{target.Module.Name}.{target.Name}";
        }
    }
}
