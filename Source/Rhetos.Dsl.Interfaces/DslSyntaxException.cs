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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos.Dsl
{
    [Serializable]
    public class DslSyntaxException : RhetosException
    {
        public readonly string ErrorCode;
        public readonly FilePosition FilePosition;
        public readonly string Details;

        public DslSyntaxException() { }
        public DslSyntaxException(string message) : base(message) { }
        public DslSyntaxException(string message, Exception inner) : base(message, inner) { }
        
        public DslSyntaxException(IConceptInfo concept, string additionalMessage) : base(concept.GetUserDescription() + ": " + additionalMessage) { }
        public DslSyntaxException(IConceptInfo concept, string additionalMessage, Exception inner) : base(concept.GetUserDescription() + ": " + additionalMessage, inner) { }

        protected DslSyntaxException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public DslSyntaxException(string message, string errorCode, DslScript dslScript, int positionBegin = 0, int positionEnd = 0, string additionalDetails = null)
            : base(message)
        {
            ErrorCode = errorCode;
            
            if (!string.IsNullOrEmpty(dslScript?.Path))
                FilePosition = new FilePosition(dslScript.Path, dslScript.Script, positionBegin, positionEnd);

            var detailsList = new List<string>();
            if (!string.IsNullOrEmpty(dslScript?.Script))
                detailsList.Add($"Syntax error at \"{ScriptPositionReporting.ReportPreviousAndFollowingTextInline(dslScript.Script, positionBegin)}\"");
            if (!string.IsNullOrEmpty(additionalDetails))
                detailsList.Add(additionalDetails);
            Details = string.Join("\r\n", detailsList);
        }

        public override string ToString() => ReportWithFilePositionAndDetails(base.ToString());

        public override string MessageForLog() => ReportWithFilePositionAndDetails(Message);

        private string ReportWithFilePositionAndDetails(string message)
        {
            var report = new StringBuilder();
            report.Append(message);

            if (!string.IsNullOrEmpty(FilePosition?.Path))
                report.AppendLine().Append(FilePosition.CanonicalOrigin);

            if (!string.IsNullOrEmpty(Details))
                report.AppendLine().AppendLine("Details:").Append(CsUtility.Indent(Details, 3));

            return report.ToString();
        }
    }
}
