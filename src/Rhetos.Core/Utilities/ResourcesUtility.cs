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
        /// Loads the text entries from the resources file (.resx) embedded in the compiled assembly (dll).
        /// </summary>
        /// <param name="resxFilePath">Relative path of the .resx file within the project folder.</param>
        /// <param name="resourceAssembly">Assembly that contains the embedded .resx file.</param>
        /// <param name="addKeyPrefix">(Optional) The project name or a similar global identifier, that will be added to all resource keys to avoid conflicts between different plugins with same keys.</param>
        public static Dictionary<string, string> ReadEmbeddedResx(string resxFilePath, Assembly resourceAssembly, bool exceptionIfMissing, string addKeyPrefix = "")
        {
            const string expectedExtension = ".resx";
            if (Path.GetExtension(resxFilePath) != expectedExtension)
                throw new FrameworkException($"The resource file should have extension '{expectedExtension}'. File: '{resxFilePath}'.");
            string resxFilePathWithoutExtension = Path.Combine(Path.GetDirectoryName(resxFilePath), Path.GetFileNameWithoutExtension(resxFilePath));
            string resourceName = $"{resourceAssembly.GetName().Name}.{PathSeparatorToDot(resxFilePathWithoutExtension)}";
            var resourceManager = new ResourceManager(resourceName, resourceAssembly);
            ResourceSet resourceSet = resourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
            if (resourceSet == null)
            {
                if (exceptionIfMissing)
                    throw new FileNotFoundException($"Cannot find the embedded resource '{resourceName}' in the assembly '{resourceAssembly.FullName}'.");
                else
                    return null;
            }

            return resourceSet.Cast<DictionaryEntry>().ToDictionary(
                entry => addKeyPrefix + (string)entry.Key,
                entry => (string)entry.Value);
        }

        /// <summary>
        /// Reads the text from the file resource embedded in the compiled assembly (dll).
        /// </summary>
        /// <param name="filePath">Relative path of the text file within the project folder.</param>
        /// <param name="resourceAssembly">Assembly that contains the embedded text file.</param>
        public static string ReadEmbeddedTextFile(string filePath, Assembly resourceAssembly, bool exceptionIfMissing)
        {
            string resourceName = $"{resourceAssembly.GetName().Name}.{PathSeparatorToDot(filePath)}";
            using var resourceStream = resourceAssembly.GetManifestResourceStream(resourceName);
            if (resourceStream == null)
            {
                if (exceptionIfMissing)
                    throw new FileNotFoundException($"Cannot find the embedded resource '{resourceName}' in the assembly '{resourceAssembly.FullName}'.");
                else
                    return null;
            }

            using var reader = new StreamReader(resourceStream);
            return reader.ReadToEnd();
        }

        private static string PathSeparatorToDot(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, '.');
        }
    }
}
