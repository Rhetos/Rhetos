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

namespace Rhetos
{
    /// <summary>
    /// Internal class that creates Rhetos unit-of-work scope for each host application's DI scope.
    /// </summary>
    internal sealed class RhetosScopeServiceProvider : IDisposable
    {
        private readonly IUnitOfWorkScope unitOfWorkScope;

        public RhetosScopeServiceProvider(RhetosHost rhetosHost)
        {
            unitOfWorkScope = rhetosHost.CreateScope();
        }

        public T Resolve<T>()
        {
            return unitOfWorkScope.Resolve<T>();
        }

        public void Dispose()
        {
            unitOfWorkScope.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
