/*
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
using System.Linq;
using System.Text;
using Rhetos.Dom.DefaultConcepts;
using Rhetos.Dsl.DefaultConcepts;
using System.Globalization;
using System.ComponentModel.Composition;
using Rhetos.Extensibility;
using Rhetos.Dsl;
using Rhetos.Compiler;

namespace Rhetos.Dom.DefaultConcepts
{
    [Export(typeof(IConceptCodeGenerator))]
    [ExportMetadata(MefProvider.Implements, typeof(EntityComputedFromInfo))]
    public class EntityComputedFromCodeGenerator : IConceptCodeGenerator
    {
        public static readonly CsTag<EntityComputedFromInfo> ComparePropertyTag = "CompareProperty";
        public static readonly CsTag<EntityComputedFromInfo> ClonePropertyTag = "CloneProperty";
        public static readonly CsTag<EntityComputedFromInfo> AssignPropertyTag = "AssignProperty";

        protected static string CodeSnippet(EntityComputedFromInfo info)
        {
            return string.Format(
@"        public void {4}()
        {{
            {4}<FilterAll>(null);
        }}

        public void {4}<T>(T filterLoad)
        {{
            {4}(filterLoad, x => x);
        }}

        public void {4}<T>(T filterLoad, Func<IEnumerable<{0}>, IEnumerable<{0}>> filterSave)
        {{
            var delete = new List<{0}>();
            var insert = new List<{0}>();
            var update = new List<{0}>();

            var sourceRepository = (IFilterRepository<T, {1}>)_domRepository.{1};
            var destRepository = (IFilterRepository<T, {0}>)_domRepository.{0};

            {1}[] sourceArray = sourceRepository.Filter(filterLoad);
            {0}[] destArray = destRepository.Filter(filterLoad);

            Array.Sort(sourceArray, (a, b)=>a.ID.CompareTo(b.ID));
            Array.Sort(destArray, (a, b) => a.ID.CompareTo(b.ID));

            IEnumerator<{1}> sourceEnum = sourceArray.AsEnumerable().GetEnumerator();
            IEnumerator<{0}> destEnum = destArray.AsEnumerable().GetEnumerator();

            try
            {{
                bool sourceExists = sourceEnum.MoveNext();
                bool destExists = destEnum.MoveNext();

                while (true)
                {{
                    int keyDiff;

                    if (sourceExists)
                        if (destExists)
                            keyDiff = sourceEnum.Current.ID.CompareTo(destEnum.Current.ID);
                        else
                            keyDiff = -1;
                    else
                        if (destExists)
                            keyDiff = 1;
                        else
                            break;

                    if (keyDiff == 0)
                    {{
                        bool same = true;
{2}

                        if (!same)
                        {{
                            _executionContext.NHibernateSession.Evict(destEnum.Current);
                            {5}
                            update.Add(destEnum.Current);
                        }}

                        sourceExists = sourceEnum.MoveNext();
                        destExists = destEnum.MoveNext();
                    }}
                    else if (keyDiff < 0)
                    {{
                        insert.Add(new {0}
                                {{
                                    ID = sourceEnum.Current.ID{3}
                                }});

                        sourceExists = sourceEnum.MoveNext();
                    }}
                    else
                    {{
                        delete.Add(destEnum.Current);
                        destExists = destEnum.MoveNext();
                    }}
                }}
            }}
            finally
            {{
                sourceEnum.Dispose();
                destEnum.Dispose();
            }}

            _domRepository.{0}.Save(filterSave(insert).ToArray(), filterSave(update).ToArray(), filterSave(delete).ToArray());
        }}

",
            info.Target.GetKeyProperties(),
            info.Source.GetKeyProperties(),
            ComparePropertyTag.Evaluate(info),
            ClonePropertyTag.Evaluate(info),
            EntityComputedFromInfo.RecomputeFunctionName(info),
            AssignPropertyTag.Evaluate(info));
        }

        public void GenerateCode(IConceptInfo conceptInfo, ICodeBuilder codeBuilder)
        {
            var info = (EntityComputedFromInfo)conceptInfo;
            codeBuilder.InsertCode(CodeSnippet(info), RepositoryHelper.RepositoryMembers, info.Target);
        }
    }
}
