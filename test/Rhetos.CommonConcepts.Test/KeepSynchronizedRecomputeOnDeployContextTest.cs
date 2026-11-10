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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Dom.DefaultConcepts;

namespace Rhetos.CommonConcepts.Test
{
    [TestClass]
    public class KeepSynchronizedRecomputeOnDeployContextTest
    {
        private sealed class TestDslModel : IDslModel
        {
            private readonly DslModelIndexByType _index = new();

            public TestDslModel(IEnumerable<IConceptInfo> concepts)
            {
                Concepts = [.. concepts];

                foreach (IConceptInfo concept in Concepts)
                    _index.Add(concept);
            }

            public IEnumerable<IConceptInfo> Concepts { get; }

            public IConceptInfo FindByKey(string conceptKey) => throw new NotImplementedException();

            public T GetIndex<T>() where T : IDslModelIndex
            {
                if (typeof(T) == typeof(DslModelIndexByType))
                    return (T)(object)_index;

                throw new NotImplementedException($"Type '{typeof(T)}' is not supported in this mock.");
            }
        }

        private static string GenerateContext(params (string targetProperty, string sourceProperty)[] mappings)
        {
            ModuleInfo module = new() { Name = "Test" };

            DataStructureInfo source = new() { Module = module, Name = "Source" };
            EntityInfo        target = new() { Module = module, Name = "Target" };

            List<PropertyComputedFromInfo> computedProperties =
            [
                .. mappings.Select(m =>
                {
                    PropertyInfo targetProperty = new() { DataStructure = target, Name = m.targetProperty };
                    PropertyInfo sourceProperty = new() { DataStructure = source, Name = m.sourceProperty };
                    return new PropertyComputedFromInfo { Target = targetProperty, Source = sourceProperty };
                })
            ];

            KeepSynchronizedInfo keepSynchronized = new()
            {
                EntityComputedFrom = new EntityComputedFromInfo { Target = target, Source = source },
                FilterSaveExpression = ""
            };

            List<IConceptInfo> concepts =
            [
                module, source, target,
                .. computedProperties,
                keepSynchronized
            ];

            TestDslModel model = new(concepts);
            CodeBuilder codeBuilder = new("/*", "*/");
            codeBuilder.InsertCode(KeepSynchronizedRecomputeOnDeployInfrastructureCodeGenerator.AddKeepSynchronizedMetadataTag);

            KeepSynchronizedRecomputeOnDeployCodeGenerator generator = new(model);
            generator.GenerateCode(keepSynchronized, codeBuilder);

            string generated = codeBuilder.GenerateCode();
            Match match = Regex.Match(generated, @"Context = @""([^""]*)""");
            Assert.IsTrue(match.Success, "Could not find Context in generated metadata:\r\n" + generated);
            return match.Groups[1].Value;
        }

        [TestMethod]
        public void Context_includes_target_computed_properties()
        {
            string context = GenerateContext(("Name", "Column1"));
            Assert.Contains("TargetComputedProperties: Name=Column1", context);
        }

        [TestMethod]
        public void Adding_a_computed_property_changes_context_and_triggers_recompute()
        {
            string contextBefore = GenerateContext(("Name", "Column1"));

            string contextAfter = GenerateContext(
                ("Name", "Column1"), 
                ("Name2", "Column2"));

            Assert.AreNotEqual(contextBefore, contextAfter, "Adding a computed property to the target must change the KeepSynchronized context, so the data is recomputed on deployment (issue #79).");
            Assert.Contains("TargetComputedProperties: Name2=Column2, Name=Column1", contextAfter);
        }

        [TestMethod]
        public void Removing_a_computed_property_changes_context_and_triggers_recompute()
        {
            string contextBefore = GenerateContext(
                ("Name", "Column1"), 
                ("Name2", "Column2"));
            
            string contextAfter = GenerateContext(("Name", "Column1"));

            Assert.AreNotEqual(contextBefore, contextAfter);
        }

        [TestMethod]
        public void Unchanged_mapping_does_not_change_context()
        {
            string context1 = GenerateContext(("Name", "Column1"));
            string context2 = GenerateContext(("Name", "Column1"));

            Assert.AreEqual(context1, context2);
        }

        [TestMethod]
        public void Mapping_input_order_does_not_change_context()
        {
            string context1 = GenerateContext(("Name", "Column1"), ("Name2", "Column2"));
            string context2 = GenerateContext(("Name2", "Column2"), ("Name", "Column1"));

            Assert.AreEqual(context1, context2);
        }

        [TestMethod]
        public void Build_culture_does_not_change_context()
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
                string contextEn = GenerateContext(("Image", "Column1"), ("idea", "Column2"));

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                string contextTr = GenerateContext(("Image", "Column1"), ("idea", "Column2"));

                Assert.AreEqual(contextEn, contextTr, "Build culture must not change the recompute context for identical mappings.");
                Assert.Contains("TargetComputedProperties: Image=Column1, idea=Column2", contextEn);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }

        [TestMethod]
        public void No_computed_properties_produces_empty_computed_properties_context()
        {
            string context = GenerateContext();
            Assert.Contains("TargetComputedProperties: ", context);
        }
    }
}
