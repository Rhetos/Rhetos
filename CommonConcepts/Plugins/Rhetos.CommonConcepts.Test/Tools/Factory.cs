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

using Rhetos.CommonConcepts.Test.Mocks;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.CommonConcepts.Test.Tools
{
    public static class Factory
    {

        public static GenericFilterHelper CreateGenericFilterHelper(IDataStructureReadParameters readParameters, bool dynamicTypeResolution = true, List<string> log = null)
        {
            return new GenericFilterHelper(
                new DomainObjectModelMock(),
                readParameters,
                new CommonConceptsRuntimeOptions { DynamicTypeResolution = dynamicTypeResolution },
                log != null
                    ? new ConsoleLogProvider((eventType, eventName, message) => log.Add($"[{eventType}] {eventName}: {message()}"))
                    : new ConsoleLogProvider());
        }

        public static DataStructureReadParameters CreateDataStructureReadParameters(IRepository repository, Type type)
        {
            var readParameterTypesProperty = repository.GetType().GetField("ReadParameterTypes", BindingFlags.Public | BindingFlags.Static);
            var specificFilterTypes = readParameterTypesProperty == null ?
                Array.Empty<KeyValuePair<string, Type>>() :
                (KeyValuePair<string, Type>[])readParameterTypesProperty.GetValue(null);
            return new DataStructureReadParameters(new Dictionary<string, KeyValuePair<string, Type>[]> {
                { type.FullName, specificFilterTypes }
            });
        }
    }
}
