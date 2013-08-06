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
using Rhetos.Compiler;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(CreationTimeInfo))]
    public class CreationTimeCodeGenerator : IConceptCodeGenerator
    {
        private string SetCreationTimeValue(CreationTimeInfo info)
        {
            return string.Format(
@"            {{ 
                var nowFull = SqlUtility.GetDatabaseTime(_executionContext.SqlExecuter);
                var now = new DateTime(nowFull.Year, nowFull.Month, nowFull.Day, nowFull.Hour, nowFull.Minute, nowFull.Second); // Rounding for NHibernate compatibility

                foreach (var newItem in insertedNew)
                    if(newItem.{0} == null)
                        newItem.{0} = now;
            }}
", info.Property.Name);
        }

        public void GenerateCode(Dsl.IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (CreationTimeInfo)conceptInfo;

            codeBuilder.InsertCode(SetCreationTimeValue(info), WritableOrmDataStructureCodeGenerator.InitializationTag, info.Property.DataStructure);
        }
    }
}
