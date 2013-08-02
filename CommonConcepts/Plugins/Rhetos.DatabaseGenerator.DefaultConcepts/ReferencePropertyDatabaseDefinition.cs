﻿/*
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
using Rhetos.Utilities;
using Rhetos.DatabaseGenerator;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dsl;
using System.Globalization;
using Rhetos.Compiler;

namespace Rhetos.DatabaseGenerator.DefaultConcepts
{
    [Export(typeof(IConceptDatabaseDefinition))]
    [ExportMetadata(MefProvider.Implements, typeof(ReferencePropertyInfo))]
    [ConceptImplementationVersion(2, 0)]
    public class ReferencePropertyDatabaseDefinition : IConceptDatabaseDefinition
    {
        public static bool IsSupported(ReferencePropertyInfo info)
        {
            return info.DataStructure is EntityInfo;
        }

        public string CreateDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (ReferencePropertyInfo)conceptInfo;
            if (IsSupported(info))
                return PropertyDatabaseDefinition.AddColumn((EntityInfo) info.DataStructure, info.GetColumnName(), Sql.Get("ReferencePropertyDatabaseDefinition_DataType"));
            return "";
        }

        public string RemoveDatabaseStructure(IConceptInfo conceptInfo)
        {
            var info = (ReferencePropertyInfo)conceptInfo;
            if (IsSupported(info))
                return PropertyDatabaseDefinition.RemoveColumn((EntityInfo)info.DataStructure, info.GetColumnName());
            return "";
        }
    }

    public static class ReferencePropertyInfoExtensions
    {
        public static string GetColumnName(this ReferencePropertyInfo info)
        {
            return SqlUtility.Identifier(info.Name + "ID");
        }
    }
}
