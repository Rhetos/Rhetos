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

using Rhetos.Host.AspNet.Dashboard;
using System;
using System.ComponentModel.Composition;

namespace Rhetos.Core.Test.Reference
{
    /// <summary>
    /// This class is used in test project "Rhetos.Extensibility.Test", to test Rhetos PluginsScanner error handling
    /// when loading plugins with incorrectly configured dependencies.
    /// 1. The class depends on "Rhetos.Host.AspNet" assembly because it implements <see cref="IDashboardSnippet"/>,
    /// but the referenced assembly is not available in the test project "Rhetos.Extensibility.Test".
    /// 2. The class is registered as a plugin for commonly available interface <see cref="ICloneable"/>.
    /// </summary>
    [Export(typeof(ICloneable))]
    public class ExportClass : IDashboardSnippet, ICloneable
    {
        public string DisplayName => throw new NotImplementedException();

        public int Order => throw new NotImplementedException();

        public object Clone() => throw new NotImplementedException();

        public string RenderHtml() => throw new NotImplementedException();
    }
}
