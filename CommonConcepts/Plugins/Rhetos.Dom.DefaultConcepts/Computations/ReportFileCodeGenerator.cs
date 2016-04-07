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
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReportFileInfo))]
    public class ReportFileCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ReportFileInfo> BeforePrepareDataTag = "BeforePrepareData";
        public static readonly CsTag<ReportFileInfo> BeforeGenerateReportTag = "BeforeGenerateReport";
        public static readonly CsTag<ReportFileInfo> AfterGenerateReportTag = "AfterGenerateReport";

        private static string RepositoryFunctionsSnippet(ReportFileInfo info)
        {
            // TODO: Implement UseExecutionContext concept for reports, then remove the predefined parameter executionContext from _ReportFile_FileGenerator.
            return string.Format(
        @"protected static Func<object[][], string, Common.ExecutionContext, Rhetos.Dom.DefaultConcepts.ReportFile> _ReportFile_FileGenerator = {2};

        public Rhetos.Dom.DefaultConcepts.ReportFile GenerateReport({0}.{1} parameter, string convertFormat = null)
        {{
            {3}
            object[][] reportData = GetReportData(parameter);
            {4}
            Rhetos.Dom.DefaultConcepts.ReportFile file = _ReportFile_FileGenerator(reportData, convertFormat, _executionContext);
            {5}

            return file;
        }}

        Rhetos.Dom.DefaultConcepts.ReportFile IReportRepository.GenerateReport(object parameters, string convertFormat)
        {{
            return GenerateReport(({0}.{1}) parameters, convertFormat);
        }}

        ",
            info.Module.Name,
            info.Name,
            info.Expression,
            BeforePrepareDataTag.Evaluate(info),
            BeforeGenerateReportTag.Evaluate(info),
            AfterGenerateReportTag.Evaluate(info));
        }

        public static string RegisterRepository(ReportFileInfo info)
        {
            return string.Format(@"builder.RegisterType<{0}._Helper.{1}_Repository>().Keyed<IReportRepository>(""{0}.{1}"").InstancePerLifetimeScope();
            ", info.Module.Name, info.Name);
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ReportFileInfo)conceptInfo;

            codeBuilder.InsertCode(RepositoryFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
            codeBuilder.InsertCode("IReportRepository", RepositoryHelper.RepositoryInterfaces, info);
            codeBuilder.InsertCode(RegisterRepository(info), ModuleCodeGenerator.CommonAutofacConfigurationMembersTag);

            codeBuilder.AddReferencesFromDependency(typeof(ReportFile));
            codeBuilder.AddReferencesFromDependency(typeof(IReportRepository));
        }
    }
}
