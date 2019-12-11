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

namespace Rhetos.Utilities
{
    public class BuildOptions
    {
        public string DatabaseLanguage { get; set; }
        public string GeneratedAssetsFolder { get; set; }
        public string GeneratedSourceFolder { get; set; }
        public string GeneratedFilesCacheFolder { get; set; }
        public bool Debug { get; set; }
        public bool ShortTransactions { get; set; }
        // redundant options with deploy options of the same name, one should be removed
        public bool DatabaseOnly { get; set; }
        public bool DataMigration__SkipScriptsWithWrongOrder { get; set; } = true;
        public bool CommonConcepts__Legacy__AutoGeneratePolymorphicProperty { get; set; } = true;
        public bool CommonConcepts__Legacy__CascadeDeleteInDatabase { get; set; } = true;
    }
}
