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
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Security;
using Rhetos.Dom.DefaultConcepts;
using Autofac.Features.Indexed;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(IClaimProvider))]
    [ExportMetadata(MefProvider.Implements, typeof(DownloadReportCommandInfo))]
    public class DownloadReportCommandClaims : IClaimProvider
    {
        public IList<Claim> GetRequiredClaims(ICommandInfo commandInfo)
        {
            var info = (DownloadReportCommandInfo)commandInfo;
            string reportName = info.Report.GetType().FullName;

            return new[] { new Claim(reportName, "DownloadReport") };
        }

        public IList<Claim> GetAllClaims(IDslModel dslModel)
        {
            var allReports = dslModel.Concepts.OfType<ReportDataInfo>(); // TODO: Change ReportDataInfo to ReportFileInfo, after modifying TemplaterReportInfo to inherit ReportFileInfo.
            return allReports.Select(report => new Claim(report.Module.Name + "." + report.Name, "DownloadReport")).ToArray();
        }
    }
}