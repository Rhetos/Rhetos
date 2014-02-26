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
    public class XmlDataArray : IDataArray 
    {
        private object[] Data;
        private bool refreshXmlRequired = true;

        private string xmlData;

        private Type ElementType;

        public XmlDataArray(Type elementType)
        {
            this.ElementType = elementType;
        }

        public XmlDataArray(Type elementType, object[] data)
        {
            this.ElementType = elementType;
            this.Data = data;
        }

        public T[] GetData<T>(Type type) where T : class
        {
            if (ElementType == null)
            {
                ElementType = type;
                Data = XmlUtility.DeserializeArrayFromXml(xmlData, ElementType) as object[];
            }

            if (Data == null)
                return null;

            return (T[])Data;
        }

        public void SetData<T>(T[] data) where T : class
        {
            if(data ==null)
                this.Data = null;
            else 
                Data = data;
        }

        public static XmlDataArray Create<T>(IEnumerable<T> data) where T : class
        {
            return new XmlDataArray(typeof(T))
            {
                Data = (data == null || data.Count() == 0) ? null : data.ToArray()
            };
        }

        [DataMember]
        public string Xml
        {
            get
            {
                if (refreshXmlRequired)
                {
                    xmlData = XmlUtility.SerializeArrayToXml(Data, ElementType);
                    refreshXmlRequired = false;
                }
                return xmlData;
            }
            set
            {
                if (ElementType != null)
                    Data = XmlUtility.DeserializeArrayFromXml(value, ElementType) as object[];
                xmlData = value;
            }
        }

        object ICommandData.Value
        {
            get { return Data; }
        }

    }
}
