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
using Rhetos.Compiler;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Processing.DefaultCommands;
using Rhetos.RestGenerator;

namespace Rhetos.Rest.DefaultConcepts
{
    [Export(typeof(IRestGeneratorPlugin))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class WritableOrmDataStructureCodeGenerator : IRestGeneratorPlugin
    {
        private const string DeclarationCodeSnippet = @"
        [OperationContract]
        [WebInvoke(Method = ""POST"", UriTemplate = ""/{0}/{1}"", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        string Insert{0}{1}({0}.{1} entity);

        [OperationContract]
        [WebInvoke(Method = ""PUT"", UriTemplate = ""/{0}/{1}/{{id}}"", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void Update{0}{1}(string id, {0}.{1} entity);

        [OperationContract]
        [WebInvoke(Method = ""DELETE"", UriTemplate = ""/{0}/{1}/{{id}}"", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void Delete{0}{1}(string id);
";

        private const string ImplementationCodeSnippet = @"
        public string Insert{0}{1}({0}.{1} entity)
        {{
            if (Guid.Empty == entity.ID)
                entity.ID = Guid.NewGuid();

            InsertData(entity);

            return entity.ID.ToString();
        }}

        public void Update{0}{1}(string id, {0}.{1} entity)
        {{
            Guid guid = Guid.Parse(id);
            if (Guid.Empty == entity.ID)
                entity.ID = guid;
            if (guid != entity.ID)
                throw new WebFaultException<string>(""Given entity ID is not equal to resource ID from URI."", HttpStatusCode.BadRequest);

            UpdateData(entity);
        }}

        public void Delete{0}{1}(string id)
        {{
            var entity = new {0}.{1} {{ ID = Guid.Parse(id) }};

            DeleteData(entity);
        }}

";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            GenerateInitialCode(codeBuilder);

            DataStructureInfo info = (DataStructureInfo) conceptInfo;

            if (info is IWritableOrmDataStructure)
            {
                codeBuilder.InsertCode(
                    String.Format(DeclarationCodeSnippet, info.Module.Name, info.Name),
                    RestGeneratorTags.InterfaceMembers);

                codeBuilder.InsertCode(
                    String.Format(ImplementationCodeSnippet, info.Module.Name, info.Name),
                    RestGeneratorTags.ImplementationMembers);
            }
        }

        private static bool _isInitialCallMade;

        private static void GenerateInitialCode(ICodeBuilder codeBuilder)
        {
            if (_isInitialCallMade)
                return;
            _isInitialCallMade = true;

            codeBuilder.InsertCode(@"
        private void InsertData<T>(T entity)
        {
            var commandInfo = new SaveEntityCommandInfo
                                  {
                                      Entity = typeof(T).FullName,
                                      DataToInsert = new[] { (IEntity)entity }
                                  };

            var result = _serverApplication.Execute(ToServerCommand(commandInfo));
            CheckForErrors(result);
        }

        private void UpdateData<T>(T entity)
        {
            var commandInfo = new SaveEntityCommandInfo
            {
                Entity = typeof(T).FullName,
                DataToUpdate = new[] { (IEntity)entity }
            };

            var result = _serverApplication.Execute(ToServerCommand(commandInfo));
            CheckForErrors(result);
        }

        private void DeleteData<T>(T entity)
        {
            var commandInfo = new SaveEntityCommandInfo
            {
                Entity = typeof(T).FullName,
                DataToDelete = new[] { (IEntity)entity }
            };

            var result = _serverApplication.Execute(ToServerCommand(commandInfo));
            CheckForErrors(result);
        }
", RestGeneratorTags.ImplementationMembers);

            codeBuilder.InsertCode(@"
using Rhetos.Processing.DefaultCommands;
using Rhetos.Dom.DefaultConcepts;
", RestGeneratorTags.Using);

            codeBuilder.AddReferencesFromDependency(typeof(QueryDataSourceCommandInfo));
            codeBuilder.AddReferencesFromDependency(typeof(IEntity));
        }
    }
}