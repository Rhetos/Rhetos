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
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("RowPermissions")]
    public class RowPermissionsInfo : ComposableFilterByInfo, IMacroConcept, IAlternativeInitializationConcept
    {
        public static ParameterInfo FilterParameter = new ParameterInfo { Module = new ModuleInfo { Name = "Common" }, Name = "RowPermissionsAllowedItems" };

        public string SimplifiedExpression { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { "Parameter", "Expression" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Parameter = FilterParameter.GetKeyProperties();
            Expression = ReformatLambdaExpression(SimplifiedExpression);
            createdConcepts = null;
        }

        // ugly workaround to eliminate unnecessary parameter which ComposableFilterBy expects
        // we don't need parameter in row permission filter/expression
        string ReformatLambdaExpression(string expression)
        {
            Regex regex = new Regex(@"^\((.+?),(.+?),(.+?)\).*?=>(.*)$", RegexOptions.Singleline);
            Match match = regex.Match(expression);
            if (match.Groups.Count != 5)
                throw new DslSyntaxException("RowPermissions expression format is not valid: " + expression);

            string source = match.Groups[1].Value, repo = match.Groups[2].Value, context = match.Groups[3].Value,
                expr = match.Groups[4].Value;

            string reformatted = string.Format("({0}, {1}, __obsoleteParameter, {2}) => {3}",
                source, repo, context, expr);

            return reformatted;
        }

        public new IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();
            newConcepts.AddRange(base.CreateNewConcepts(existingConcepts));

            newConcepts.Add(FilterParameter);
            newConcepts.Add(new ComposableFilterUseExecutionContextInfo() { Filter = this });

            return newConcepts;
        }
    }
}
