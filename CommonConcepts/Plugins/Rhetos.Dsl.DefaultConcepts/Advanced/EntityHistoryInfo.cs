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
using Rhetos.Compiler;
using System.Globalization;

namespace Rhetos.Dsl.DefaultConcepts
{
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("History")]
    public class EntityHistoryInfo : IMacroConcept, IAlternativeInitializationConcept
    {
        [ConceptKey]
        public EntityInfo Entity { get; set; }

        // EntityHistory's CodeGenerator and DatabaseGenerator implementations depend on this concepts:
        public EntityInfo ChangesEntity { get; set; }
        public SqlQueryableInfo HistorySqlQueryable { get; set; }
        public SqlFunctionInfo AtTimeSqlFunction { get; set; }
        public WriteInfo Write { get; set; }
        
        public static readonly CsTag<EntityHistoryInfo> ClonePropertiesTag = "ClonePropertiesRewrite";

        public IEnumerable<string> DeclareNonparsableProperties()
        {
            return new string[] { "ChangesEntity", "HistorySqlQueryable", "AtTimeSqlFunction", "Write" };
        }

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            ChangesEntity = new EntityInfo { Module = Entity.Module, Name = Entity.Name + "_Changes" };
            HistorySqlQueryable = new SqlQueryableInfo { Module = Entity.Module, Name = Entity.Name + "_History", SqlSource = HistorySqlSnippet() };
            AtTimeSqlFunction = new SqlFunctionInfo { Module = Entity.Module, Name = Entity.Name + "_AtTime", Arguments = "@ContextTime DATETIME", Source = AtTimeSqlSnippet() };
            Write = new WriteInfo {
                    DataStructure = HistorySqlQueryable,
                    SaveImplementation = HistorySaveFunction()
                };

            createdConcepts = new IConceptInfo[] { ChangesEntity, HistorySqlQueryable, AtTimeSqlFunction, Write };        
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(IEnumerable<IConceptInfo> existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            // Expand the base entity:
            var activeSinceProperty = new DateTimePropertyInfo { DataStructure = Entity, Name = "ActiveSince" }; // TODO: SystemRequired, Default 1.1.1900.
            var activeSinceHistory = new EntityHistoryPropertyInfo { Property = activeSinceProperty };
            newConcepts.AddRange(new IConceptInfo[] { activeSinceProperty, activeSinceHistory });

            // DenySave for base entity: it is not allowed to save with ActiveSince older than last one used in History
            var denyFilter = new ComposableFilterByInfo {
                Parameter = "Common.OlderThanHistoryEntries",
                Source = Entity,
                Expression = String.Format(
                    @"(items, repository, parameter) => items.Where(item => 
                                repository.{0}.{1}_Changes.Query().Where(his => his.ActiveSince >= item.ActiveSince && his.Entity == item).Count() > 0)", 
                                Entity.Module.Name, 
                                Entity.Name) 
            };
            var denySaveValidation = new DenySaveForPropertyInfo {
                FilterType = "Common.OlderThanHistoryEntries", 
                Source = Entity, 
                ErrorMessage = "ActiveSince is not allowed to be older than last entry in history.", 
                DependedProperty = activeSinceProperty 
            };
            newConcepts.AddRange(new IConceptInfo[] { denyFilter, denySaveValidation, new ParameterInfo { Module = new ModuleInfo { Name = "Common" }, Name = "OlderThanHistoryEntries" } });

            // Create a new entity for history data:
            var currentProperty = new ReferencePropertyInfo { DataStructure = ChangesEntity, Name = "Entity", Referenced = Entity };
            var historyActiveSinceProperty = new DateTimePropertyInfo { DataStructure = ChangesEntity, Name = activeSinceProperty.Name };
            newConcepts.AddRange(new IConceptInfo[] {
                currentProperty,
                new ReferenceDetailInfo { Reference = currentProperty },
                new RequiredPropertyInfo { Property = currentProperty }, // TODO: SystemRequired
                new PropertyFromInfo { Destination = ChangesEntity, Source = activeSinceProperty },
                historyActiveSinceProperty,
                new UniquePropertiesInfo { DataStructure = ChangesEntity, Property1 = currentProperty, Property2 = historyActiveSinceProperty }
            });

            // DenySave for history entity: it is not allowed to save with ActiveSince newer than current entity
            var denyFilterHistory = new ComposableFilterByInfo
            {
                Parameter = "Common.NewerThanCurrentEntry",
                Source = ChangesEntity,
                Expression = @"(items, repository, parameter) => items.Where(item => item.ActiveSince > item.Entity.ActiveSince)"
            };
            var denySaveValidationHistory = new DenySaveForPropertyInfo
            {
                FilterType = "Common.NewerThanCurrentEntry",
                Source = ChangesEntity,
                ErrorMessage = "ActiveSince of history entry is not allowed to be newer than current entry.",
                DependedProperty = historyActiveSinceProperty
            };
            newConcepts.AddRange(new IConceptInfo[] { denyFilterHistory, denySaveValidationHistory, new ParameterInfo { Module = new ModuleInfo { Name = "Common" }, Name = "NewerThanCurrentEntry" } });

            // Create ActiveUntil SqlQueryable:
            var activeUntilSqlQueryable = new SqlQueryableInfo { Module = Entity.Module, Name = Entity.Name + "_ChangesActiveUntil", SqlSource = ActiveUntilSqlSnippet() };
            newConcepts.AddRange(new IConceptInfo[] {
                activeUntilSqlQueryable,
                new DateTimePropertyInfo { DataStructure = activeUntilSqlQueryable, Name = "ActiveUntil" },
                new SqlDependsOnDataStructureInfo { Dependent = activeUntilSqlQueryable, DependsOn = ChangesEntity },
                new SqlDependsOnDataStructureInfo { Dependent = activeUntilSqlQueryable, DependsOn = Entity },
                new DataStructureExtendsInfo { Base = ChangesEntity, Extension = activeUntilSqlQueryable }
            });

            // Configure History SqlQueryable:
            newConcepts.AddRange(new IConceptInfo[] {
                new SqlDependsOnDataStructureInfo { Dependent = HistorySqlQueryable, DependsOn = Entity },
                new SqlDependsOnDataStructureInfo { Dependent = HistorySqlQueryable, DependsOn = ChangesEntity },
                new SqlDependsOnDataStructureInfo { Dependent = HistorySqlQueryable, DependsOn = activeUntilSqlQueryable },
                new DateTimePropertyInfo { DataStructure = HistorySqlQueryable, Name = "ActiveUntil" },
                new AllPropertiesFromInfo { Source = ChangesEntity, Destination = HistorySqlQueryable }
            });

            // Configure AtTime SqlFunction:
            newConcepts.Add(new SqlDependsOnDataStructureInfo { Dependent = AtTimeSqlFunction, DependsOn = HistorySqlQueryable });

            return newConcepts;
        }

        private string ActiveUntilSqlSnippet()
        {
            return string.Format(
                @"SELECT
	                history.ID,
	                ActiveUntil = COALESCE(MIN(newerVersion.ActiveSince), MIN(currentItem.ActiveSince))
                FROM {0}.{2} history
	                LEFT JOIN {0}.{2} newerVersion ON 
				                newerVersion.EntityID = history.EntityID AND 
				                newerVersion.ActiveSince > history.ActiveSince
	                INNER JOIN {0}.{1} currentItem ON currentItem.ID = history.EntityID
                GROUP BY history.ID",
                    SqlUtility.Identifier(Entity.Module.Name),
                    SqlUtility.Identifier(Entity.Name),
                    SqlUtility.Identifier(ChangesEntity.Name));
        }

        private string HistorySqlSnippet()
        {
            return string.Format(
                @"SELECT
                    ID = entity.ID,
                    EntityID = entity.ID,
                    ActiveUntil = CAST(NULL AS DateTime){5}
                FROM
                    {0}.{1} entity

                UNION ALL

                SELECT
                    ID = history.ID,
                    EntityID = history.EntityID,
                    au.ActiveUntil{4}
                FROM
                    {0}.{2} history
                    LEFT JOIN {0}.{3} au ON au.ID = history.ID",
                SqlUtility.Identifier(Entity.Module.Name),
                SqlUtility.Identifier(Entity.Name),
                SqlUtility.Identifier(ChangesEntity.Name),
                SqlUtility.Identifier(Entity.Name + "_ChangesActiveUntil"),
                SelectHistoryPropertiesTag.Evaluate(this),
                SelectEntityPropertiesTag.Evaluate(this));
        }

        private string AtTimeSqlSnippet()
        {
            return string.Format(
                @"RETURNS TABLE
                AS
                RETURN
	                SELECT
                        ID = history.EntityID,
                        ActiveUntil,
                        EntityID = history.EntityID{2}
                    FROM
                        {0}.{1} history
                        INNER JOIN
                        (
                            SELECT EntityID, Max_ActiveSince = MAX(ActiveSince)
                            FROM {0}.{1}
                            WHERE ActiveSince <= @ContextTime
                            GROUP BY EntityID
                        ) last ON last.EntityID = history.EntityID AND last.Max_ActiveSince = history.ActiveSince",
                    SqlUtility.Identifier(Entity.Module.Name),
                    SqlUtility.Identifier(Entity.Name + "_History"),
                    SelectHistoryPropertiesTag.Evaluate(this));
        }

        public static readonly SqlTag<EntityHistoryInfo> SelectHistoryPropertiesTag = "SelectHistoryProperties";
        public static readonly SqlTag<EntityHistoryInfo> SelectEntityPropertiesTag = "SelectEntityProperties";

        private string HistorySaveFunction()
        {
            return String.Format(@"var updateEnt = new List<{0}_History>();
            var deletedEnt = new List<{0}_History>();
            var insertedEnt = new List<{0}_History>();

            var updateHist = new List<{0}_History>();
            var deletedHist = new List<{0}_History>();
            var insertedHist = new List<{0}_History>();

            foreach(var item in insertedNew)
                if(item.EntityID == Guid.Empty)
                    throw new Rhetos.UserException(""Inserting into History is not allowed because property EntityID is not set."");

            _executionContext.NHibernateSession.Clear(); // Updating a modified persistent object could break old-data validations such as checking for locked items.
            
            Guid[] distinctEntityIDs = insertedNew.Union(updatedNew).Select(x => x.EntityID.Value).Distinct().ToArray();
            Guid[] existingEntities = _domRepository.{1}.{0}.Filter(distinctEntityIDs).Select(x => x.ID).ToArray();
            var nonExistentEntity = distinctEntityIDs.Except(existingEntities).FirstOrDefault();
            if (nonExistentEntity != default(Guid))
                throw new Rhetos.UserException(""Insert or update of History is not allowed because there is no entity with EntityID: "" + nonExistentEntity.ToString());
            
            // INSERT
            insertedEnt.AddRange(insertedNew
                .Where(newItem => _domRepository.{1}.{0}.Filter(new[]{{newItem.EntityID.Value}}).SingleOrDefault().ActiveSince < newItem.ActiveSince)
                .ToArray());
            insertedHist.AddRange(insertedNew
                .Where(newItem => _domRepository.{1}.{0}.Filter(new[]{{newItem.EntityID.Value}}).SingleOrDefault().ActiveSince >= newItem.ActiveSince)
                .ToArray());

            // UPDATE
            updateEnt.AddRange(updatedNew
                .Where(item => item.ID == item.EntityID)
                .ToArray());
                
            updateHist.AddRange(updatedNew
                .Where(item => item.ID != item.EntityID)
                .ToArray());
                
            // DELETE
            deletedHist.AddRange(deletedIds
                .Where(item => _domRepository.{1}.{0}_Changes.Filter(new[]{{item.ID}}).Any())
                .ToArray());
                
            var deletingHistIds = deletedHist.Select(hist => hist.ID).ToArray();
            deletedEnt.AddRange(deletedIds
                .Where(item => _domRepository.{1}.{0}.Filter(new[]{{item.ID}}).Any())
                .Where(item => !(_domRepository.{1}.{0}_Changes.Query().Any(hist => hist.Entity.ID == item.ID && !deletingHistIds.Contains(hist.ID))))
                .ToArray());

            var histBackup = deletedIds
                .Where(item => _domRepository.{1}.{0}.Filter(new[]{{item.ID}}).Any())
                .Where(item => _domRepository.{1}.{0}_Changes.Query().Any(hist => hist.Entity.ID == item.ID && !deletingHistIds.Contains(hist.ID)))
                .Select(item => _domRepository.{1}.{0}_History.Query()
                        .Where(fh => fh.Entity.ID == item.ID && fh.ID != item.ID && !deletingHistIds.Contains(fh.ID))
                        .OrderByDescending(fh => fh.ActiveSince)
                        .Take(1).Single())
                .ToArray();
                
            updateEnt.AddRange(histBackup);
            deletedHist.AddRange(histBackup);

            // SAVE IN BASE AND HISTORY
            _domRepository.{1}.{0}_Changes.Save(
                    insertedHist.Select(item => {{
                        var ret = new {0}_Changes();
                        ret.ID = item.ID;
                        ret.EntityID = item.EntityID;{2}
                        return ret;
                    }}).ToArray()
                    ,updateHist.Select(item => {{
                        var ret = _domRepository.{1}.{0}_Changes.Filter(new [] {{item.ID}}).Single();{2}
                        return ret;
                    }}).ToArray()
                    , _domRepository.{1}.{0}_Changes.Filter(deletedHist.Select(de => de.ID).ToArray()));

            _domRepository.{1}.{0}.Save(null
                , insertedEnt.Select(item => {{
                        var ret = _domRepository.{1}.{0}.Filter(new [] {{item.EntityID.Value}}).Single();{2}
                        return ret;
                    }}).ToArray().Concat(
                        updateEnt.Select(item => {{
                            var ret = _domRepository.{1}.{0}.Filter(new [] {{item.EntityID.Value}}).Single();
                            ret.SetOverwriteCurrentRecordOnUpdate(true);{2}
                            return ret;
                        }}).ToArray()
                    )
                , deletedEnt.Select(de => new {1}.{0} {{ ID = de.EntityID.Value }}).ToArray());

            ",
             Entity.Name,
             Entity.Module.Name,
             ClonePropertiesTag.Evaluate(this));
        }
    }
}
