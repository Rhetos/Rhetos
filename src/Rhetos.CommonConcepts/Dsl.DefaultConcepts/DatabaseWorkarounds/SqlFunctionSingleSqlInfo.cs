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
using System.Text.RegularExpressions;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Stored function in database.
    /// The function is specified in a single SQL script.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlFunction")]
    public class SqlFunctionSingleSqlInfo: SqlFunctionInfo, IAlternativeInitializationConcept
    {
        /// <summary>
        /// Must start with "CREATE FUNCTION" or "CREATE OR ALTER FUNCTION".
        /// </summary>
        public string FullFunctionSource { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { nameof(Name), nameof(Arguments), nameof(Source) };
        }

        private static readonly Regex createPartRegex = new Regex(@"^[\s\n]*CREATE\s*(OR\s*ALTER)?\s*FUNCTION\b", RegexOptions.IgnoreCase);
        private static readonly Regex namePartRegex = new Regex(@"^[\s\n]*(\[?(?<module>\w+)\]?\s*\.\s*)?\[?(?<name>\w+)\]?", RegexOptions.IgnoreCase);
        private static readonly Regex parametersPartRegex = new Regex(@"^[\s\n]*\((?<params>(.|\n)*?)\)[\s\n]*\b(?<returns>RETURNS)\b", RegexOptions.IgnoreCase);
        private static readonly Regex goStatementRegex = new Regex(@"\n\s*GO\s*\n", RegexOptions.IgnoreCase);

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Match createPart = createPartRegex.Match(FullFunctionSource);
            if (!createPart.Success)
                throw new DslConceptSyntaxException(this, $"The SqlFunction script must start with \"CREATE FUNCTION\" or \"CREATE OR ALTER FUNCTION\". Module '{Module}', SqlFunction: {FullFunctionSource.Limit(50, "...")}.");

            string rest = FullFunctionSource.Substring(createPart.Index + createPart.Length);

            Match namePart = namePartRegex.Match(rest);
            if (!namePart.Success)
                throw new DslConceptSyntaxException(this, $"Cannot detect function name in the SQL script. Make sure its syntax is correct. Do not use comments before the function name. Rest: {rest.Limit(50, "...")}.");
            string moduleName = namePart.Groups["module"].Value;
            Name = namePart.Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new DslConceptSyntaxException(this, $"SqlFunction '{Name}' should be named with schema '{Module.Name}.{Name}', to match the DSL module where the SqlFunction is placed.");
            if (!string.Equals(moduleName, Module.Name, StringComparison.OrdinalIgnoreCase))
                throw new DslConceptSyntaxException(this, $"SqlFunction '{moduleName}.{Name}' should have schema '{Module.Name}' instead of '{moduleName}', to match the DSL module where the SqlFunction is placed.");

            rest = rest.Substring(namePart.Index + namePart.Length);

            Match parametersPart = parametersPartRegex.Match(rest);
            if (!parametersPart.Success)
                throw new DslConceptSyntaxException(this, $"Cannot detect beginning of the parameters and code block in function '{Module.Name}.{Name}'. Make sure the SQL script has a valid syntax. Rest: {rest.Limit(50, "...")}.");
            Arguments = parametersPart.Groups["params"].Value;
            string returns = parametersPart.Groups["returns"].Value.Trim(); // This is parametrized to match the letter case.

            rest = rest.Substring(parametersPart.Index + parametersPart.Length);

            Source = (returns + rest).TrimStart('\r', '\n').TrimEnd();
            if (goStatementRegex.IsMatch(Source))
                throw new DslConceptSyntaxException(this, $"Please remove \"GO\" statement from the SQL script, or use SqlObject instead of SqlFunction.");

            createdConcepts = null;
        }
    }
}
