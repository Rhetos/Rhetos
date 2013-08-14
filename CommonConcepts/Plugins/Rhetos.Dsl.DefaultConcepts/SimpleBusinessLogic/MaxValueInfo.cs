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
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using Rhetos.Utilities;
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("MaxValue")]
    public class MaxValueInfo : IMacroConcept, IValidationConcept
    {
        [ConceptKey]
        public PropertyInfo Property { get; set; }

        public string Value { get; set; }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            string propertyPrepared = (this.Property is IntegerPropertyInfo) ? Value :
                                      (this.Property is DecimalPropertyInfo) ? "(decimal)" + Value :
                                      (this.Property is MoneyPropertyInfo) ? "(decimal)" + Value :
                                      (this.Property is DatePropertyInfo) ? String.Format(@"DateTime.Parse(""{0}"")", Value) :
                                      (this.Property is DateTimePropertyInfo) ? String.Format(@"DateTime.Parse(""{0}"")", Value) : "";
            // Expand the base entity:
            var itemFilterMinValueProperty = new ItemFilterInfo {
                    Expression = String.Format(@"item => item.{0} > {1}", Property.Name, propertyPrepared),
                    FilterName = Property.Name + "_MaxValueFilter", 
                    Source = Property.DataStructure 
            };
            var denySaveMinValueProperty = new DenySaveForPropertyInfo { 
                    DependedProperty = Property,
                    FilterType = itemFilterMinValueProperty.FilterName,
                    Title = String.Format("Maximum value of {0} is {1}.", Property.Name, Value), 
                    Source = Property.DataStructure 
            };
            return new IConceptInfo[] { itemFilterMinValueProperty, denySaveMinValueProperty };
        }

        private static readonly Regex DecimalChecker = new Regex(@"^[+-]?(\d+(\.\d*)?|\.\d+)$");

        public void CheckSemantics(IEnumerable<IConceptInfo> concepts)
        {

            switch (this.Property.GetType().Name)
            {
                case "IntegerPropertyInfo":
                    int i;
                    if (!Int32.TryParse(this.Value, out i))
                        throw new DslSyntaxException(this, "Value is not an integer.");
                    break;
                case "DecimalPropertyInfo":
                    if (!DecimalChecker.IsMatch(this.Value))
                        throw new DslSyntaxException(this, "Value is not an valid decimal (use period as decimal separator).");
                    break;
                case "MoneyPropertyInfo":
                    if (!DecimalChecker.IsMatch(this.Value))
                        throw new DslSyntaxException(this, "Value is not an valid decimal (use period as decimal separator).");
                    break;
                case "DatePropertyInfo":
                    DateTime i3;
                    if (!DateTime.TryParse(this.Value, out i3))
                        throw new DslSyntaxException(this, "Value is not an date.");
                    break;
                case "DateTimePropertyInfo":
                    DateTime i4;
                    if (!DateTime.TryParse(this.Value, out i4))
                        throw new DslSyntaxException(this, "Value is not an datetime.");
                    break;
                default:
                    throw new DslSyntaxException(this, "MaxValue can only be used on Integer, Decimal, Money, Date or DateTime.");
            }
           
        }
    }
}
