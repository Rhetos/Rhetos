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
using Rhetos.Dom;

namespace Rhetos.XmlSerialization
{
    [DataContract]
    public class XmlDomainData : IDomainData
    {
        private object _data;
        private bool refreshXmlRequired = true;
        private string xmlData;
        private Type _elementType;
        private readonly XmlUtility _xmlUtility;

        public XmlDomainData(IDomainObjectModel domainObjectModel, Type elementType)
            : this(domainObjectModel, elementType, null)
        {
        }

        public XmlDomainData(IDomainObjectModel domainObjectModel, Type elementType, object data)
        {
            _xmlUtility = new XmlUtility(domainObjectModel);
            _elementType = elementType;
            _data = data;
        }

        public T GetData<T>(Type type) where T : class
        {
            if (_elementType == null)
            {
                _elementType = type;
                _data = _xmlUtility.DeserializeFromXml(_elementType, xmlData);
            }

            return _data as T;
        }

        public void SetData<T>(T data) where T : class
        {
            this._data = data;
        }

        public static XmlDomainData Create<T>(IDomainObjectModel domainObjectModel, T data)
        {
            return new XmlDomainData(domainObjectModel, typeof(T)) { _data = data };
        }

        [DataMember]
        public string Xml
        {
            get
            {
                if (refreshXmlRequired)
                {
                    xmlData = _xmlUtility.SerializeToXml(_data, _elementType);
                    refreshXmlRequired = false;
                }
                return xmlData;
            }
            set
            {
                if (_elementType != null)
                    _data = _xmlUtility.DeserializeFromXml(_elementType, value);
                xmlData = value;
            }
        }

#pragma warning disable CA1033 // Interface methods should be callable by child types
        object ICommandData.Value => _data;
#pragma warning restore CA1033 // Interface methods should be callable by child types
    }
}
