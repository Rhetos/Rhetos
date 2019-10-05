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
using System.Threading.Tasks;

namespace DeployPackages
{
    public class DeployOptions
    {
        public bool Help { get; set; }
        public bool StartPaused { get; set; }
        public bool Debug { get; set; }
        public bool NoPause { get; set; }
        public bool IgnoreDependencies { get; set; }
        public bool ShortTransactions { get; set; }
        public bool DatabaseOnly { get; set; }
        public bool SkipRecompute { get; set; }
    }
}
