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
using System.IO;
using System.Linq;
using System.Text;
using Autofac.Features.Indexed;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Processing.DefaultCommands
{
    [Export(typeof(ICommandImplementation))]
    [ExportMetadata(MefProvider.Implements, typeof(DownloadReportCommandInfo))]
    public class DownloadReportCommand : ICommandImplementation
    {
        private readonly IIndex<string, IReportRepository> _reportIndex;

        public DownloadReportCommand(IIndex<string, IReportRepository> reportIndex)
        {
            _reportIndex = reportIndex;
        }

        public CommandResult Execute(ICommandInfo commandInfo)
        {
            var info = (DownloadReportCommandInfo)commandInfo;

            string reportName = info.Report.GetType().FullName;
            IReportRepository reportRepository = _reportIndex[reportName];
            var generatedReportFile = reportRepository.GenerateReport(info.Report, info.ConvertFormat);

            return new CommandResult
            {
                Data = new DownloadReportCommandResult
                {
                    ReportFile =  generatedReportFile.Content,
                    SuggestedFileName = generatedReportFile.Name
                },
                Message = "Report generated: " + reportName + " to " + generatedReportFile.Name,
                Success = true
            };
        }
    }
}