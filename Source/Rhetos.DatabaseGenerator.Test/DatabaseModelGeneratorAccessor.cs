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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Logging;
using Rhetos.TestCommon;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator.Test
{
    public class DatabaseModelGeneratorAccessor : DatabaseModelGenerator, ITestAccessor
    {
        public DatabaseModelGeneratorAccessor(
            IPluginsContainer<IConceptDatabaseDefinition> plugins,
            IDslModel dslModel)
            : base(plugins, dslModel, new ConsoleLogProvider(), null)
        {
        }

        public List<NewConceptApplication> CreateNewApplications()
        {
            return (List<NewConceptApplication>)this.Invoke("CreateNewApplications");
        }

        public static string GetConceptApplicationSeparator(int scriptKey)
        {
            return (string)TestAccessorHelpers.Invoke<DatabaseModelGenerator>("GetConceptApplicationSeparator", scriptKey);
        }

        public static void ExtractCreateQueries(string generatedSqlCode, List<NewConceptApplication> newConceptApplications)
        {
            TestAccessorHelpers.Invoke<DatabaseModelGenerator>("ExtractCreateQueries", generatedSqlCode, newConceptApplications);
        }

        public static IEnumerable<Dependency> GetConceptApplicationDependencies(IEnumerable<Tuple<IConceptInfo, IConceptInfo, string>> conceptInfoDependencies, IEnumerable<ConceptApplication> conceptApplications)
        {
            return (IEnumerable<Dependency>)TestAccessorHelpers.Invoke<DatabaseModelGenerator>("GetConceptApplicationDependencies", conceptInfoDependencies, conceptApplications);
        }

        public static IEnumerable<Dependency> ExtractDependenciesFromConceptInfos(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            return (IEnumerable<Dependency>)TestAccessorHelpers.Invoke<DatabaseModelGenerator>("ExtractDependenciesFromConceptInfos", newConceptApplications);
        }

        public void ComputeDependsOn(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            this.Invoke("ComputeDependsOn", newConceptApplications);
        }
    }
}
