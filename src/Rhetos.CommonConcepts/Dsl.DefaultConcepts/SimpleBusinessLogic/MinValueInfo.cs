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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Limits the smallest allowed value of the property.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("MinValue")]
    public class MinValueInfo : IMacroConcept, IValidatedConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string Value { get; set; }

        public static readonly IReadOnlyDictionary<Type, Func<string, string>> LimitSnippetByType = new Dictionary<Type,Func<string, string>>
        {
            { typeof(IntegerPropertyInfo), limit => $"int limit = {limit}" },
            { typeof(DecimalPropertyInfo), limit => $"decimal limit = {limit}M" },
            { typeof(MoneyPropertyInfo), limit => $"decimal limit = {limit}M" },
            { typeof(DatePropertyInfo), limit => $@"var limit = DateTime.Parse({CsUtility.QuotedString(limit)})" },
            { typeof(DateTimePropertyInfo), limit => $@"var limit = DateTime.Parse({CsUtility.QuotedString(limit)})" },
        };

        public IEnumerable<IConceptInfo> CreateNewConcepts()
        {
            string limitSnippet = LimitSnippetByType
                .Where(snippet => snippet.Key.IsInstanceOfType(Property))
                .Select(snippet => snippet.Value.Invoke(Value))
                .Single();

            var filterParameter = new ParameterInfo
            {
                Module = Property.DataStructure.Module,
                Name = Property.Name + "_MinValueFilter"
            };
            var filter = new QueryFilterExpressionInfo
            {
                Expression = $@"(items, parameter) => {{ {limitSnippet}; return items.Where(item => item.{Property.Name} != null && item.{Property.Name} < limit); }}",
                Parameter = filterParameter.Module.Name + "." + filterParameter.Name,
                Source = Property.DataStructure
            };
            var invalidData = new InvalidDataInfo
            {
                Source = Property.DataStructure,
                FilterType = filter.Parameter,
                ErrorMessage = "Minimum value of {0} is {1}."
            };
            var messageParameters = new InvalidDataMessageParametersConstantInfo
            {
                InvalidData = invalidData,
                MessageParameters = CsUtility.QuotedString(Property.Name) + ", " + CsUtility.QuotedString(Value)
            };
            var invalidProperty = new InvalidDataMarkProperty2Info
            {
                InvalidData = invalidData,
                MarkProperty = Property
            };
            return new IConceptInfo[] { filterParameter, filter, invalidData, messageParameters, invalidProperty };
        }

        private static readonly Regex DecimalChecker = new Regex(@"^[+-]?(\d+(\.\d*)?|\.\d+)$");

        public void CheckSemantics(IDslModel existingConcepts)
        {
            if (this.Property is IntegerPropertyInfo)
            {
                int i;
                if (!Int32.TryParse(this.Value, out i))
                    throw new DslSyntaxException(this, "Value is not an integer.");
            }
            else if (this.Property is DecimalPropertyInfo)
            {
                if (!DecimalChecker.IsMatch(this.Value))
                    throw new DslSyntaxException(this, "Value is not an valid decimal (use period as decimal separator).");
            }
            else if (this.Property is MoneyPropertyInfo)
            {
                if (!DecimalChecker.IsMatch(this.Value))
                    throw new DslSyntaxException(this, "Value is not an valid decimal (use period as decimal separator).");
            }
            else if (this.Property is DatePropertyInfo)
            {
                DateTime i3;
                if (!DateTime.TryParse(this.Value, out i3))
                    throw new DslSyntaxException(this, "Value is not an date.");
            }
            else if (this.Property is DateTimePropertyInfo)
            {
                DateTime i4;
                if (!DateTime.TryParse(this.Value, out i4))
                    throw new DslSyntaxException(this, "Value is not an datetime.");
            }
            else
                throw new DslSyntaxException(this, "MinValue can only be used on Integer, Decimal, Money, Date or DateTime.");
        }
    }
}
