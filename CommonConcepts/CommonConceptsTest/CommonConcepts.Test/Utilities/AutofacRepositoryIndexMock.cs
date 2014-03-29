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

using Rhetos.Dom.DefaultConcepts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test.Utilities
{
    class AutofacRepositoryIndexMock : Autofac.Features.Indexed.IIndex<string, IRepository>
    {
        private readonly Common.DomRepository _domRepository;

        public AutofacRepositoryIndexMock(Common.DomRepository domRepository)
        {
            _domRepository = domRepository;
        }

        public bool TryGetValue(string key, out IRepository value)
        {
            value = null;
            var path = key.Split('.');

            var moduleProp = _domRepository.GetType().GetProperty(path[0]);
            if (moduleProp == null) return false;
            var moduleRepos = moduleProp.GetValue(_domRepository, null);

            var dsProp = moduleRepos.GetType().GetProperty(path[1]);
            if (dsProp == null) return false;
            var dsRepos = dsProp.GetValue(moduleRepos, null);

            value = dsRepos as IRepository;
            return value != null;
        }

        public IRepository this[string key]
        {
            get
            {
                IRepository result;
                if (!TryGetValue(key, out result))
                    throw new ApplicationException("AutofacRepositoryIndexMock: Missing key '" + key + "'.");
                return result;
            }
        }
    }
}
