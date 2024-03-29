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

using Rhetos.Dsl.DefaultConcepts;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(LinkedItemsInfo))]
    public class LinkedItemsCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (LinkedItemsInfo)conceptInfo;

            if (DslUtility.IsQueryable(info.ReferenceProperty.DataStructure) && DslUtility.IsQueryable(info.DataStructure))
            {
                string propertyType = $"IList<Common.Queryable.{info.ReferenceProperty.DataStructure.Module.Name}_{info.ReferenceProperty.DataStructure.Name}>";
                DataStructureQueryableCodeGenerator.AddNavigationProperty(codeBuilder, info.DataStructure, info.Name, propertyType);
            }
        }
    }
}
