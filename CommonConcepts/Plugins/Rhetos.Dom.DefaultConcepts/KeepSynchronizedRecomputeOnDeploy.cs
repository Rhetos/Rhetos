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

using Rhetos.Extensibility;
using Rhetos.Logging;
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
        GenericRepositories _genericRepositories;
        ILogger _performanceLogger;
        ILogger _logger;
        CurrentKeepSynchronizedMetadata _currentKeepSynchronizedMetadata;

        public KeepSynchronizedRecomputeOnDeploy(
            GenericRepositories genericRepositories,
            ILogProvider logProvider,
            CurrentKeepSynchronizedMetadata currentKeepSynchronizedMetadata)
        {
            _genericRepositories = genericRepositories;
            _performanceLogger = logProvider.GetLogger("Performance");
            _logger = logProvider.GetLogger("KeepSynchronizedRecomputeOnDeploy");
            _currentKeepSynchronizedMetadata = currentKeepSynchronizedMetadata;
        }

        // Called at deployment time
        public void Initialize()
        {
            var sw = Stopwatch.StartNew();

            var keepSyncRepos = _genericRepositories.GetGenericRepository<IKeepSynchronizedMetadata>();
            var ignoreTargets = _genericRepositories.GetGenericRepository<IKeepSynchronizedIgnoreTarget>()
                .Query()
                .Select(x => x.Target)
                .ToList();

            var oldItems = keepSyncRepos.Load();
            var avoidRecompute = new HashSet<string>(oldItems.Where(item => item.Context == "NORECOMPUTE").Select(GetKey));

            IEnumerable<IKeepSynchronizedMetadata> toInsert, toUpdate, toDelete;
            keepSyncRepos.Diff(oldItems, _currentKeepSynchronizedMetadata, new SameRecord(), SameValue, Assign, out toInsert, out toUpdate, out toDelete);

            foreach (var keepSynchronized in toInsert.Concat(toUpdate))
                if (!avoidRecompute.Contains(GetKey(keepSynchronized)) && !ignoreTargets.Contains(keepSynchronized.Target))
                {
                    _logger.Info(() => string.Format("Recomputing {0} from {1}.", keepSynchronized.Target, keepSynchronized.Source));
                    _genericRepositories.GetGenericRepository(keepSynchronized.Target).RecomputeFrom(keepSynchronized.Source);
                    _performanceLogger.Write(sw, () => string.Format("KeepSynchronizedRecomputeOnDeploy: {0} from {1}.", keepSynchronized.Target, keepSynchronized.Source));
                }
                else
                    _logger.Info(() => string.Format("Specified not to recompute {0} from {1}.", keepSynchronized.Target, keepSynchronized.Source));

            keepSyncRepos.Save(toInsert, toUpdate, toDelete);
        }

        private class SameRecord : IComparer<IKeepSynchronizedMetadata>
        {
            public int Compare(IKeepSynchronizedMetadata x, IKeepSynchronizedMetadata y)
            {
                return string.Compare(GetKey(x), GetKey(y), StringComparison.Ordinal);
            }
        }

        private bool SameValue(IKeepSynchronizedMetadata x, IKeepSynchronizedMetadata y)
        {
            return x.Source == y.Source && x.Target == y.Target && x.Context == y.Context;
        }

        private void Assign(IKeepSynchronizedMetadata destination, IKeepSynchronizedMetadata source)
        {
            destination.Source = source.Source;
            destination.Target = source.Target;
            destination.Context = source.Context;
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }

        private static string GetKey(IKeepSynchronizedMetadata ks)
        {
            return ks.Source + "/" + ks.Target;
        }
    }
}
