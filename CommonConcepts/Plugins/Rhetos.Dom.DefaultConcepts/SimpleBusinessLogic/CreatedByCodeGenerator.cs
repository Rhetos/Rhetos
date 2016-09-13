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
    [ExportMetadata(MefProvider.Implements, typeof(CreatedByInfo))]
    public class CreatedByCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(Dsl.IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (CreatedByInfo)conceptInfo;

            string createdByValue =
            @"{
                var userName = _executionContext.UserInfo.UserName;
                var userIds = _domRepository.Common.Principal.Query(p => p.Name == userName).Select(p => p.ID).ToList();

                if (userIds.Count > 1)
                    throw new Rhetos.UserException(""The system is not configured properly: There are multiple principals with the same username '{0}'. Please contact your system administrator."", new[] { userName }, null, null);
                if (userIds.Count == 0)
                    throw new Rhetos.UserException(""The system is not configured properly: There are no principals with username '{0}'. Please contact your system administrator."", new[] { userName }, null, null);
                Guid userId = userIds.Single();

                foreach (var newItem in insertedNew)
                    if(newItem." + info.Property.Name + @"ID == null)
                        newItem." + info.Property.Name + @"ID = userId;
            }
            ";

            codeBuilder.InsertCode(createdByValue, WritableOrmDataStructureCodeGenerator.InitializationTag, info.Property.DataStructure);
        }
    }
}
