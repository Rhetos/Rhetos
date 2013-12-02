/*
    Copyright (C) 2013 Omega software d.o.o.

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
    [ExportMetadata(MefProvider.Implements, typeof(RegisteredQueryableRepositoryInfo))]
    public class RegisteredQueryableRepositoryCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (RegisteredQueryableRepositoryInfo)conceptInfo;

            codeBuilder.InsertCode(
                RegisterRepository(info.ImplementsInterface.DataStructure, info.ImplementsInterface.GetInterfaceType()),
                ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);
        }

        protected static string RegisterRepository(DataStructureInfo info, Type type)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().As<IQueryableRepository<{2}>>();
            ", info.Module.Name, info.Name, type.FullName);
        }
    }
}
