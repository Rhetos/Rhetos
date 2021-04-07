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
using Rhetos.Security;
using Rhetos.Utilities;
using System.ComponentModel.Composition;

namespace DeployPackages.Test
{
    [Export(typeof(Module))]
    public class TestWebSecurityModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // CheckOverride is implemented here as a legacy feature:
            // WcfWindowsUserInfo is correctly expected here for runtime registration, but this plugin module is also
            // registered at build-time, so this specific CheckOverride should be ignored by Rhetos framework at build time.
            builder.GetRhetosPluginRegistration().CheckOverride<IUserInfo, TestWebSecurityUserInfo>(typeof(WcfWindowsUserInfo));

            builder.RegisterType<TestWebSecurityUserInfo>().As<IUserInfo>();
            base.Load(builder);
        }
    }
}
