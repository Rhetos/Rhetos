﻿/*
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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ComputedInfo))]
    public class ComputedCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ComputedInfo)conceptInfo;

            string loadFunctionBodySnippet =
            $@"Func<Common.DomRepository{DataStructureUtility.ComputationAdditionalParametersTypeTag.Evaluate(info)}, global::{info.Module.Name}.{info.Name}[]> compute_Function =
            {info.Expression};

            return compute_Function(_domRepository{DataStructureUtility.ComputationAdditionalParametersArgumentTag.Evaluate(info)});";

            RepositoryHelper.GenerateReadableRepository(info, codeBuilder, loadFunctionBodySnippet);

            DataStructureCodeGenerator.AddInterfaceAndReference(codeBuilder, $"EntityBase<{info.Module.Name}.{info.Name}>", info);
        }
    }
}
