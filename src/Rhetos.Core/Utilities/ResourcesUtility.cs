using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;

namespace Rhetos.Utilities
{
    public static class ResourcesUtility
    {
        /// <summary>
        /// Loads text entries from resources file (.resx) embedded in the compiled assembly (dll).
        /// </summary>
        /// <param name="sampleType">Specify which assembly and namespace is searched for the embedded .resx file, by providing the sample type from the same assembly and same namespace.</param>
        /// <param name="addKeyPrefix">(Optional) The project name or a similar global identifier, that will be added to all resource keys to avoid conflicts between different plugins with same keys.</param>
        public static Dictionary<string, string> ReadEmbeddedResx(string resxFileNameWithoutExtension, Type sampleType, bool exceptionIfMissing, string addKeyPrefix = "")
            => ReadEmbeddedResx(resxFileNameWithoutExtension, sampleType.Assembly, sampleType.Namespace, exceptionIfMissing, addKeyPrefix);

        /// <summary>
        /// Loads text entries from resources file (.resx) embedded in the compiled assembly (dll).
        /// </summary>
        /// <param name="resourceAssembly">Assembly that contains the embedded .resx file.</param>
        /// <param name="resourceNamespace">Namespace of the .resx file is usually the *subfolder path* inside the C# project where the .resx file is located.</param>
        /// <param name="addKeyPrefix">(Optional) The project name or a similar global identifier, that will be added to all resource keys to avoid conflicts between different plugins with same keys.</param>
        public static Dictionary<string, string> ReadEmbeddedResx(string resxFileNameWithoutExtension, Assembly resourceAssembly, string resourceNamespace, bool exceptionIfMissing, string addKeyPrefix = "")
        {
            string resxFullName = $"{resourceNamespace}.{resxFileNameWithoutExtension}";
            var resourceManager = new ResourceManager(resxFullName, resourceAssembly);
            ResourceSet resourceSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            if (resourceSet == null)
            {
                if (exceptionIfMissing)
                    throw new FileNotFoundException($"Cannot find the embedded resource '{resxFullName}' in the assembly '{resourceAssembly.FullName}'.");
                else
                    return null;
            }

            return resourceSet.Cast<DictionaryEntry>().ToDictionary(
                entry => addKeyPrefix + (string)entry.Key,
                entry => (string)entry.Value);
        }

        /// <summary>
        /// Reads text from the text file resource embedded in the compiled assembly (dll).
        /// </summary>
        /// <param name="sampleType">Specify which assembly and namespace is searched for the embedded text file, by providing the sample type from the same assembly and same namespace.</param>
        public static string ReadEmbeddedTextFile(string fileName, Type sampleType, bool exceptionIfMissing)
            => ReadEmbeddedTextFile(fileName, sampleType.Assembly, sampleType.Namespace, exceptionIfMissing);

        /// <summary>
        /// Reads text from the text file resource embedded in the compiled assembly (dll).
        /// </summary>
        /// <param name="resourceAssembly">Assembly that contains the embedded text file.</param>
        /// <param name="resourceNamespace">Namespace of the text file is usually the *subfolder path* inside the C# project where the text file is located.</param>
        public static string ReadEmbeddedTextFile(string fileName, Assembly resourceAssembly, string resourceNamespace, bool exceptionIfMissing)
        {
            string fileFullName = $"{resourceNamespace}.{fileName}";
            using var resourceStream = resourceAssembly.GetManifestResourceStream(fileFullName);
            if (resourceStream == null)
            {
                if (exceptionIfMissing)
                    throw new FileNotFoundException($"Cannot find the embedded resource '{fileFullName}' in the assembly '{resourceAssembly.FullName}'.");
                else
                    return null;
            }

            using var reader = new StreamReader(resourceStream);
            return reader.ReadToEnd();
        }
    }
}
