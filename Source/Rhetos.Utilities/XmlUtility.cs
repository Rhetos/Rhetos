﻿/*
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
using System.Reflection;
using System.Text;
using System.IO;
using System.Xml;
using System.Runtime.Serialization;

namespace Rhetos.Utilities
{
    public static class XmlUtility
    {
        private static readonly GenericDataContractResolver Resolver = new GenericDataContractResolver();

        public static Assembly Dom; // TODO: Find a solution better than "public static"
        public static object DomLock = new object();

        public static string SerializeToXml<T>(T obj)
        {
            return SerializeToXml(obj, obj != null ? obj.GetType() : typeof(T));
        }

        public static string SerializeToXml(object obj, Type type)
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
                serializer.WriteObject(xmlDict, obj, Resolver);
                xmlWriter.Flush();
                return sb.ToString();
            }
        }

        public static T DeserializeFromXml<T>(string xml)
        {
            return (T)DeserializeFromXml(typeof(T), xml);
        }

        public static object DeserializeFromXml(Type type, string xml)
        {
            using (var sr = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(sr))
            using (var xmlDict = XmlDictionaryReader.CreateDictionaryReader(xmlReader))
            {
                var serializer = new DataContractSerializer(type);
                return serializer.ReadObject(xmlDict, false, Resolver);
            }
        }

        public static string SerializeArrayToXml<T>(T[] data)
        {
            return SerializeToXml(data, typeof(T[]));
        }

        public static string SerializeArrayToXml(object data, Type element)
        {
            return SerializeToXml(data, element.MakeArrayType());
        }

        public static T[] DeserializeArrayFromXml<T>(string xml)
        {
            return (T[]) DeserializeFromXml(typeof(T[]), xml);
        }

        public static object DeserializeArrayFromXml(string xml, Type element)
        {
            return DeserializeFromXml(element.MakeArrayType(), xml);
        }

        public static string SerializeLogElementToXml<T>(T obj, Guid serverCallID)
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
                var serializer = new DataContractSerializer(typeof(LogEntry), new [] {typeof(T)});
                serializer.WriteObject(xmlDict, new LogEntry { Entry = obj, ServerCallID = serverCallID });
                xmlWriter.Flush();
                return sb.ToString();
            }
        }
    }

    [DataContract]
    public class LogEntry
    {
        [DataMember]
        public object Entry;

        [DataMember]
        public Guid ServerCallID;
    }
}
