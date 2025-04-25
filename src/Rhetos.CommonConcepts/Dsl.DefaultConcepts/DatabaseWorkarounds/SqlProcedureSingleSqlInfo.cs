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
    /// Stored procedure in database.
    /// The procedure is specified in a single SQL script.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("SqlProcedure")]
    public class SqlProcedureSingleSqlInfo: SqlProcedureInfo, IAlternativeInitializationConcept
    {
        /// <summary>
        /// Must start with "CREATE PROCEDURE" or "CREATE OR ALTER PROCEDURE", and contain "AS" in its own line.
        /// </summary>
        public string FullProcedureSource { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new[] { nameof(Name), nameof(ProcedureArguments), nameof(ProcedureSource) };
        }

        private static readonly Regex createPartRegex = new Regex(@"^[\s\n]*CREATE\s*(OR\s*ALTER)?\s*PROCEDURE\b", RegexOptions.IgnoreCase);
        private static readonly Regex namePartRegex = new Regex(@"^[\s\n]*(\[?(?<module>\w+)\]?\s*\.\s*)?\[?(?<name>\w+)\]?", RegexOptions.IgnoreCase);
        private static readonly Regex parametersPartRegex = new Regex(@"^(?<params>(.|\n)*?)\s*\bAS\b\s*?\n", RegexOptions.IgnoreCase);
        private static readonly Regex goStatementRegex = new Regex(@"\n\s*GO\s*\n", RegexOptions.IgnoreCase);

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Match createPart = createPartRegex.Match(FullProcedureSource);
            if (!createPart.Success)
                throw new DslConceptSyntaxException(this, $"The SqlProcedure script must start with \"CREATE PROCEDURE\" or \"CREATE OR ALTER PROCEDURE\". Module '{Module}', SqlProcedure: {FullProcedureSource.Limit(50, "...")}.");

            string rest = FullProcedureSource.Substring(createPart.Index + createPart.Length);

            Match namePart = namePartRegex.Match(rest);
            if (!namePart.Success)
                throw new DslConceptSyntaxException(this, $"Cannot detect procedure name in the SQL script. Make sure its syntax is correct. Do not use comments before the procedure name. Rest: {rest.Limit(50, "...")}.");
            string moduleName = namePart.Groups["module"].Value;
            Name = namePart.Groups["name"].Value;
            if (string.IsNullOrWhiteSpace(moduleName))
                throw new DslConceptSyntaxException(this, $"Procedure '{Name}' should be named with schema '{Module.Name}.{Name}', to match the DSL module where the SqlProcedure is placed.");
            if (!string.Equals(moduleName, Module.Name, StringComparison.OrdinalIgnoreCase))
                throw new DslConceptSyntaxException(this, $"Procedure '{moduleName}.{Name}' should have schema '{Module.Name}' instead of '{moduleName}', to match the DSL module where the SqlProcedure is placed.");

            rest = rest.Substring(namePart.Index + namePart.Length);

            Match parametersPart = parametersPartRegex.Match(rest);
            if (!parametersPart.Success)
                throw new DslConceptSyntaxException(this, $"Cannot detect beginning of the code block in procedure '{Module.Name}.{Name}'. Make sure the script contains \"AS\" in its own line. Rest: {rest.Limit(50, "...")}.");
            ProcedureArguments = parametersPart.Groups["params"].Value.Trim();

            rest = rest.Substring(parametersPart.Index + parametersPart.Length);

            ProcedureSource = rest.TrimStart('\r', '\n').TrimEnd();
            if (goStatementRegex.IsMatch(ProcedureSource))
                throw new DslConceptSyntaxException(this, $"Please remove \"GO\" statement from the SQL script, or use SqlObject instead of SqlProcedure.");

            createdConcepts = null;
        }
    }
}
