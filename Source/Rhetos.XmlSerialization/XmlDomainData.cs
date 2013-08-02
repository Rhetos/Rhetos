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
using Rhetos.Utilities;
using System.Xml.Serialization;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;
using Rhetos.Processing;

namespace Rhetos.XmlSerialization
{
    [DataContract]
    public class XmlDomainData : IDomainData
    {
        private object Data;
        private bool refreshXmlRequired = true;

        private string xmlData;

        private Type ElementType;

        public XmlDomainData(Type elementType)
        {
            this.ElementType = elementType;
        }

        public XmlDomainData(Type elementType, object data)
        {
            this.ElementType = elementType;
            this.Data = data;
        }

        public T GetData<T>(Type type) where T : class
        {
            if (ElementType == null)
            {
                ElementType = type;
                Data = XmlUtility.DeserializeFromXml(ElementType, xmlData);
            }

            return Data as T;
        }

        public void SetData<T>(T data) where T : class
        {
            this.Data = data;
        }

        public static XmlDomainData Create<T>(T data)
        {
            return new XmlDomainData(typeof(T)) { Data = data };
        }

        [DataMember]
        public string Xml
        {
            get
            {
                if (refreshXmlRequired)
                {
                    xmlData = XmlUtility.SerializeToXml(Data, ElementType);
                    refreshXmlRequired = false;
                }
                return xmlData;
            }
            set
            {
                if (ElementType != null)
                    Data = XmlUtility.DeserializeFromXml(ElementType, value);
                xmlData = value;
            }
        }

        object ICommandData.Value
        {
            get { return Data; }
        }

    }
}
