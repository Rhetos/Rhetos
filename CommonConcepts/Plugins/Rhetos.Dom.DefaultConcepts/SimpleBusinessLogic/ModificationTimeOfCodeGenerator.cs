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

using Rhetos.Compiler;
using Rhetos.Dsl;
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
    [ExportMetadata(MefProvider.Implements, typeof(ModificationTimeOfInfo))]
    public class ModificationTimeOfCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(Dsl.IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ModificationTimeOfInfo)conceptInfo;

            string timeProperty = info.Property.GetSimplePropertyName();
            string modifiedProperty = info.ModifiedProperty.GetSimplePropertyName();

            string snippet =
            $@"{{ 
                var now = SqlUtility.GetDatabaseTime(_executionContext.SqlExecuter);

                foreach (var newItem in insertedNew)
                    if(newItem.{timeProperty} == null)
                        newItem.{timeProperty} = now;

                var modifiedItems = updatedOld
					.Zip(updatedNew, (oldValue, newValue) => new {{ oldValue, newValue }})
					.Where(modified => modified.oldValue.{modifiedProperty} == null && modified.newValue.{modifiedProperty} != null
                        || modified.oldValue.{modifiedProperty} != null && !modified.oldValue.{modifiedProperty}.Equals(modified.newValue.{modifiedProperty}));

                foreach (var modified in modifiedItems)
                    modified.newValue.{timeProperty} = now;
            }}
            ";

            codeBuilder.InsertCode(snippet, WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Property.DataStructure);
        }
    }
}
