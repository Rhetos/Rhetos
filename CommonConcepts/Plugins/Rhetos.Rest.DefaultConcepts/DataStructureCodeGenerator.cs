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
using System.ComponentModel.Composition;
using System.Globalization;
using System.Runtime.Serialization.Json;
using System.Xml;
using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Processing.DefaultCommands;
using Rhetos.RestGenerator;
using Rhetos.XmlSerialization;

namespace Rhetos.Rest.DefaultConcepts
{
    [Export(typeof(IRestGeneratorPlugin))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class DataStructureCodeGenerator : IRestGeneratorPlugin
    {
        public class DataStructureTag : Tag<DataStructureInfo>
        {
            public DataStructureTag(TagType tagType, string tagFormat, string nextTagFormat = null, string firstEvaluationContext = null, string nextEvaluationContext = null)
                : base(tagType, tagFormat, (info, format) => string.Format(CultureInfo.InvariantCulture, format, info.Module.Name, info.Name), nextTagFormat, firstEvaluationContext, nextEvaluationContext)
            { }
        }

        private static string DeclarationCodeSnippet(DataStructureInfo info)
        {
            return string.Format(@"        [OperationContract]
        [WebGet(UriTemplate = ""/{0}/{1}?filter={{filter}}&fparam={{fparam}}&genericfilter={{genericfilter}}&page={{page}}&psize={{psize}}&sort={{sort}}"", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        QueryResult<{0}.{1}> Get{0}{1}(string filter, string fparam, string genericfilter, int page, int psize, string sort);

        [OperationContract]
        [WebGet(UriTemplate = ""/{0}/{1}/{{id}}"", RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        {0}.{1} Get{0}{1}ById(string id);

",
                info.Module.Name, info.Name);
        }

        private static string ImplementationCodeSnippet(DataStructureInfo info)
        {
            return string.Format(@"        public QueryResult<{0}.{1}> Get{0}{1}(string filter, string fparam, string genericfilter, int page, int psize, string sort)
        {{
            FilterCriteria[] genericFilterInstance = null;
            if (!string.IsNullOrEmpty(genericfilter))
            {{
                genericFilterInstance = DeserializeJson<FilterCriteria[]>(genericfilter);
                if (genericFilterInstance == null)
                        throw new Rhetos.UserException(""Invalid format of the generic filter: '"" + genericfilter + ""'."");
            }}
            Type filterType = null;
            if (!string.IsNullOrEmpty(filter))
            {{
                filterType = Type.GetType(filter);

                if (filterType == null && Rhetos.Utilities.XmlUtility.Dom != null)
                    filterType = Rhetos.Utilities.XmlUtility.Dom.GetType(filter);

                if (filterType == null)
                    throw new Rhetos.UserException(""Filter type '"" + filter + ""' is not recognised."");
            }}
            object filterInstance = null;
            if (filterType != null)
            {{
                if (!string.IsNullOrEmpty(fparam))
                {{
                    filterInstance = DeserializeJson(filterType, fparam);
                    if (filterInstance == null)
                        throw new Rhetos.UserException(""Invalid filter parameter format for filter '"" + filter + ""', data: '"" + fparam + ""'."");
                }}
                else
                    filterInstance = Activator.CreateInstance(filterType);
            }}

            return GetData<{0}.{1}>(filterInstance, genericFilterInstance, page, psize, sort);
        }}

        public {0}.{1} Get{0}{1}ById(string id)
        {{
            var filter = new [] {{ Guid.Parse(id) }};

            var result = GetData<{0}.{1}>(filter).Records.FirstOrDefault();
            if (result == null)
                throw new WebFaultException<string>(""There is no resource of this type with a given ID."", HttpStatusCode.NotFound);

            return result;
        }}

",
                info.Module.Name, info.Name);
        }

        private static bool _isInitialCallMade;

        public static bool IsTypeSupported(DataStructureInfo conceptInfo)
        {
            return conceptInfo is EntityInfo
                || conceptInfo is BrowseDataStructureInfo
                || conceptInfo is LegacyEntityInfo
                || conceptInfo is LegacyEntityWithAutoCreatedViewInfo
                || conceptInfo is SqlQueryableInfo
                || conceptInfo is QueryableExtensionInfo;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureInfo info = (DataStructureInfo)conceptInfo;

            if (IsTypeSupported(info))
            {
                GenerateInitialCode(codeBuilder);

                codeBuilder.InsertCode(DeclarationCodeSnippet(info), RestGeneratorTags.InterfaceMembers);
                codeBuilder.InsertCode(ImplementationCodeSnippet(info), RestGeneratorTags.ImplementationMembers);
            }
        }

        private static void GenerateInitialCode(ICodeBuilder codeBuilder)
        {
            if (_isInitialCallMade)
                return;
            _isInitialCallMade = true;

            codeBuilder.InsertCode(@"

        private QueryResult<T> GetData<T>(object filter, FilterCriteria[] genericFilter=null, int page=0, int psize=0, string sort="""")
        {
            var commandInfo = new QueryDataSourceCommandInfo
                                  {
                                      DataSource = typeof (T).FullName,
                                      Filter = filter,
                                      GenericFilter = genericFilter,
                                      PageNumber = page,
                                      RecordsPerPage = psize
                                  };

            if (!String.IsNullOrWhiteSpace(sort))
            {
                var sortParameters = sort.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                commandInfo.OrderByProperty = sortParameters[0];
                commandInfo.OrderDescending = sortParameters.Count() >= 2 && sortParameters[1].ToLower().Equals(""desc"");
            }

            var result = _serverApplication.Execute(ToServerCommand(commandInfo));
            CheckForErrors(result);

            var resultData = Rhetos.Utilities.XmlUtility.DeserializeFromXml<QueryDataSourceCommandResult>(result.ServerCommandResults[0].Data);

            commandInfo.Filter = null;
            commandInfo.GenericFilter = null;

            return new QueryResult<T>
            {
                Records = resultData.Records.Select(o => (T)o).ToList(),
                TotalRecords = resultData.TotalRecords,
                CommandArguments = commandInfo
            };
        }

", RestGeneratorTags.ImplementationMembers);

            codeBuilder.InsertCode(@"
using Rhetos.Processing.DefaultCommands;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.XmlSerialization;
using System.Linq;
", RestGeneratorTags.Using);

            codeBuilder.InsertCode(@"
    public class QueryResult<T>
    {
        public List<T> Records { get; set; }
        public int TotalRecords { get; set; }
        public QueryDataSourceCommandInfo CommandArguments { get; set; }
    }

    public class InsertDataResult
    {
        public Guid ID;
    }

", RestGeneratorTags.NamespaceMembers);

            codeBuilder.AddReferencesFromDependency(typeof(QueryDataSourceCommandInfo));
            codeBuilder.AddReferencesFromDependency(typeof(XmlDomainData));

            codeBuilder.InsertCode(@"
        private static T DeserializeJson<T>(string json)
        {
            return (T) DeserializeJson(typeof(T), json);
        }

        private static object DeserializeJson(Type type, string json)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var serializer = new DataContractJsonSerializer(type);
                return serializer.ReadObject(stream);
            }
        }

", RestGeneratorTags.ImplementationMembers);

            codeBuilder.InsertCode(@"
using System.IO;
using System.Text;
using System.Runtime.Serialization.Json;
", RestGeneratorTags.Using);

            codeBuilder.AddReferencesFromDependency(typeof(XmlReader));
            codeBuilder.AddReferencesFromDependency(typeof(DataContractJsonSerializer));

        }
    }
}