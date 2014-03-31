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

using Autofac.Features.Indexed;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CommonConcepts.Test.Utilities
{
    static class Create
    {
        public static IDomainObjectModel DomainObjectModel()
        {
            return new DomainObjectModelMock(typeof(Common.DomRepository).Assembly);
        }

        public static XmlUtility XmlUtility()
        {
            return new XmlUtility(DomainObjectModel());
        }

        public static GenericRepositories GenericRepositories(Common.ExecutionContext executionContext)
        {
            var dom = DomainObjectModel();

            return new GenericRepositories(
                dom,
                new Lazy<IIndex<string, IRepository>>(() => new AutofacRepositoryIndexMock(new Common.DomRepository(executionContext))),
                null,
                new ConsoleLogProvider(),
                executionContext.PersistenceTransaction,
                new GenericFilterHelper(dom));
        }

        public static GenericFilterHelper GenericFilterHelper()
        {
            var dom = DomainObjectModel();
            return new GenericFilterHelper(dom);
        }

    }
}
