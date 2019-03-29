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
        public static void GenerateCode(this IDataMigrationScript dataMigrationScript, IConceptInfo conceptInfo, IDataMigrationScriptBuilder dataMigrationScriptBuilder)
        {
            foreach (var method in GetPluginMethods(dataMigrationScript, conceptInfo))
            {
                method.InvokeEx(dataMigrationScript, new object[] { conceptInfo, dataMigrationScriptBuilder });
            }
        }

        private static List<MethodInfo> GetPluginMethods(IDataMigrationScript dataMigrationScript, IConceptInfo conceptInfo)
        {
            var methods = dataMigrationScript.GetType().GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDataMigrationScript<>)
                        && i.GetGenericArguments().Single().IsAssignableFrom(conceptInfo.GetType()))
                    .Select(i => i.GetMethod("GenerateCode"))
                    .ToList();

            if (methods.Count == 0)
                throw new FrameworkException(string.Format(
                    "Plugin {0} does not implement generic interface {1} that accepts argument {2}.",
                    dataMigrationScript.GetType().FullName,
                    typeof(IDataMigrationScript<>).FullName,
                    conceptInfo.GetType().FullName));

            return methods;
        }
    }
}
