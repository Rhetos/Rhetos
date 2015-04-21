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
using Rhetos.Dsl;
using System.ComponentModel.Composition;
using Rhetos.Utilities;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Enables lazy loading of navigation properties an all data structures, if the configuration property is set.
    /// </summary>
    [Export(typeof(IConceptMacro))]
    public class LazyLoadAllMacro : IConceptMacro<InitializationConcept>
    {
        private readonly Lazy<bool> _lazyLoadAll;

        public LazyLoadAllMacro(IConfiguration configuration)
        {
            _lazyLoadAll = configuration.GetBool("CommonConcepts.LazyLoadAll", false);
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(InitializationConcept conceptInfo, IDslModel existingConcepts)
        {
            if (_lazyLoadAll.Value)
                return existingConcepts.FindByType<ModuleInfo>().Select(m => new LazyLoadModuleInfo { Module = m });
            return null;
        }
    }
}
