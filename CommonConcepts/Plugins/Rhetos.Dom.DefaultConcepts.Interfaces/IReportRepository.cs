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

namespace Rhetos.Dom.DefaultConcepts
{
    public interface IReportRepository : IRepository
    {
        /// <summary>
        /// Returns generated binary file and suggested file name.
        /// Set convertFormat to "pdf" to convert result to pdf.
        /// Set convertFormat to null to get the original report template file format (docx or xls, for example).
        /// </summary>
        ReportFile GenerateReport(object parameters, string convertFormat);

        IList<string> DataSourcesNames { get; }
    }
}
