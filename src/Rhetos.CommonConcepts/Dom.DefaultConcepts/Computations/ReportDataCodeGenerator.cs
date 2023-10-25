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
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReportDataInfo))]
    public class ReportDataCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<ReportDataInfo> GetReportDataTag = "GetReportData";
        public static readonly CsTag<ReportDataInfo> DataSourceNameTag = "DataSourceName";

        private static string RepositoryFunctionsSnippet(ReportDataInfo info)
        {
            return string.Format(
        @"public object[][] GetReportData({0}.{1} parameter)
        {{
	        var reportData = new Dictionary<string, object[]>();
	        {2}

	        return reportData.OrderBy(i => i.Key).Select(i => i.Value).ToArray();
        }}

        public IList<string> DataSourcesNames {{ get {{ return _dataSourcesNames; }} }}
        private readonly static string[] _dataSourcesNames = new string[] {{ {3} }};

        ",
            info.Module.Name,
            info.Name,
            GetReportDataTag.Evaluate(info),
            DataSourceNameTag.Evaluate(info));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ReportDataInfo)conceptInfo;

            RepositoryHelper.GenerateRepository(info, codeBuilder);
            codeBuilder.InsertCode(RepositoryFunctionsSnippet(info), RepositoryHelper.RepositoryMembers, info);
        }
    }
}
