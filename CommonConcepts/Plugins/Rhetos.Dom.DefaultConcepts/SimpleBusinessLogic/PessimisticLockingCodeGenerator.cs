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
        public static readonly DataStructureCodeGenerator.DataStructureTag AdditionalLocksTag =
            new DataStructureCodeGenerator.DataStructureTag(TagType.Appendable, "/*PessimisticLocking AdditionalLocks {0}.{1}*/");

        private string CheckLocksCodeSnippet(PessimisticLockingInfo info)
        {
            return string.Format(
@"            {{
                // Check pessimistic locking:

                Common.ExclusiveLock deniedLock = null;

                Guid[] resourceIDs = updated.Select(item => item.ID).Concat(deleted.Select(item => item.ID)).ToArray();
                if (resourceIDs.Count() > 0)
                {{
                    var queryLock = _domRepository.Common.ExclusiveLock.Query().Where(itemLock =>
                        itemLock.ResourceType == ""{0}.{1}""
                        && resourceIDs.Contains(itemLock.ResourceID.Value)
                        && itemLock.LockFinish >= DateTime.Now);

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
                        lockInfo = ""Locked record "" + deniedLock.ResourceType + "", ID "" + deniedLock.ResourceID + ""."";
                    else
                        lockInfo = ""Locked record "" + deniedLock.ResourceType + "", ID "" + deniedLock.ResourceID + "" prevents the changes in '{0}.{1}'."";

                    if (_executionContext.UserInfo.IsUserRecognized
                        && deniedLock.UserName.Equals(_executionContext.UserInfo.UserName, StringComparison.InvariantCultureIgnoreCase))
                            throw new Rhetos.UserException(""It is not allowed to enter the record at client workstation '"" + _executionContext.UserInfo.Workstation
                            + ""' because the data entry is in progress at workstation '"" + deniedLock.Workstation + ""'.""
                            + ""\r\n"" + lockInfo);
                    else
                        throw new Rhetos.UserException(""It is not allowed to enter the record because the data entry is in progress by user '"" + deniedLock.UserName + ""'.""
                            + ""\r\n"" + lockInfo);
                }}
            }}
",
                info.Resource.Module.Name,
                info.Resource.Name,
                AdditionalLocksTag.Evaluate(info.Resource));
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
