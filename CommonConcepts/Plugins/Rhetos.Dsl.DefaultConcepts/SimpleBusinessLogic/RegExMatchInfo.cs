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
using Rhetos.Utilities;
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RegExMatch")]
    public class RegExMatchInfo : IMacroConcept, IValidatedConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string RegularExpression { get; set; }

        public string ErrorMessage { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var filterParameter = new ParameterInfo
            {
                Module = Property.DataStructure.Module,
                Name = Property.Name + "_RegExMatchFilter",
            };

            string filterExpression = string.Format(@"(source, repository, parameter) =>
                {{
                    var items = source.Where(item => !string.IsNullOrEmpty(item.{0})).Select(item => new {{ item.ID, item.{0} }}).ToList();
                    var regex = new System.Text.RegularExpressions.Regex({1});
                    var invalidItemIds = items.Where(item => !regex.IsMatch(item.{0})).Select(item => item.ID).ToList();
                    return Filter(source, invalidItemIds);
                }}",
                Property.Name,
                CsUtility.QuotedString("^" + RegularExpression + "$"));

            var itemFilter = new ComposableFilterByInfo
            {
                Expression = filterExpression,
                Parameter = filterParameter.Module.Name + "." + filterParameter.Name,
                Source = Property.DataStructure
            };
            var invalidData = new InvalidDataInfo
            {
                Source = Property.DataStructure,
                FilterType = itemFilter.Parameter,
                ErrorMessage = ErrorMessage
            };
            var invalidProperty = new InvalidDataMarkProperty2Info
            {
                InvalidData = invalidData,
                MarkProperty = Property
            };
            return new IConceptInfo[] { filterParameter, itemFilter, invalidData, invalidProperty };
        }

        public void CheckSemantics(IDslModel existingConcepts)
        {
            try
            {
                new Regex(RegularExpression);
            }
            catch (Exception ex)
            {
                var msg = "Invalid format of the regular expression.";
                throw new DslSyntaxException(this, msg, ex);
            }
        }
    }
}
