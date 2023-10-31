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

using Rhetos.DatabaseGenerator;
using System;

namespace Rhetos.Utilities
{
    public class StaticUtilities : IDisposable
    {
        private bool disposedValue;
        private readonly DatabaseSettings _databaseSettings;
        private readonly ISqlUtility _sqlUtility;

        public StaticUtilities(DatabaseSettings databaseSettings, ISqlUtility sqlUtility)
        {
            _databaseSettings = databaseSettings;
            _sqlUtility = sqlUtility;
        }

        /// <summary>
        /// Initialize static utilities <see cref="Sql"/> and <see cref="SqlUtility"/>.
        /// They provide static features to simplify plugin development.
        /// </summary>
        public void Initialize()
        {
            Sql.Initialize(_databaseSettings);
            SqlUtility.Initialize(_sqlUtility);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Sql.Initialize(null);
                    SqlUtility.Initialize(null);
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
