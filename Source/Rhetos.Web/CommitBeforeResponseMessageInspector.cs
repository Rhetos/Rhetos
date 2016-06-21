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

using Autofac;
using Autofac.Integration.Wcf;
using Rhetos.Persistence;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace Rhetos.Web
{
    /// <summary>
    /// This WCF extension forces the Rhetos web server to close SQL transaction (either commit or rollback) before sending the response to a client.
    /// IoC container would close the transaction when automatically disposing all components after the response,
    /// but that would be too late for detection of some SQL errors (specially those related to snapshot isolation).
    /// </summary>
    public class CommitBeforeResponseMessageInspector : IDispatchMessageInspector
    {
        public object AfterReceiveRequest(ref Message request, IClientChannel channel, InstanceContext instanceContext)
        {
            return null;
        }

        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var context = GetCurrentLifetimeContext();
            if (context != null)
                context.Resolve<IPersistenceTransaction>().Dispose();
        }

        private static AutofacInstanceContext GetCurrentLifetimeContext()
        {
            var wcfOperationContext = OperationContext.Current;
            if (wcfOperationContext != null)
            {
                var wcfInstanceContext = wcfOperationContext.InstanceContext;
                if (wcfInstanceContext != null)
                    return wcfInstanceContext.Extensions.Find<AutofacInstanceContext>();
            }
            return null;
        }
    }
}
