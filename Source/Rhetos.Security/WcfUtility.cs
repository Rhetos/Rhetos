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
using System.Net;
using System.ServiceModel;
using System.ServiceModel.Channels;
using Rhetos.Utilities;
using Rhetos.Logging;
using System.Text.RegularExpressions;
using System.Security.Principal;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;

namespace Rhetos.Security
{
    public class WcfUtility
    {
        public static string InitClientWorkstation(ILogger logger)
        {
            RemoteEndpointMessageProperty endpointInfo;
            try
            {
                endpointInfo = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            }
            catch (Exception ex)
            {
                logger.Error("Cannot obtain client host address: " + ex);
                return "Cannot obtain client host address";
            }

            if (endpointInfo == null)
            {
                logger.Error("Cannot obtain client host address: RemoteEndpointMessageProperty is null.");
                return "Cannot obtain client host address";
            }

            logger.Trace(() => "RemoteEndpointMessageProperty: address " + endpointInfo.Address + ", port " + endpointInfo.Port);

            string name = GetNameFromAddress(logger, endpointInfo.Address, endpointInfo.Port);

            if (string.IsNullOrEmpty(name))
            {
                string ipv4 = IPv4FromIPv6.Match(endpointInfo.Address).Groups[1].Value;
                if (!string.IsNullOrEmpty(ipv4))
                {
                    logger.Trace(() => "Extracted IPv4 address: " + ipv4);
                    name = GetNameFromAddress(logger, ipv4);
                }
            }

            if (string.IsNullOrEmpty(name))
                name = endpointInfo.Address + " port " + endpointInfo.Port;

            logger.Trace(() => "Workstation: " + name + ".");
            return name;
        }

        private static readonly Regex IPv4FromIPv6 = new Regex(@":(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})$", RegexOptions.Compiled);

        private static string GetNameFromAddress(ILogger logger, string address, int? port = null)
        {
            try
            {
                var clientAddress = IPAddress.Parse(address);
                var dnsEntry = Dns.GetHostEntry(clientAddress);
                if (IPAddress.IsLoopback(clientAddress) || dnsEntry.AddressList.Contains(clientAddress))
                    return dnsEntry.HostName;

                // It seems that "nbtstat.exe -A <ipaddress>" can return up to date values while 'nslookup.exe <ipaddress>' and Dns.GetHostEntry(ipaddress) return old values.
                logger.Trace("Cannot obtain client host name. DSN lookup returned a DSN entry that does not match given IP address. Check if 'nslookup.exe " + clientAddress + "' returns up-to-date values.");
                if (port.HasValue)
                    return address + " port " + port;
                return address;
            }
            catch (Exception ex)
            {
                logger.Trace("Cannot obtain client host name for " + address + ". " + ex);
                return null;
            }
        }
    }
}