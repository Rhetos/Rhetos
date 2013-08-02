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
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Security;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(IClaimProvider))]
    [ExportMetadata(MefProvider.Implements, typeof(DownloadReportCommandInfo))]
    public class DownloadReportCommandClaims : IClaimProvider
    {
        private readonly IDslModel _dslModel;

        public DownloadReportCommandClaims(IDslModel dslModel)
        {
            _dslModel = dslModel;
        }

        public IEnumerable<IClaim> GetRequiredClaims(ICommandInfo commandInfo, Func<string, string, IClaim> newClaim)
        {
            var info = (DownloadReportCommandInfo)commandInfo;

            var reportName = (info.Report.GetType().FullName).Split('.');
            var reportConceptInfo = _dslModel.FindByKey(new ReportDataInfo { Module = new ModuleInfo { Name = reportName[0] }, Name = reportName[1] }.GetKey());

            var dataSources = new List<DataStructureInfo>();
            dataSources.AddRange(_dslModel.Concepts.OfType<ReportDataSourceInfo>()
                .Where(ds => ds.Report == reportConceptInfo)
                .Select(ds => ds.DataSource));

            List<IClaim> claims = dataSources
                .Select(ds => ds.Module.Name + "." + ds.Name).Distinct()
                .Select(resource => newClaim(resource, "Read")).ToList();

            claims.Add(newClaim(info.Report.GetType().FullName, "DownloadReport"));

            return claims;
        }

        public IEnumerable<IClaim> GetAllClaims(IDslModel dslModel, Func<string, string, IClaim> newClaim)
        {
            var allReports = dslModel.Concepts.OfType<ReportDataInfo>(); // TODO: Change ReportDataInfo to ReportFileInfo, after modifying TemplaterReportInfo to inherit ReportFileInfo.
            return allReports.Select(report => newClaim(report.Module.Name + "." + report.Name, "DownloadReport")).ToArray();
        }
    }
}