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
using System.ServiceModel;
using System.ServiceModel.Web;
using Rhetos;
using Rhetos.Utilities;
using Rhetos.Compiler;
using Rhetos.Dom;
using Rhetos.Dsl;
using Rhetos.Logging;
using Rhetos.Processing;
using Rhetos.Security;
using System.IO;

namespace Rhetos.RestGenerator
{
    internal class InitialCodeGenerator : IRestGeneratorPlugin
    {
        private const string CodeSnippet =
@"
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Net;
using Rhetos;
using Rhetos.Processing;
using Rhetos.Logging;
using Autofac;
" + RestGeneratorTags.Using + @"

namespace Rhetos
{
    public class DomainServiceModuleConfiguration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DomainService>().InstancePerDependency();
            base.Load(builder);
        }
    }

" + RestGeneratorTags.NamespaceMembers + @"

	[ServiceContract]
	public interface IDomainService
	{
" + RestGeneratorTags.InterfaceMembers + @"
	}
	
	public class DomainService : IDomainService
	{

        private readonly IServerApplication _serverApplication;

        public DomainService(
            IServerApplication serverApplication,
            Rhetos.Dom.IDomainObjectModel domainObjectModel,
            ILogProvider logProvider)
        {
            ILogger logger = logProvider.GetLogger(""RestService"");
            logger.Trace(""Service initialization."");

            _serverApplication = serverApplication;

            if (Rhetos.Utilities.XmlUtility.Dom == null)
                lock(Rhetos.Utilities.XmlUtility.DomLock)
                    if (Rhetos.Utilities.XmlUtility.Dom == null)
                    {
                        Rhetos.Utilities.XmlUtility.Dom = domainObjectModel.ObjectModel;
                        logger.Trace(""Domain object model initialized."");
                    }
        }

        private static ServerCommandInfo ToServerCommand(ICommandInfo commandInfo)
        {
            return new ServerCommandInfo
            {
                CommandName = commandInfo.GetType().Name,
                Data = Rhetos.Utilities.XmlUtility.SerializeToXml(commandInfo)
            };
        }

        private static void CheckForErrors(ServerProcessingResult result)
        {
            if (!result.Success)
                if (string.IsNullOrEmpty(result.UserMessage))
                    throw new WebFaultException<string>(result.SystemMessage, HttpStatusCode.InternalServerError);
                else
                    throw new WebFaultException<string>(result.UserMessage, HttpStatusCode.BadRequest);
        }

" + RestGeneratorTags.ImplementationMembers + @"
    
    }
}
";

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            codeBuilder.InsertCode(CodeSnippet);

            codeBuilder.AddReferencesFromDependency(typeof(IServerApplication));
            codeBuilder.AddReferencesFromDependency(typeof (ServiceContractAttribute));
            codeBuilder.AddReferencesFromDependency(typeof(ICommandInfo));
            codeBuilder.AddReferencesFromDependency(typeof(XmlUtility));
            codeBuilder.AddReferencesFromDependency(typeof (Guid));
            codeBuilder.AddReferencesFromDependency(typeof(WebFaultException));
            codeBuilder.AddReferencesFromDependency(typeof(System.Linq.Enumerable));
            codeBuilder.AddReferencesFromDependency(typeof(System.Net.HttpStatusCode));
            codeBuilder.AddReferencesFromDependency(typeof(IClaim));
            codeBuilder.AddReferencesFromDependency(typeof(Rhetos.Utilities.XmlUtility));
            codeBuilder.AddReferencesFromDependency(typeof(IDomainObjectModel));
            codeBuilder.AddReferencesFromDependency(typeof(ILogProvider));
          
            codeBuilder.AddReference(Path.Combine(_rootPath, "ServerDom.dll"));
            codeBuilder.AddReference(Path.Combine(_rootPath, "Autofac.dll"));
        }

        private static readonly string _rootPath = AppDomain.CurrentDomain.BaseDirectory;
    }
}