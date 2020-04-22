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
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace Rhetos.HomePage
{
    public class HomePageServiceHost : WebServiceHost
    {
        public HomePageServiceHost(Type serviceType, Uri[] baseAddresses)
            : base(serviceType, baseAddresses) { }

        protected override void OnOpening()
        {
            var wcfCreatesDefaultBindingsOnOpening = Description.Endpoints.Count == 0;
            // WebServiceHost will automatically create HTTP and HTTPS REST-like endpoints/binding/behaviors pairs, if service endpoint/binding/behavior configuration is empty 
            // After OnOpening setup, we will setup default binding sizes, if needed
            base.OnOpening();

            if (wcfCreatesDefaultBindingsOnOpening)
            {
                const int sizeInBytes = 200 * 1024 * 1024;
                foreach (var binding in Description.Endpoints.Select(x => x.Binding as WebHttpBinding))
                {
                    binding.MaxReceivedMessageSize = sizeInBytes;
                    binding.ReaderQuotas.MaxArrayLength = sizeInBytes;
                    binding.ReaderQuotas.MaxStringContentLength = sizeInBytes;
                    /*InitialCodeGenerator.ServiceHostOnOpeningDefaultBindingTag*/
                }
            }

            if (Description.Behaviors.Find<Rhetos.Web.JsonErrorServiceBehavior>() == null)
                Description.Behaviors.Add(new Rhetos.Web.JsonErrorServiceBehavior());
        }
    }
}