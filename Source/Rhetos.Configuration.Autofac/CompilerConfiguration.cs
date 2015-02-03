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
using Autofac;
using Rhetos.Dsl;
using Rhetos.Compiler;
using System.Diagnostics.Contracts;
using Rhetos.Extensibility;

namespace Rhetos.Configuration.Autofac
{
    public class CompilerConfiguration : Module
    {
        private readonly bool _deploymentTime;

        public CompilerConfiguration(bool deploymentTime)
        {
            _deploymentTime = deploymentTime;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_deploymentTime)
            {
                builder.RegisterType<CodeBuilder>().As<ICodeBuilder>();
                builder.RegisterType<CodeGenerator>().As<ICodeGenerator>();
                builder.RegisterType<AssemblyGenerator>().As<IAssemblyGenerator>();
                Plugins.FindAndRegisterPlugins<IConceptCodeGenerator>(builder);
            }

            base.Load(builder);
        }
    }
}
