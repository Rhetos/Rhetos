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
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Extensibility;
using Rhetos.Factory;
using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rhetos.DatabaseGenerator.Test
{
    class DatabaseGenerator_Accessor : DatabaseGenerator
    {
        public DatabaseGenerator_Accessor()
            : base(null, null, new NullPluginsContainer<IConceptDatabaseDefinition>(), null, new ConsoleLogProvider())
        {
        }

        public DatabaseGenerator_Accessor(IDslModel dslModel, PluginsContainer<IConceptDatabaseDefinition> plugins)
            : base(null, dslModel, plugins, null, new ConsoleLogProvider())
        {
        }

        new public static void AddConceptApplicationSeparator(ConceptApplication ca, CodeBuilder sqlCodeBuilder)
        {
            DatabaseGenerator.AddConceptApplicationSeparator(ca, sqlCodeBuilder);
        }

        new public static void ExtractCreateQueries(string generatedSqlCode, IEnumerable<ConceptApplication> toBeInserted)
        {
            DatabaseGenerator.ExtractCreateQueries(generatedSqlCode, toBeInserted);
        }

        new public void ComputeDependsOn(IEnumerable<NewConceptApplication> newConceptApplications)
        {
            base.ComputeDependsOn(newConceptApplications);
        }

        new public static void CalculateApplicationsToBeRemovedAndInserted(IEnumerable<ConceptApplication> oldApplications, IEnumerable<NewConceptApplication> newApplications, out List<ConceptApplication> toBeRemoved, out List<NewConceptApplication> toBeInserted, ILogger consoleLogger)
        {
            DatabaseGenerator.CalculateApplicationsToBeRemovedAndInserted(oldApplications, newApplications, out toBeRemoved, out toBeInserted, consoleLogger);
        }

        new public static List<Tuple<NewConceptApplication, NewConceptApplication>> GetDependencyPairs(IEnumerable<NewConceptApplication> conceptApplications)
        {
            return DatabaseGenerator.GetDependencyPairs(conceptApplications);
        }

        new public static List<Tuple<ConceptApplication, ConceptApplication>> GetDependencyPairs(IEnumerable<ConceptApplication> conceptApplications)
        {
            return DatabaseGenerator.GetDependencyPairs(conceptApplications);
        }

        new public List<NewConceptApplication> CreateNewApplications()
        {
            return base.CreateNewApplications();
        }

        new public static IEnumerable<Dependency> ExtractDependenciesFromConceptInfos(IEnumerable<NewConceptApplication> all)
        {
            return DatabaseGenerator.ExtractDependenciesFromConceptInfos(all);
        }

        new public static IEnumerable<Dependency> GetConceptApplicationDependencies(IEnumerable<Tuple<IConceptInfo, IConceptInfo>> conceptInfoDependencies, IEnumerable<ConceptApplication> conceptApplications)
        {
            return DatabaseGenerator.GetConceptApplicationDependencies(conceptInfoDependencies, conceptApplications);
        }
    }
}
