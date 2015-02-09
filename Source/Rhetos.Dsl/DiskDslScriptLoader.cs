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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class DiskDslScriptLoader : IDslScriptsProvider
    {
        private List<DslScript> _scripts = null;
        private readonly object _scriptsLock = new object();

        public IEnumerable<DslScript> DslScripts
        {
            get
            {
                if (_scripts == null)
                    lock (_scriptsLock)
                        if (_scripts == null)
                        {
                            var baseFolder = Path.GetFullPath(Paths.DslScriptsFolder);
                            if (baseFolder.Last() != '\\') baseFolder += '\\';

                            var files = Directory.GetFiles(baseFolder, "*.rhe", SearchOption.AllDirectories).OrderBy(path => path);

                            _scripts = files.Select(file =>
                                new DslScript
                                {
                                    Name = file.Replace(baseFolder, String.Empty),
                                    Script = File.ReadAllText(file, Encoding.Default),
                                    Path = Path.GetFullPath(file)
                                }).ToList();
                        }

                return _scripts;
            }
        }
    }
}