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

using Rhetos.Persistence;
using System;
using System.Data.Common;

namespace Rhetos.CommonConcepts.Test.Mocks
{
    public class FakePersistenceTransaction : IPersistenceTransaction
    {
        public DbConnection Connection => throw new NotImplementedException();

        public DbTransaction Transaction => throw new NotImplementedException();

#pragma warning disable CA1713 // Events should not have 'Before' or 'After' prefix
        public event Action BeforeClose;
        public event Action AfterClose;
#pragma warning restore CA1713 // Events should not have 'Before' or 'After' prefix

        bool _commitOnDispose;
        bool _discardOnDispose;
        private bool disposedValue;

        public void CommitAndClose()
        {
            _commitOnDispose = true;
            Dispose();
        }

        public void RollbackAndClose()
        {
            DiscardOnDispose();
            Dispose();
        }

        public void DiscardOnDispose()
        {
            _discardOnDispose = true;
        }
        private void Close()
        {
            if (_commitOnDispose && !_discardOnDispose)
            {
                BeforeClose?.Invoke();
                AfterClose?.Invoke();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
