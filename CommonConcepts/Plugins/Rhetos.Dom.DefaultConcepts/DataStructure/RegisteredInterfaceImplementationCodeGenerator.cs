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

            codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
            codeBuilder.InsertCode(RegisterImplementationName(info), ModuleCodeGenerator.RegisteredInterfaceImplementationNameTag);
        }

        // TODO: Remove IQueryableRepository registration.  IQueryableRepository should be cast from repository object in Rhetos.Dom.DefaultConcepts.GenericRepositories class.
        protected static string RegisterRepository(RegisteredInterfaceImplementationInfo info)
        {
            return string.Format(
            @"builder.RegisterType<{0}._Helper.{1}_Repository>().As<IQueryableRepository<{2}>>();
            ",
                info.ImplementsInterface.DataStructure.Module.Name,
                info.ImplementsInterface.DataStructure.Name,
                info.ImplementsInterface.GetInterfaceType().FullName);
        }

        protected static string RegisterImplementationName(RegisteredInterfaceImplementationInfo info)
        {
            return string.Format(
            @"{{ typeof({0}), {1} }},
            ",
                info.ImplementsInterface.GetInterfaceType().FullName,
                CsUtility.QuotedString(
                    info.ImplementsInterface.DataStructure.Module.Name
                    + "." + info.ImplementsInterface.DataStructure.Name));
        }
    }
}
