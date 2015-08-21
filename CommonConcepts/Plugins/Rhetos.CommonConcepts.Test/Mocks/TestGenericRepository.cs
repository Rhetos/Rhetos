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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Dom;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Rhetos.CommonConcepts.Test.Mocks
{
    class TestGenericRepository<TEntityInterface, TEntity> : GenericRepository<TEntityInterface>
        where TEntityInterface : class, IEntity
        where TEntity : class, TEntityInterface
    {
        public TestGenericRepository(IEnumerable<TEntity> items = null)
            : base(
                new GenericRepositoryParameters
                {
                    DomainObjectModel = new DomainObjectModelMock(),
                    Repositories = new Lazy<IIndex<string, IRepository>>(() => new RepositoryIndexMock<TEntityInterface, TEntity>(items)),
                    LogProvider = new ConsoleLogProvider(),
                    GenericFilterHelper = new GenericFilterHelper(new DomainObjectModelMock()),
                },
                new RegisteredInterfaceImplementations { { typeof(TEntityInterface), typeof(TEntity).FullName }})
        {
        }

        public TestGenericRepository(IRepository repository)
            : base(
                new GenericRepositoryParameters
                {
                    DomainObjectModel = new DomainObjectModelMock(),
                    Repositories = new Lazy<IIndex<string, IRepository>>(() => new RepositoryIndexMock(typeof(TEntity), repository)),
                    LogProvider = new ConsoleLogProvider(),
                    GenericFilterHelper = new GenericFilterHelper(new DomainObjectModelMock()),
                },
                new RegisteredInterfaceImplementations { { typeof(TEntityInterface), typeof(TEntity).FullName } })
        {
        }

        public RepositoryMock<TEntityInterface, TEntity> RepositoryMock
        {
            get
            {
                return (RepositoryMock<TEntityInterface, TEntity>)EntityRepository;
            }
        }
    }
}
