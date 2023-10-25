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
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(PessimisticLockingParentInfo))]
    public class PessimisticLockingParentCodeGenerator : IConceptCodeGenerator
    {
        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (PessimisticLockingParentInfo)conceptInfo;
            var master = info.Reference.Referenced;

            string snippet = $@"if (deniedLock == null)
                {{
                    // Check locks for parent {master.FullName}:
                    Guid[] parentResourceIDs = updated.Where(item => item.{info.Reference.Name}ID != null).Select(item => item.{info.Reference.Name}ID.Value)
                        .Concat(deleted.Where(item => item.{info.Reference.Name}ID != null).Select(item => item.{info.Reference.Name}ID.Value))
                        .Concat(insertedNew.Where(item => item.{info.Reference.Name}ID != null).Select(item => item.{info.Reference.Name}ID.Value))
                        .Concat(updatedNew.Where(item => item.{info.Reference.Name}ID != null).Select(item => item.{info.Reference.Name}ID.Value))
                            .Distinct().ToArray();

                    if (parentResourceIDs.Count() > 0)
                    {{
                        var now = SqlUtility.GetDatabaseTime(_executionContext.SqlExecuter);
                        var queryParentLock = _domRepository.Common.ExclusiveLock.Query().Where(itemLock =>
                            itemLock.ResourceType == ""{master.FullName}""
                            && parentResourceIDs.Contains(itemLock.ResourceID.Value)
                            && itemLock.LockFinish >= now);

                        if (_executionContext.UserInfo.IsUserRecognized)
                            queryParentLock = queryParentLock.Where(itemLock =>
                                itemLock.UserName != _executionContext.UserInfo.UserName
                                || itemLock.Workstation != _executionContext.UserInfo.Workstation);

                        deniedLock = queryParentLock.FirstOrDefault();
                    }}
                }}
                
                ";

            codeBuilder.InsertCode(snippet, PessimisticLockingCodeGenerator.AdditionalLocksTag, info.Detail);
        }
    }
}
