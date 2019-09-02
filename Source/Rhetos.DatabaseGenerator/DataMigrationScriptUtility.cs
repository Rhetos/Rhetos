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

using Rhetos.Dsl;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.DatabaseGenerator
{
    public static class DataMigrationScriptUtility
    {
        public static void GenerateCode(this IConceptDataMigration dataMigrationScript, IConceptInfo conceptInfo, IDataMigrationScriptBuilder dataMigrationScriptBuilder)
        {
            foreach (var method in GetPluginMethods(dataMigrationScript, conceptInfo))
            {
                method.InvokeEx(dataMigrationScript, new object[] { conceptInfo, dataMigrationScriptBuilder });
            }
        }

        private static List<MethodInfo> GetPluginMethods(IConceptDataMigration dataMigrationScript, IConceptInfo conceptInfo)
        {
            var methods = dataMigrationScript.GetType().GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IConceptDataMigration<>)
                        && i.GetGenericArguments().Single().IsAssignableFrom(conceptInfo.GetType()))
                    .Select(i => i.GetMethod("GenerateCode"))
                    .ToList();

            if (methods.Count == 0)
                throw new FrameworkException(string.Format(
                    "Plugin {0} does not implement generic interface {1} that accepts argument {2}.",
                    dataMigrationScript.GetType().FullName,
                    typeof(IConceptDataMigration<>).FullName,
                    conceptInfo.GetType().FullName));

            return methods;
        }
    }
}
