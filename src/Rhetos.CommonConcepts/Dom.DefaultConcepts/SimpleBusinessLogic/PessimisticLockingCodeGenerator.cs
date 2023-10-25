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
    [ExportMetadata(MefProvider.Implements, typeof(PessimisticLockingInfo))]
    public class PessimisticLockingCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<PessimisticLockingInfo> AdditionalLocksTag = "AdditionalLocks";

        private string CheckLocksCodeSnippet(PessimisticLockingInfo info)
        {
            return string.Format(
            @"{{
                // Check pessimistic locking:

                Common.ExclusiveLock deniedLock = null;

                Guid[] resourceIDs = updated.Select(item => item.ID).Concat(deleted.Select(item => item.ID)).ToArray();
                if (resourceIDs.Count() > 0)
                {{
                    var now = SqlUtility.GetDatabaseTime(_executionContext.SqlExecuter);
                    var queryLock = _domRepository.Common.ExclusiveLock.Query().Where(itemLock =>
                        itemLock.ResourceType == ""{0}.{1}""
                        && resourceIDs.Contains(itemLock.ResourceID.Value)
                        && itemLock.LockFinish >= now);

                    if (_executionContext.UserInfo.IsUserRecognized)
                        queryLock = queryLock.Where(itemLock =>
                            itemLock.UserName != _executionContext.UserInfo.UserName
                            || itemLock.Workstation != _executionContext.UserInfo.Workstation);

                    deniedLock = queryLock.FirstOrDefault();
                }}

                {2}

                if (deniedLock != null)
                {{
                    string lockInfo;
                    if (deniedLock.ResourceType == ""{0}.{1}"")
                        lockInfo = _localizer[""Locked record {{0}}, ID {{1}}."",
                            deniedLock.ResourceType, deniedLock.ResourceID];
                    else
                        lockInfo = _localizer[""Locked record {{0}}, ID {{1}} prevents the changes in '{{2}}'."",
                            deniedLock.ResourceType, deniedLock.ResourceID, ""{0}.{1}""];

                    string errorInfo;
                    if (_executionContext.UserInfo.IsUserRecognized
                        && deniedLock.UserName.Equals(_executionContext.UserInfo.UserName, StringComparison.InvariantCultureIgnoreCase))
                            errorInfo = _localizer[""It is not allowed to enter the record at client workstation '{{0}}' because the data entry is in progress at workstation '{{1}}'."",
                                _executionContext.UserInfo.Workstation, deniedLock.Workstation];
                    else
                        errorInfo = _localizer[""It is not allowed to enter the record because the data entry is in progress by user '{{0}}'."",
                            deniedLock.UserName];

                    string localizedMessage = errorInfo + ""\r\n"" + lockInfo;
                    throw new Rhetos.UserException(localizedMessage);
                }}
            }}
            ",
                info.Resource.Module.Name,
                info.Resource.Name,
                AdditionalLocksTag.Evaluate(info));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (PessimisticLockingInfo)conceptInfo;

            if (info.Resource is IWritableOrmDataStructure)
                codeBuilder.InsertCode(CheckLocksCodeSnippet(info), WritableOrmDataStructureCodeGenerator.OldDataLoadedTag, info.Resource);
            else
                throw new FrameworkException("PessimisticLocking is not supported on '" + info.Resource.GetShortDescription() + "', it is not IWritableOrmDataStructure.'");
        }
    }
}
