﻿/*
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
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(ReportDataSourceInfo))]
    public class ReportDataSourceCodeGenerator : IConceptCodeGenerator
    {
        private static string GetReportDataSnippet(ReportDataSourceInfo info)
        {
            return string.Format(@"
	        {{
                var dataSourceRepositiory = _domRepository.{1} as IFilterRepository<{0}, {1}>;
                if (dataSourceRepositiory == null)
                    throw new Rhetos.UserException(""Report's data source '{1}' does not implement the filter for '{0}'."");
                
                var data = dataSourceRepositiory.Filter(parameter);
                var order = {2};
	            reportData.Add(order, data);
            }}
",
            info.Report.GetKeyProperties(),
            info.DataSource.GetKeyProperties(),
            CsUtility.QuotedString(info.Order));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (ReportDataSourceInfo)conceptInfo;
            codeBuilder.InsertCode(GetReportDataSnippet(info), ReportDataCodeGenerator.GetReportDataTag, info.Report);
        }
    }
}
