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

using Autofac;
using System;

namespace Rhetos.Utilities.Test.Helpers
{
    public class FakeUnitOfWorkScope : IUnitOfWorkScope
    {
        private ILifetimeScope _scope;
        private bool _disposedValue;

        public FakeUnitOfWorkScope(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public void CommitAndClose()
        {
            Console.WriteLine($"{GetType()} {nameof(CommitAndClose)}");
        }

        public T Resolve<T>()
        {
            return _scope.Resolve<T>();
        }

        public void RollbackAndClose()
        {
            Console.WriteLine($"{GetType()} {nameof(RollbackAndClose)}");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                    _scope.Dispose();
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }
    }
}