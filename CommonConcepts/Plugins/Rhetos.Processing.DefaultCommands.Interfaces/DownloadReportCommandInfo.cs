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
using System.ComponentModel.Composition;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandInfo))]
    public class DownloadReportCommandInfo : ICommandInfo
    {
        /// <summary>
        /// Instance of the report type.
        /// </summary>
        public object Report { get; set; }
        /// <summary>
        /// Typically a file extension ("pdf", for example), or null if no conversion of report file type is needed.
        /// </summary>
        public string ConvertFormat { get; set; }

        public override string ToString()
        {
            return GetType().Name
                + (Report != null ? " " + Report.GetType().FullName : "")
                + (string.IsNullOrEmpty(ConvertFormat) ? "" : " as " + ConvertFormat);
        }
    }

    public class DownloadReportCommandResult : ICommandData
    {
        public byte[] ReportFile { get; set; }
        public string SuggestedFileName { get; set; }

        [Obsolete]
        public object Value
        {
            get { return this; }
        }
    }
}
