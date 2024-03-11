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

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// List of readable data structures that provide the data for the report. The module name is optional.
    ///Each provided data source should have a FilterBy implementation with the filter parameter name same as the report name.
    ///It is recommended to use FilterByBase, FilterByReferenced and FilterByLinkedItems,
    ///to avoid writing redundant filters on related structures(e.g.report may filtered documents and the related detail items).
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("DataSources")]
    public class ReportDataSourcesInfo : IMacroConcept
    {
        [ConceptKey]
        public ReportDataInfo Report { get; set; }

        public string DataSources { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            var dataSourceNames = DataSources.Split(',').Select(ds => ds.Trim()).ToList();

            var sources = dataSourceNames
                .Select(name => name.Split('.'))
                .Select(nameParts =>
                            {
                                if (nameParts.Length == 2)
                                    return new DataStructureInfo {Module = new ModuleInfo {Name = nameParts[0]}, Name = nameParts[1]};
                                if (nameParts.Length == 1)
                                    return new DataStructureInfo {Module = Report.Module, Name = nameParts[0]};

                                throw new DslSyntaxException("Invalid use of " + this.GetUserDescription()
                                    + ". Data structure '" + string.Join(".", nameParts) + "' does not exist.");
                            });

            var digits = dataSourceNames.Count.ToString().Length;

            return sources.Select((source, index) =>
                                  new ReportDataSourceInfo
                                      {
                                          Report = Report,
                                          DataSource = source,
                                          Order = index.ToString("D" + digits)
                                      }).ToList();
        }
    }
}
