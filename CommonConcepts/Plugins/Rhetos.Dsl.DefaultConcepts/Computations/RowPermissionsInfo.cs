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
    public class RowPermissionsInfo : IConceptInfo, IMacroConcept
    {
        public static string filterName = "RowPermissions_AllowedItems";

        [ConceptKey]
        public DataStructureInfo Source { get; set; }

        public string Expression { get; set; }

        // ugly workaround to eliminate unnecessary parameter which ComposableFilterBy expects
        // we don't need parameter in row permission filter/expression
        string ReformatLambdaExpression(string expression)
        {
            Regex regex = new Regex(@"^\((.+?),(.+?),(.+?)\).*?=>(.*)$");
            Match match = regex.Match(expression);
            if (match.Groups.Count != 5)
                throw new DslSyntaxException("RowPermissions expression format is not valid: " + expression);

            string source = match.Groups[1].Value, repo = match.Groups[2].Value, context = match.Groups[3].Value,
                expr = match.Groups[4].Value;

            string reformatted = string.Format("({0}, {1}, __obsoleteParameter, {2}) => {3}",
                source, repo, context, expr);

            return reformatted;
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newExpression = ReformatLambdaExpression(Expression);
            var filterParameter = new ParameterInfo() { Module = Source.Module, Name = filterName };
            var rpFilter = new ComposableFilterByInfo()
            {
                Source = Source,
                Parameter = filterParameter.GetKeyProperties(),
                Expression = newExpression
            };

            var rpFilterUseExecutionContext = new ComposableFilterUseExecutionContextInfo()
            {
                Filter = rpFilter
            };

            return new IConceptInfo[]
            {
                filterParameter,
                rpFilter,
                rpFilterUseExecutionContext,

                /*
                new ModuleExternalReferenceInfo
                {
                    Module = new ModuleInfo {Name = Source.Module.Name},
                    TypeOrAssembly = typeof (DslUtility).AssemblyQualifiedName
                }*/
            };
        }
    }
}
