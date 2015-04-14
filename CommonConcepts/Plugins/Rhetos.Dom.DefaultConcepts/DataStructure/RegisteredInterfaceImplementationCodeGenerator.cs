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
using System.Globalization;
using System.ComponentModel.Composition;
using Microsoft.CSharp.RuntimeBinder;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(RegisteredInterfaceImplementationInfo))]
    public class RegisteredInterfaceImplementationCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (RegisteredInterfaceImplementationInfo)conceptInfo;

            var interfaceType = Type.GetType(info.InterfaceAssemblyQualifiedName);
            if (interfaceType == null)
                throw new DslSyntaxException(conceptInfo, "Could not find type '" + info.InterfaceAssemblyQualifiedName + "'.");

            // TODO: Remove IQueryableRepository registration.  IQueryableRepository should be cast from repository object in Rhetos.Dom.DefaultConcepts.GenericRepositories class.
            string registerRepository = string.Format(
                @"builder.RegisterType<{0}._Helper.{1}_Repository>().As<IQueryableRepository<{2}>>().InstancePerLifetimeScope();
            ",
                    info.DataStructure.Module.Name,
                    info.DataStructure.Name,
                    interfaceType.FullName);

            codeBuilder.InsertCode(registerRepository, ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);

            string registerImplementationName = string.Format(
                @"{{ typeof({0}), {1} }},
            ",
                    interfaceType.FullName,
                    CsUtility.QuotedString(
                        info.DataStructure.Module.Name
                        + "." + info.DataStructure.Name));

            codeBuilder.InsertCode(registerImplementationName, ModuleCodeGenerator.RegisteredInterfaceImplementationNameTag);
        }
    }
}
