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
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Rhetos.Dsl.DefaultConcepts
{
    /// <summary>
    /// Enables temporal data management on the entity: Automatically keeps old versions of each records.
    /// Allows reading record's state at a given point in time. Allow entering data values that were effective from a previous point in time.
    /// </summary>
    [Export(typeof(IConceptInfo))]
    [ConceptKeyword("History")]
    public class EntityHistoryInfo : IAlternativeInitializationConcept
    {
        [ConceptKey]
        public EntityInfo Entity { get; set; }

        public EntityInfo Dependency_ChangesEntity { get; set; }

        public IEnumerable<string> DeclareNonparsableProperties() => new[] { "Dependency_ChangesEntity" };

        public void InitializeNonparsableProperties(out IEnumerable<IConceptInfo> createdConcepts)
        {
            Dependency_ChangesEntity = new EntityInfo { Module = Entity.Module, Name = Entity.Name + "_Changes" };

            createdConcepts = new IConceptInfo[] { Dependency_ChangesEntity };
        }
    }

    [Export(typeof(IConceptMacro))]
    public class EntityHistoryMacro : IConceptMacro<EntityHistoryInfo>
    {
        public static readonly CsTag<EntityHistoryInfo> ClonePropertiesTag = "ClonePropertiesRewrite";
        public static readonly SqlTag<EntityHistoryInfo> SelectHistoryPropertiesTag = "SelectHistoryProperties";
        public static readonly SqlTag<EntityHistoryInfo> SelectEntityPropertiesTag = "SelectEntityProperties";

        private readonly string _dateTimeSqlColumnType;

        public EntityHistoryMacro(CommonConceptsDatabaseSettings databaseSettings)
        {
            // TODO: we inject DatabaseSetttings here, but correct solution would be to inject ConceptMetadata and use ConceptMetadata.GetColumnType to fetch SQL type of the column
            // we can't do that because of circular reference problem between projects and IDatabaseColumnType is not available
            // needs to be refactored after we rearrange/merge projects and remove circular dependency in question
            _dateTimeSqlColumnType = databaseSettings.UseLegacyMsSqlDateTime
                ? "DATETIME"
                : $"DATETIME2({databaseSettings.DateTimePrecision})";
        }

        public IEnumerable<IConceptInfo> CreateNewConcepts(EntityHistoryInfo conceptInfo, IDslModel existingConcepts)
        {
            var newConcepts = new List<IConceptInfo>();

            var atTimeSqlFunction = new SqlFunctionInfo { Module = conceptInfo.Entity.Module, Name = conceptInfo.Entity.Name + "_AtTime", Arguments = $"@ContextTime {_dateTimeSqlColumnType}", Source = AtTimeSqlSnippet(conceptInfo) };
            newConcepts.Add(atTimeSqlFunction);

            var historySqlQueryable = new SqlQueryableInfo { Module = conceptInfo.Entity.Module, Name = conceptInfo.Entity.Name + "_History", SqlSource = HistorySqlSnippet(conceptInfo) };
            newConcepts.Add(historySqlQueryable);

            var write = new WriteInfo { DataStructure = historySqlQueryable, SaveImplementation = HistorySaveFunction(conceptInfo) };
            newConcepts.Add(write);

            // Expand the base entity:
            var activeSinceProperty = new DateTimePropertyInfo { DataStructure = conceptInfo.Entity, Name = "ActiveSince" }; // TODO: SystemRequired, Default 1.1.1900., after implementing optimization for multiple simple property validations.
            var activeSinceHistory = new EntityHistoryPropertyInfo { Property = activeSinceProperty };
            newConcepts.AddRange(new IConceptInfo[] { activeSinceProperty, activeSinceHistory });

            // InvalidData for base entity: it is not allowed to save with ActiveSince older than last one used in History
            var denyFilter = new ComposableFilterByInfo {
                Parameter = "Common.OlderThanHistoryEntries",
                Source = conceptInfo.Entity,
                Expression = String.Format(
                    @"(items, repository, parameter) => items.Where(item => 
                                repository.{0}.{1}_Changes.Subquery.Where(his => his.ActiveSince >= item.ActiveSince && his.Entity == item).Count() > 0)",
                                conceptInfo.Entity.Module.Name,
                                conceptInfo.Entity.Name) 
            };
            var invalidDataValidation = new InvalidDataInfo {
                FilterType = "Common.OlderThanHistoryEntries", 
                Source = conceptInfo.Entity, 
                ErrorMessage = "ActiveSince is not allowed to be older than last entry in history."
            };
            newConcepts.AddRange(new IConceptInfo[]
                {
                    denyFilter,
                    invalidDataValidation,
                    new ParameterInfo { Module = new ModuleInfo { Name = "Common" }, Name = "OlderThanHistoryEntries" },
                    new InvalidDataMarkProperty2Info { InvalidData = invalidDataValidation, MarkProperty = activeSinceProperty }
                });

            // Create a new entity for history data:
            var currentProperty = new ReferencePropertyInfo { DataStructure = conceptInfo.Dependency_ChangesEntity, Name = "Entity", Referenced = conceptInfo.Entity };
            var historyActiveSinceProperty = new DateTimePropertyInfo { DataStructure = conceptInfo.Dependency_ChangesEntity, Name = activeSinceProperty.Name };
            newConcepts.AddRange(new IConceptInfo[] {
                currentProperty,
                new ReferenceDetailInfo { Reference = currentProperty },
                new RequiredPropertyInfo { Property = currentProperty }, // TODO: SystemRequired, after implementing optimization for multiple simple property validations.
                new PropertyFromInfo { Destination = conceptInfo.Dependency_ChangesEntity, Source = activeSinceProperty },
                historyActiveSinceProperty,
                new UniqueMultiplePropertiesInfo { DataStructure = conceptInfo.Dependency_ChangesEntity, PropertyNames = $"{currentProperty.Name} {historyActiveSinceProperty.Name}" }
            });

            // InvalidData for history entity: it is not allowed to save with ActiveSince newer than current entity
            var denyFilterHistory = new ComposableFilterByInfo
            {
                Parameter = "Common.NewerThanCurrentEntry",
                Source = conceptInfo.Dependency_ChangesEntity,
                Expression = @"(items, repository, parameter) => items.Where(item => item.ActiveSince > item.Entity.ActiveSince)"
            };
            var invalidDataValidationHistory = new InvalidDataInfo
            {
                FilterType = "Common.NewerThanCurrentEntry",
                Source = conceptInfo.Dependency_ChangesEntity,
                ErrorMessage = "ActiveSince of history entry is not allowed to be newer than current entry."
            };
            newConcepts.AddRange(new IConceptInfo[]
                {
                    denyFilterHistory,
                    invalidDataValidationHistory,
                    new ParameterInfo { Module = new ModuleInfo { Name = "Common" }, Name = "NewerThanCurrentEntry" },
                    new InvalidDataMarkProperty2Info { InvalidData = invalidDataValidationHistory, MarkProperty = historyActiveSinceProperty }
                });

            // Create ActiveUntil SqlQueryable:
            var activeUntilSqlQueryable = new SqlQueryableInfo { Module = conceptInfo.Entity.Module, Name = conceptInfo.Entity.Name + "_ChangesActiveUntil", SqlSource = ActiveUntilSqlSnippet(conceptInfo) };
            newConcepts.AddRange(new IConceptInfo[] {
                activeUntilSqlQueryable,
                new DateTimePropertyInfo { DataStructure = activeUntilSqlQueryable, Name = "ActiveUntil" },
                new SqlDependsOnDataStructureInfo { Dependent = activeUntilSqlQueryable, DependsOn = conceptInfo.Dependency_ChangesEntity },
                new SqlDependsOnDataStructureInfo { Dependent = activeUntilSqlQueryable, DependsOn = conceptInfo.Entity },
                new DataStructureExtendsInfo { Base = conceptInfo.Dependency_ChangesEntity, Extension = activeUntilSqlQueryable }
            });

            // Configure History SqlQueryable:
            newConcepts.AddRange(new IConceptInfo[] {
                new SqlDependsOnDataStructureInfo { Dependent = historySqlQueryable, DependsOn = conceptInfo.Entity },
                new SqlDependsOnDataStructureInfo { Dependent = historySqlQueryable, DependsOn = conceptInfo.Dependency_ChangesEntity },
                new SqlDependsOnDataStructureInfo { Dependent = historySqlQueryable, DependsOn = activeUntilSqlQueryable },
                new DateTimePropertyInfo { DataStructure = historySqlQueryable, Name = "ActiveUntil" },
                new AllPropertiesFromInfo { Source = conceptInfo.Dependency_ChangesEntity, Destination = historySqlQueryable }
            });

            // Configure AtTime SqlFunction:
            newConcepts.Add(new SqlDependsOnDataStructureInfo { Dependent = atTimeSqlFunction, DependsOn = historySqlQueryable });

            return newConcepts;
        }

        private string ActiveUntilSqlSnippet(EntityHistoryInfo conceptInfo)
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
                    SqlUtility.Identifier(conceptInfo.Entity.Module.Name),
                    SqlUtility.Identifier(conceptInfo.Entity.Name),
                    SqlUtility.Identifier(conceptInfo.Dependency_ChangesEntity.Name));
        }

        private string HistorySqlSnippet(EntityHistoryInfo conceptInfo)
        {
            return string.Format(
                @"SELECT
                    ID = entity.ID,
                    EntityID = entity.ID,
                    ActiveUntil = CAST(NULL AS {6}){5}
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
                SqlUtility.Identifier(conceptInfo.Entity.Module.Name),
                SqlUtility.Identifier(conceptInfo.Entity.Name),
                SqlUtility.Identifier(conceptInfo.Dependency_ChangesEntity.Name),
                SqlUtility.Identifier(conceptInfo.Entity.Name + "_ChangesActiveUntil"),
                SelectHistoryPropertiesTag.Evaluate(conceptInfo),
                SelectEntityPropertiesTag.Evaluate(conceptInfo),
                _dateTimeSqlColumnType);
        }

        private string AtTimeSqlSnippet(EntityHistoryInfo conceptInfo)
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
                    SqlUtility.Identifier(conceptInfo.Entity.Module.Name),
                    SqlUtility.Identifier(conceptInfo.Entity.Name + "_History"),
                    SelectHistoryPropertiesTag.Evaluate(conceptInfo));
        }

        private string HistorySaveFunction(EntityHistoryInfo conceptInfo)
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
            
            Guid[] distinctEntityIDs = insertedNew.Union(updatedNew).Select(x => x.EntityID.Value).Distinct().ToArray();
            Guid[] existingEntities = _domRepository.{1}.{0}.Load(distinctEntityIDs).Select(x => x.ID).ToArray();
            Guid nonExistentEntityID = distinctEntityIDs.Except(existingEntities).FirstOrDefault();
            if (nonExistentEntityID != default(Guid))
                throw new Rhetos.UserException(""Insert or update of History is not allowed because there is no entity with EntityID: {{0}}"", new[] {{ nonExistentEntityID.ToString() }}, null, null);
            
            // INSERT
            insertedEnt.AddRange(insertedNew
                .Where(newItem => _domRepository.{1}.{0}.Load(new[]{{newItem.EntityID.Value}}).SingleOrDefault().ActiveSince < newItem.ActiveSince)
                .ToArray());
            insertedHist.AddRange(insertedNew
                .Where(newItem => _domRepository.{1}.{0}.Load(new[]{{newItem.EntityID.Value}}).SingleOrDefault().ActiveSince >= newItem.ActiveSince)
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
                .Where(item => _domRepository.{1}.{0}_Changes.Load(new[]{{item.ID}}).Any())
                .ToArray());
                
            var deletingHistIds = deletedHist.Select(hist => hist.ID).ToArray();
            deletedEnt.AddRange(deletedIds
                .Where(item => _domRepository.{1}.{0}.Load(new[]{{item.ID}}).Any())
                .Where(item => !(_domRepository.{1}.{0}_Changes.Query().Any(hist => hist.Entity.ID == item.ID && !deletingHistIds.Contains(hist.ID))))
                .ToArray());

            var histBackup = deletedIds
                .Where(item => _domRepository.{1}.{0}.Load(new[]{{item.ID}}).Any())
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
                insertedHist.Select(item =>
                    {{
                        var ret = new {0}_Changes();
                        ret.ID = item.ID;
                        ret.EntityID = item.EntityID;{2}
                        return ret;
                    }}),
                updateHist.Select(item =>
                    {{
                        var ret = _domRepository.{1}.{0}_Changes.Load(new [] {{ item.ID }}).Single();{2}
                        return ret;
                    }}),
                _domRepository.{1}.{0}_Changes.Load(deletedHist.Select(de => de.ID)));

            var updateCurrentAndAddHistory = insertedEnt.Select(item =>
                {{
                    var ret = _domRepository.{1}.{0}.Load(new [] {{ item.EntityID.Value }}).Single();{2}
                    return ret;
                }});
            var updateCurrentItemsOnly = new Common.DontTrackHistory<{1}.{0}>();
            updateCurrentItemsOnly.AddRange(updateEnt.Select(item =>
                {{
                    var ret = _domRepository.{1}.{0}.Load(new [] {{ item.EntityID.Value }}).Single();{2}
                    return ret;
                }}));
            _domRepository.{1}.{0}.Save(null, updateCurrentAndAddHistory, deletedEnt.Select(de => new {1}.{0} {{ ID = de.EntityID.Value }}));
            _domRepository.{1}.{0}.Save(null, updateCurrentItemsOnly, null);

            ", 
             conceptInfo.Entity.Name,
             conceptInfo.Entity.Module.Name,
             ClonePropertiesTag.Evaluate(conceptInfo));
        }
    }
}
