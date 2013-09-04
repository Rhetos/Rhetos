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
    [ExportMetadata(MefProvider.Implements, typeof(WriteInfo))]
    public class WriteCodeGenerator : IRestGeneratorPlugin
    {
        private const string DeclarationCodeSnippet = @"
        [OperationContract]
        [WebInvoke(Method = ""POST"", UriTemplate = ""/{0}/{1}"", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        InsertDataResult Insert{0}{1}({0}.{1} entity);

        [OperationContract]
        [WebInvoke(Method = ""PUT"", UriTemplate = ""/{0}/{1}/{{id}}"", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void Update{0}{1}(string id, {0}.{1} entity);

        [OperationContract]
        [WebInvoke(Method = ""DELETE"", UriTemplate = ""/{0}/{1}/{{id}}"", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        void Delete{0}{1}(string id);
";
        
        private const string ImplementationCodeSnippet = @"
        public InsertDataResult Insert{0}{1}({0}.{1} entity)
        {{
            if (Guid.Empty == entity.ID)
                entity.ID = Guid.NewGuid();

            var result = InsertData(entity);
            return new InsertDataResult {{ ID = entity.ID }};
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
            WriteInfo info = (WriteInfo)conceptInfo;

            codeBuilder.InsertCode(
                String.Format(DeclarationCodeSnippet, info.DataStructure.Module.Name, info.DataStructure.Name),
                RestGeneratorTags.InterfaceMembers);

            codeBuilder.InsertCode(
                String.Format(ImplementationCodeSnippet, info.DataStructure.Module.Name, info.DataStructure.Name),
                RestGeneratorTags.ImplementationMembers);
        }

    }
}