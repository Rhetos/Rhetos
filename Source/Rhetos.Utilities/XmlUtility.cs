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
using System.Reflection;
using System.Text;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;
using Rhetos.Dom;

namespace Rhetos.Utilities
{
    public class XmlUtility
    {
        /// <param name="domainObjectModel">
        /// Use of domainObjectModel.Assembly.GetType(string) is needed along with Type.GetType(string) to find objects in the generate domain object model.
        /// Since DOM assembly is not directly referenced from other dlls, Type.GetType will not find types in DOM
        /// before the Dom.GetType is used. The problem usually manifests on the first server call after restarting the process.
        /// </param>
        public XmlUtility(IDomainObjectModel domainObjectModel)
        {
            _resolver = new GenericDataContractResolver(domainObjectModel);
        }

        private GenericDataContractResolver _resolver;

        public string SerializeToXml<T>(T obj)
        {
            return SerializeToXml(obj, obj != null ? obj.GetType() : typeof(T));
        }

        public string SerializeToXml(object obj, Type type)
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
                {
                    Indent = true,
                    CheckCharacters = false,
                    NewLineHandling = NewLineHandling.Entitize
                };
            using (var xmlWriter = XmlWriter.Create(sb, settings))
            using (var xmlDict = XmlDictionaryWriter.CreateDictionaryWriter(xmlWriter))
            {
                var serializer = new DataContractSerializer(type);
                serializer.WriteObject(xmlDict, obj, _resolver);
                xmlWriter.Flush();
                return sb.ToString();
            }
        }

        public T DeserializeFromXml<T>(string xml)
        {
            return (T)DeserializeFromXml(typeof(T), xml);
        }

        public object DeserializeFromXml(Type type, string xml)
        {
            using (var sr = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(sr))
            using (var xmlDict = XmlDictionaryReader.CreateDictionaryReader(xmlReader))
            {
                var serializer = new DataContractSerializer(type);
                return serializer.ReadObject(xmlDict, false, _resolver);
            }
        }

        public string SerializeArrayToXml<T>(T[] data)
        {
            return SerializeToXml(data, typeof(T[]));
        }

        public string SerializeArrayToXml(object data, Type element)
        {
            return SerializeToXml(data, element.MakeArrayType());
        }

        public T[] DeserializeArrayFromXml<T>(string xml)
        {
            return (T[]) DeserializeFromXml(typeof(T[]), xml);
        }

        public object DeserializeArrayFromXml(string xml, Type element)
        {
            return DeserializeFromXml(element.MakeArrayType(), xml);
        }

        public string SerializeServerCallInfoToXml<T>(T obj, Guid serverCallID)
        {
            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(sb, new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                Encoding = Encoding.UTF8
            }))
            using (var xmlDict = XmlDictionaryWriter.CreateDictionaryWriter(xmlWriter))
            {
                var serializer = new DataContractSerializer(typeof(ServerCallInfo), new [] {typeof(T)});
                serializer.WriteObject(xmlDict, new ServerCallInfo { Entry = obj, ServerCallID = serverCallID });
                xmlWriter.Flush();
                return sb.ToString();
            }
        }
    }

    [DataContract]
    public class ServerCallInfo
    {
        [DataMember]
        public object Entry;

        [DataMember]
        public Guid ServerCallID;
    }
}
