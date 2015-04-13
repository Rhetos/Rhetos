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
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureExtendsInfo))]
    public class DataStructureExtendsCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureExtendsInfo info = (DataStructureExtendsInfo)conceptInfo;

            DataStructureQueryableCodeGenerator.AddProperty(codeBuilder, info.Extension, "Base", info.Base.Module.Name + "_" + info.Base.Name);

            if (info.Extension is IWritableOrmDataStructure && info.Base is IOrmDataStructure)
            {
                codeBuilder.InsertCode(
                    string.Format(CultureInfo.InvariantCulture,
@"            foreach(var item in insertedNew)
                item.Base = _executionContext.NHibernateSession.Load<{0}>(item.ID);
            foreach(var item in updatedNew)
                item.Base = _executionContext.NHibernateSession.Load<{0}>(item.ID);
            foreach(var item in deletedIds)
                item.Base = _executionContext.NHibernateSession.Load<{0}>(item.ID);

", info.Base.GetKeyProperties()),
                    WritableOrmDataStructureCodeGenerator.InitializationTag.Evaluate(info.Extension));
            }

            DataStructureQueryableCodeGenerator.AddProperty(codeBuilder, info.Base, ExtensionPropertyName(info), info.Extension.Module.Name + "_" + info.Extension.Name);

            if (info.Base is IWritableOrmDataStructure && info.Extension is IOrmDataStructure)
            {
                codeBuilder.InsertCode(
                    string.Format(CultureInfo.InvariantCulture,
@"            foreach(var item in updatedNew)
                item.{0} = _executionContext.NHibernateSession.Load<{1}>(item.ID);
            foreach(var item in deletedIds)
                item.{0} = _executionContext.NHibernateSession.Load<{1}>(item.ID);

", ExtensionPropertyName(info), info.Extension.GetKeyProperties()),
                    WritableOrmDataStructureCodeGenerator.InitializationTag.Evaluate(info.Base));
            }
        }

        public static string ExtensionPropertyName(DataStructureExtendsInfo info)
        {
            if (info.Base.Module == info.Extension.Module)
                return "Extension_" + info.Extension.Name;
            return "Extension_" + info.Extension.Module.Name + "_" + info.Extension.Name;
        }
    }
}
