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

using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System;
using System.ComponentModel.Composition;
using System.IO;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DataStructureInfo))]
    public class DataStructureCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<DataStructureInfo> UsingTag = "Using";

        public static readonly CsTag<DataStructureInfo> AttributesTag = "ClassAttributes";

        public static readonly CsTag<DataStructureInfo> ModelTag = "Model";

        /// <summary>
        /// Add an interface to the simple POCO model class for the DataStructure.
        /// </summary>
        /// <remarks>
        /// Use codeBuilder.InsertCode to insert <b>FullName of the interface type</b> at this tag.
        /// The tag is configured to automatically add ':' and ',' to separate class name and interfaces.
        /// See also <see cref="DataStructureQueryableCodeGenerator.InterfaceTag"/>.
        /// </remarks>
        public static readonly CsTag<DataStructureInfo> InterfaceTag = new CsTag<DataStructureInfo>("ClassInterace", TagType.Appendable, " : {0}", ", {0}");

        public static readonly CsTag<DataStructureInfo> BodyTag = "ClassBody";

        private readonly CommonConceptsOptions _commonConceptsOptions;

        public DataStructureCodeGenerator(CommonConceptsOptions commonConceptsOptions)
        {
            _commonConceptsOptions = commonConceptsOptions;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            DataStructureInfo info = (DataStructureInfo)conceptInfo;

            string modelCodeSnippet =
$@"{DomInitializationCodeGenerator.DisableWarnings(_commonConceptsOptions)}{DomInitializationCodeGenerator.StandardNamespacesSnippet}

namespace {info.Module.Name}
{{
    {UsingTag.Evaluate(info)}

    {AttributesTag.Evaluate(info)}
    public class {info.Name}{InterfaceTag.Evaluate(info)}
    {{
        {BodyTag.Evaluate(info)}
    }}
}}

{ModelTag.Evaluate(info)}{DomInitializationCodeGenerator.RestoreWarnings(_commonConceptsOptions)}
";

            string modelFile = $"{Path.Combine(GeneratedSourceDirectories.Model.ToString(), info.Module.Name, info.Name + GeneratedSourceDirectories.Model)}";

            codeBuilder.InsertCodeToFile(modelCodeSnippet, modelFile);
        }

        /// <summary>
        /// Add an interface to the simple POCO model class for the DataStructure.
        /// </summary>
        public static void AddInterfaceAndReference(ICodeBuilder codeBuilder, Type type, DataStructureInfo dataStructureInfo)
        {
            AddInterfaceAndReference(codeBuilder, type.FullName, dataStructureInfo);
        }

        /// <summary>
        /// Add an interface to the simple POCO model class for the DataStructure.
        /// </summary>
        public static void AddInterfaceAndReference(ICodeBuilder codeBuilder, string typeName, DataStructureInfo dataStructureInfo)
        {
            codeBuilder.InsertCode(typeName, InterfaceTag, dataStructureInfo);
        }
    }
}
