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

using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.IO;

namespace Rhetos
{
    internal sealed class AdditionalSourceFilesListing
    {
        private readonly IExternalTextReader _externalTextReader;
        private readonly FilesUtility _filesUtility;
        private readonly RhetosBuildEnvironment _rhetosBuildEnvironment;

        public AdditionalSourceFilesListing(IExternalTextReader externalTextReader, FilesUtility filesUtility, RhetosBuildEnvironment rhetosBuildEnvironment)
        {
            _externalTextReader = externalTextReader;
            _filesUtility = filesUtility;
            _rhetosBuildEnvironment = rhetosBuildEnvironment;
        }

        /// <summary>
        /// Writes the list of the additional external source files (see <see cref="IExternalTextReader.ExternalFiles"/>),
        /// for the MSBuild to monitor and detect the changes in the source file for incremental build.
        /// </summary>
        public void WriteList()
        {
            _filesUtility.WriteAllText(
                Path.Combine(_rhetosBuildEnvironment.CacheFolder, "Rhetos.ExternalSourceItems"),
                string.Join(Environment.NewLine, _externalTextReader.ExternalFiles),
                writeOnlyIfModified: true);
        }
    }
}