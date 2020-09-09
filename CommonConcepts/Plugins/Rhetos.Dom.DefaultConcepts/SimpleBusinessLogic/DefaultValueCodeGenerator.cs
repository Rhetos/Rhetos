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
using Rhetos.DatabaseGenerator.DefaultConcepts;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using System.ComponentModel.Composition;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(DefaultValueInfo))]
    public class DefaultValueCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<DefaultValueInfo> DefaultValueOverrideTag = new CsTag<DefaultValueInfo>("DefaultValueOverride", TagType.Reverse);
        private readonly ConceptMetadata _conceptMetadata;

        public DefaultValueCodeGenerator(ConceptMetadata conceptMetadata)
        {
            _conceptMetadata = conceptMetadata;
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (DefaultValueInfo)conceptInfo;
            string propertyName = info.Property is ReferencePropertyInfo ? info.Property.Name + "ID" : info.Property.Name;

            var parsedExpression = new ParsedExpression(info.Expression, new[] { info.Property.DataStructure.FullName }, conceptInfo);

            string getDefaultValue;
            if (parsedExpression.ResultLiteral != null)
            {
                getDefaultValue = parsedExpression.ResultLiteral;
            }
            else
            {
                string csPropertyType = _conceptMetadata.GetCsPropertyType(info.Property);
                if (string.IsNullOrEmpty(csPropertyType))
                    throw new DslSyntaxException(conceptInfo, $"{info.Property.GetKeywordOrTypeName()} is not supported" +
                        $" for {conceptInfo.GetKeywordOrTypeName()}, because it does not provide concept metadata for C# property type.");

                string defaultValueMethod =
                    $@"private {csPropertyType} DefaultValue_{propertyName}{parsedExpression.MethodParametersAndBody}

        ";
                codeBuilder.InsertCode(defaultValueMethod, RepositoryHelper.RepositoryMembers, info.Property.DataStructure);

                getDefaultValue = $"DefaultValue_{propertyName}(item)";
            }


            string saveCode = $@"foreach (var item in insertedNew)
            {{
                {DefaultValueOverrideTag.Evaluate(info)}
                if (item.{propertyName} == null)
                    item.{propertyName} = {getDefaultValue};
            }}

            ";

            codeBuilder.InsertCode(saveCode, WritableOrmDataStructureCodeGenerator.InitializationTag, info.Property.DataStructure);
        }
    }
}
