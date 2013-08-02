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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.ComponentModel.Composition;
using System.Xml.Linq;
using System.Diagnostics.Contracts;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using Rhetos.Processing;

namespace Rhetos.XmlSerialization
{
    [DataContract]
    [Export(typeof(ICommandData))]
    public class XmlData : ICommandData
    {
        private bool refreshRequired;
        private StringBuilder sb = new StringBuilder();

        protected const string EmptyData = @"<?xml version=""1.0"" encoding=""utf-16""?>";

        public static XmlData CreateEmpty()
        {
            Contract.Ensures(Contract.Result<XmlData>() != null);

            return new XmlData { Xml = EmptyData };
        }

        [DataMember]
        public virtual string Xml 
        {
            get { return sb.ToString(); }
            set
            {
                if (sb == null) // TODO: Review why the value is null during deserialization.
                    sb = new StringBuilder();

                sb.Clear();
                sb.Append(value);
                refreshRequired = true;
            }
        }

        public void AppendXml(string value)
        {
            sb.Append(value);
            refreshRequired = true;
        }

        private XElement LinqQuery;

        public XElement LinqElement
        {
            get
            {
                if(refreshRequired || LinqQuery == null)
                    LinqQuery = XElement.Parse(Xml);

                return LinqQuery;
            }
        }

        object ICommandData.Value
        {
            get { return Xml; }
        }

    }
}
