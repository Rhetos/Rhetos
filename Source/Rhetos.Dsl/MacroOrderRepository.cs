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

using Rhetos.Logging;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace Rhetos.Dsl
{
    public class MacroOrder
    {
        public string EvaluatorName;
        public decimal EvaluatorOrder;
    }

    /// <summary>
    /// Reads from file the recommended order of macro concepts evaluation.
    /// The order is optimized to reduce number of iteration in macro evaluation.
    /// </summary>
    public class MacroOrderRepository : IMacroOrderRepository
    {
        ILogger _loadOrderLogger;
        ILogger _saveOrderLogger;
        GeneratedFilesCache _generatedFilesCache;

        public MacroOrderRepository(ILogProvider logProvider, GeneratedFilesCache generatedFilesCache)
        {
            _generatedFilesCache = generatedFilesCache;
            _loadOrderLogger = logProvider.GetLogger("MacroRepositoryLoad");
            _saveOrderLogger = logProvider.GetLogger("MacroRepositorySave");
        }

        private const string MacroOrderFileName = "MacroOrder.json";

        /// <summary>
        /// Dictionary's Key is EvaluatorName, Value is EvaluatorOrder.
        /// </summary>
        public List<MacroOrder> Load()
        {
            var macroOrderJson = _generatedFilesCache.LoadFromCache(MacroOrderFileName);
            if (macroOrderJson != null)
            {
                string serializedConcepts = _generatedFilesCache.LoadFromCache(MacroOrderFileName);
                return JsonConvert.DeserializeObject<List<MacroOrder>>(serializedConcepts);
            }
            else {
                return GetDefaultOrder();
            }
        }

        private string ReportMacroOrders(IEnumerable<MacroOrder> macroOrders)
        {
            return string.Join("\r\n", macroOrders
                .OrderBy(macro => macro.EvaluatorOrder)
                .Select(macro => macro.EvaluatorName));
        }

        /// <param name="macroOrders">Tuple's Item1 is EvaluatorName, Item2 is EvaluatorOrder.</param>
        public void Save(IEnumerable<MacroOrder> macroOrders)
        {
            string serializedConcepts = JsonConvert.SerializeObject(macroOrders);
            string path = Path.Combine(Paths.GeneratedFolder, MacroOrderFileName);
            File.WriteAllText(path, serializedConcepts, Encoding.UTF8);
        }

        private List<MacroOrder> GetDefaultOrder()
        {
            return new List<MacroOrder>
            {
                new MacroOrder{ EvaluatorName = "IConceptMacro OmegaCommonConcepts.LookupVisibleMacro for OmegaCommonConcepts.LookupVisibleInfo", EvaluatorOrder = 0.0035714286m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.ChangesOnLinkedItemsMacro for Rhetos.Dsl.DefaultConcepts.ChangesOnLinkedItemsInfo", EvaluatorOrder = 0.0107142857m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.EntityLoggingMacro for Rhetos.Dsl.DefaultConcepts.EntityLoggingInfo", EvaluatorOrder = 0.0178571429m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.HierarchyWithPathMacro for Rhetos.Dsl.DefaultConcepts.HierarchyWithPathInfo", EvaluatorOrder = 0.0248226950m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.ImplementsInterfaceMacro for Rhetos.Dsl.DefaultConcepts.ImplementsInterfaceInfo", EvaluatorOrder = 0.0250000000m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.LegacyEntityWithAutoCreatedViewMacro for Rhetos.Dsl.DefaultConcepts.LegacyEntityWithAutoCreatedViewInfo", EvaluatorOrder = 0.0321428571m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.PessimisticLockingMacro for Rhetos.Dsl.DefaultConcepts.PessimisticLockingInfo", EvaluatorOrder = 0.0392857143m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.PolymorphicMacro for Rhetos.Dsl.DefaultConcepts.PolymorphicInfo", EvaluatorOrder = 0.0464285714m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.PrerequisiteAllPropertiesMacro for Rhetos.Dsl.DefaultConcepts.PrerequisiteAllProperties", EvaluatorOrder = 0.0535714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept OmegaCommonConcepts.ExtAction", EvaluatorOrder = 0.0607142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept OmegaCommonConcepts.TemplaterReportWithParamInfo", EvaluatorOrder = 0.0678571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.AutoCodeForEachInfo", EvaluatorOrder = 0.0750000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.AutoCodeSimpleInfo", EvaluatorOrder = 0.0821428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.DateRangeInfo", EvaluatorOrder = 0.0892857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.DateTimeRangeInfo", EvaluatorOrder = 0.0964285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.DeactivatableInfo", EvaluatorOrder = 0.1035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.EntityHistoryInfo", EvaluatorOrder = 0.1107142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.FilterByLinkedItemsInfo", EvaluatorOrder = 0.1178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.FilterByReferencedInfo", EvaluatorOrder = 0.1250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.IntegerRangeInfo", EvaluatorOrder = 0.1321428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.HierarchySingleRootInfo", EvaluatorOrder = 0.1382978723m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.ItemFilterApplyOnClientReadInfo", EvaluatorOrder = 0.1392857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.MaxLengthInfo", EvaluatorOrder = 0.1464285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.MinLengthInfo", EvaluatorOrder = 0.1535714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.PolymorphicMaterializedInfo", EvaluatorOrder = 0.1607142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.QueryableExtensionInfo", EvaluatorOrder = 0.1678571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.RangeInfo", EvaluatorOrder = 0.1750000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.RegExMatchDefaultMessageInfo", EvaluatorOrder = 0.1821428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.RegisteredInterfaceImplementationHelperInfo", EvaluatorOrder = 0.1892857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.ReportDataSourcesInfo", EvaluatorOrder = 0.1964285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.RowPermissionsInfo", EvaluatorOrder = 0.2035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.RowPermissionsReadInfo", EvaluatorOrder = 0.2107142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.RowPermissionsWriteInfo", EvaluatorOrder = 0.2178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.AggregationInfo", EvaluatorOrder = 0.2250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.ConfirmationIntervalPropertyInfo", EvaluatorOrder = 0.2321428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.DisposalActionPropertyInfo", EvaluatorOrder = 0.2392857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.FunctionDefinitionInfo", EvaluatorOrder = 0.2464285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.MetadataElementDefinitionInfo", EvaluatorOrder = 0.2535714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RetentionIntervalPropertyInfo", EvaluatorOrder = 0.2607142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RetentionOffsetMonthPropertyInfo", EvaluatorOrder = 0.2678571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RetentionOffsetPropertyInfo", EvaluatorOrder = 0.2750000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RetentionTriggerPropertyInfo", EvaluatorOrder = 0.2821428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept OmegaCommonConcepts.Emailnfo", EvaluatorOrder = 0.2892857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept OmegaCommonConcepts.ExtComposableFilterBy2Info", EvaluatorOrder = 0.2964285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept OmegaCommonConcepts.ExtDenySaveInfo", EvaluatorOrder = 0.3035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept OmegaCommonConcepts.JmbgInfo", EvaluatorOrder = 0.3107142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept OmegaCommonConcepts.Mod11_10", EvaluatorOrder = 0.3178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.GuiConcepts.AllGuiPropertiesInfo", EvaluatorOrder = 0.3250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.HrGovDSL.LimitAllowedRelationsTransitiveSimpleKeyReferenceInfo", EvaluatorOrder = 0.3321428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.HrGovDSL.RegistarInfo", EvaluatorOrder = 0.3392857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.HrGovDSL.SifrarnikInfo", EvaluatorOrder = 0.3464285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.UniqueProperties3Info", EvaluatorOrder = 0.3535714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.UniquePropertiesInfo", EvaluatorOrder = 0.3607142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.UniquePropertyInfo", EvaluatorOrder = 0.3678571429m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AllPropertiesLoggingMacro for Rhetos.Dsl.DefaultConcepts.AllPropertiesLoggingInfo", EvaluatorOrder = 0.3750000000m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.EntityHistoryAllPropertiesMacro for Rhetos.Dsl.DefaultConcepts.EntityHistoryAllPropertiesInfo", EvaluatorOrder = 0.3821428571m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.IsSubtypeOfMacro for Rhetos.Dsl.DefaultConcepts.IsSubtypeOfInfo", EvaluatorOrder = 0.3892857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.PersistedKeepSynchronizedWithFilteredSaveInfo", EvaluatorOrder = 0.3937500000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.EntityHistoryPropertyInfo", EvaluatorOrder = 0.3964285714m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.PropertyLoggingMacro for Rhetos.Dsl.DefaultConcepts.PropertyLoggingInfo", EvaluatorOrder = 0.4035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.SubtypeImplementationColumnInfo", EvaluatorOrder = 0.4107142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.HrGovDSL.AllowedRelationsTransitiveInfo", EvaluatorOrder = 0.4178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.HeldAggregationPropertyInfo", EvaluatorOrder = 0.4250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.HeldClassPropertyInfo", EvaluatorOrder = 0.4321428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.HeldRecordPropertyInfo", EvaluatorOrder = 0.4392857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.MetadataChangeEntryInfo", EvaluatorOrder = 0.4464285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.ServiceMetadataInfo", EvaluatorOrder = 0.4535714286m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.SqlDependsOnModuleMacro2 for Rhetos.Dsl.DefaultConcepts.SqlDependsOnModuleInfo", EvaluatorOrder = 0.4607142857m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AutoLegacyEntityDependsOnMacro for Rhetos.Dsl.DefaultConcepts.AutoLegacyEntityDependsOnInfo", EvaluatorOrder = 0.4678571429m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AutoSqlFunctionDependsOnMacro for Rhetos.Dsl.DefaultConcepts.AutoSqlFunctionDependsOnInfo", EvaluatorOrder = 0.4750000000m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AutoSqlProcedureDependsOnMacro for Rhetos.Dsl.DefaultConcepts.AutoSqlProcedureDependsOnInfo", EvaluatorOrder = 0.4821428571m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AutoSqlTriggerDependsOnMacro for Rhetos.Dsl.DefaultConcepts.AutoSqlTriggerDependsOnInfo", EvaluatorOrder = 0.4892857143m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.HierarchyMacro for Rhetos.Dsl.DefaultConcepts.HierarchyInfo", EvaluatorOrder = 0.4964285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.PersistedKeyPropertiesInfo", EvaluatorOrder = 0.5000000000m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.SqlDependsOnModuleMacro for Rhetos.Dsl.DefaultConcepts.SqlDependsOnModuleInfo", EvaluatorOrder = 0.5035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.AggregationWithDisposalHoldingInfo", EvaluatorOrder = 0.5107142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.ClosedPropertyInfo", EvaluatorOrder = 0.5178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.ConfirmationIntervalDurationPropertyInfo", EvaluatorOrder = 0.5250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.DeletedEventFunctionDefinitionInfo", EvaluatorOrder = 0.5321428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.DeletedMetadataElementDefinitionInfo", EvaluatorOrder = 0.5392857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.EntityTypes.ImplementsDisposalHoldingServiceInfo", EvaluatorOrder = 0.5464285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.HardcodedInfo", EvaluatorOrder = 0.5535714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.ImplementsAPIPluginModuleInfo", EvaluatorOrder = 0.5607142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.ImplementsDisposalSchedulingServiceInfo", EvaluatorOrder = 0.5678571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.ImplementsSystemServicesInfo", EvaluatorOrder = 0.5750000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.MinOccursPropertyInfo", EvaluatorOrder = 0.5821428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.OccurredPropertyInfo", EvaluatorOrder = 0.5892857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.PresentationOrderPropertyInfo", EvaluatorOrder = 0.5964285714m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AllPropertiesWithCascadeDeleteFromMacro for Rhetos.Dsl.DefaultConcepts.AllPropertiesWithCascadeDeleteFromInfo", EvaluatorOrder = 0.6035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RetentionIntervalDurationPropertyInfo", EvaluatorOrder = 0.6107142857m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.ModuleAutoSqlDependsOnMacro for Rhetos.Dsl.DefaultConcepts.ModuleAutoSqlDependsOnInfo", EvaluatorOrder = 0.6178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.PersistedDataStructureInfo", EvaluatorOrder = 0.6250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.PersistedKeepSynchronizedInfo", EvaluatorOrder = 0.6321428571m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AutoSqlQueryableDependsOnMacro for Rhetos.Dsl.DefaultConcepts.AutoSqlQueryableDependsOnInfo", EvaluatorOrder = 0.6392857143m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.FilterByBaseMacro for Rhetos.Dsl.DefaultConcepts.FilterByBaseInfo", EvaluatorOrder = 0.6464285714m },
                new MacroOrder{ EvaluatorName = "IConceptMacro OmegaCommonConcepts.SmartSearchMacro for OmegaCommonConcepts.SmartSearchInfo", EvaluatorOrder = 0.6535714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.BrowseDataStructureInfo", EvaluatorOrder = 0.6607142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept OmegaCommonConcepts.OibInfo", EvaluatorOrder = 0.6678571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.RegExMatchInfo", EvaluatorOrder = 0.6750000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.MaxValueInfo", EvaluatorOrder = 0.6821428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.MinValueInfo", EvaluatorOrder = 0.6892857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.ReferenceDetailInfo", EvaluatorOrder = 0.6964285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.SqlIndexInfo", EvaluatorOrder = 0.7035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.ClassInfo", EvaluatorOrder = 0.7107142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.PersistedAllPropertiesInfo", EvaluatorOrder = 0.7178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.AggregationAggregatedInInfo", EvaluatorOrder = 0.7250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RecordAggregatedInInfo", EvaluatorOrder = 0.7321428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RecordClassifiedByInfo", EvaluatorOrder = 0.7392857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.NewValuePropertyInfo", EvaluatorOrder = 0.7464285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.PreviousValuePropertyInfo", EvaluatorOrder = 0.7535714286m },
                new MacroOrder{ EvaluatorName = "IConceptMacro OmegaCommonConcepts.BrowseableMacro for OmegaCommonConcepts.BrowseableInfo", EvaluatorOrder = 0.7607142857m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.BrowseTakeNamedPropertyMacro for Rhetos.Dsl.DefaultConcepts.BrowseTakeNamedPropertyInfo", EvaluatorOrder = 0.7678571429m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AutoSqlViewDependsOnMacro for Rhetos.Dsl.DefaultConcepts.AutoSqlViewDependsOnInfo", EvaluatorOrder = 0.7750000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.AggregatedPropertyInfo", EvaluatorOrder = 0.7821428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.DisposalActionDuePropertyInfo", EvaluatorOrder = 0.7892857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.DisposalConfirmationDuePropertyInfo", EvaluatorOrder = 0.7964285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.DisposalOverdueAlertPropertyInfo", EvaluatorOrder = 0.8035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.LastReviewedCommentPropertyInfo", EvaluatorOrder = 0.8107142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.LastReviewedPropertyInfo", EvaluatorOrder = 0.8178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RecordInfo", EvaluatorOrder = 0.8250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.RetentionStartPropertyInfo", EvaluatorOrder = 0.8321428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.TransferredPropertyInfo", EvaluatorOrder = 0.8392857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.UniqueMultiplePropertiesInfo", EvaluatorOrder = 0.8464285714m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.EntityComputedFromAllPropertiesMacro for Rhetos.Dsl.DefaultConcepts.EntityComputedFromAllPropertiesInfo", EvaluatorOrder = 0.8535714286m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.AllPropertiesFromMacro for Rhetos.Dsl.DefaultConcepts.AllPropertiesFromInfo", EvaluatorOrder = 0.8607142857m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.PropertyFromMacro for Rhetos.Dsl.DefaultConcepts.PropertyFromInfo", EvaluatorOrder = 0.8678571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.SqlIndexMultipleInfo", EvaluatorOrder = 0.8750000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.AggregationClassifiedByInfo", EvaluatorOrder = 0.8821428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.AnyDisposalHoldPropertyInfo", EvaluatorOrder = 0.8892857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.CreatedPropertyInfo", EvaluatorOrder = 0.8964285714m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.DestroyedPropertyInfo", EvaluatorOrder = 0.9035714286m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.FirstUsedPropertyInfo", EvaluatorOrder = 0.9107142857m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.HierarchicalClassInfo", EvaluatorOrder = 0.9178571429m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.OriginatedPropertyInfo", EvaluatorOrder = 0.9250000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.TitlePropertyInfo", EvaluatorOrder = 0.9321428571m },
                new MacroOrder{ EvaluatorName = "IMacroConcept MoReq.Dsl.WithParentInfo", EvaluatorOrder = 0.9392857143m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.SystemRequiredInfo", EvaluatorOrder = 0.9464285714m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.ChangesOnBaseItemMacro for Rhetos.Dsl.DefaultConcepts.ChangesOnBaseItemInfo", EvaluatorOrder = 0.9535714286m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.EntityComputedFromMacro for Rhetos.Dsl.DefaultConcepts.EntityComputedFromInfo", EvaluatorOrder = 0.9607142857m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.KeepSynchronizedMacro for Rhetos.Dsl.DefaultConcepts.KeepSynchronizedInfo", EvaluatorOrder = 0.9678571429m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.ItemFilterMacro for Rhetos.Dsl.DefaultConcepts.ItemFilterInfo", EvaluatorOrder = 0.9750000000m },
                new MacroOrder{ EvaluatorName = "IMacroConcept Rhetos.Dsl.DefaultConcepts.ComposableFilterByInfo", EvaluatorOrder = 0.9821428571m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.SqlDependsOnDataStructureMacro for Rhetos.Dsl.DefaultConcepts.SqlDependsOnDataStructureInfo", EvaluatorOrder = 0.9892857143m },
                new MacroOrder{ EvaluatorName = "IConceptMacro Rhetos.Dsl.DefaultConcepts.SqlDependsOnPropertyMacro for Rhetos.Dsl.DefaultConcepts.SqlDependsOnPropertyInfo", EvaluatorOrder = 0.9964285714m }
            };
        }
    }
}
