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

using NHibernate;
using Rhetos.Compiler;
using Rhetos.Dsl;
using Rhetos.Dsl.DefaultConcepts;
using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    public class AutoCodeItem<T>
        where T : IEntity
    {
        public T Item;
        public string Code;
        public string Grouping;
    }

    public class AutoCodeHelper
    {
        public static void UpdateCodes<T>(ISession nHibernateSession, string entityName, string propertyName, IEnumerable<AutoCodeItem<T>> autoCodeItems, Action<T, string> setCode)
            where T : IEntity
        {
            CsUtility.Materialize(ref autoCodeItems);

            var parsedAutoCode = autoCodeItems
                .Where(acItem => !string.IsNullOrEmpty(acItem.Code))
                .Select(acItem =>
                {
                    int numberOfPluses = GetNumberOfPluses(acItem.Code, acItem.Item.ID, entityName, propertyName);
                    if (numberOfPluses > 0)
                        return new
                        {
                            acItem.Item,
                            acItem.Grouping,
                            Prefix = acItem.Code.Substring(0, acItem.Code.Length - numberOfPluses),
                            MinDigits = numberOfPluses,
                            ProvidedCodeValue = (int?)null
                        };

                    int suffixDigitsCount = GetSuffixDigitsCount(acItem.Code);
                    if (suffixDigitsCount > 0)
                        return new
                        {
                            acItem.Item,
                            acItem.Grouping,
                            Prefix = acItem.Code.Substring(0, acItem.Code.Length - suffixDigitsCount),
                            MinDigits = suffixDigitsCount,
                            ProvidedCodeValue = (int?)int.Parse(acItem.Code.Substring(acItem.Code.Length - suffixDigitsCount))
                        };

                    return null;
                })
                .Where(acItem => acItem != null)
                .ToList();

            var autoCodeGroups = parsedAutoCode
                .GroupBy(acItem => new { acItem.Grouping, acItem.Prefix })
                .Select(g => new
                {
                    g.Key.Grouping,
                    g.Key.Prefix,
                    MinDigits = g.Max(acItem => acItem.MinDigits),
                    ItemsToGenerateCode = g.Where(acItem => acItem.ProvidedCodeValue == null).Select(acItem => acItem.Item).ToList(),
                    MaxProvidedCode = g.Max(acItem => acItem.ProvidedCodeValue)
                })
                .OrderBy(acGroup => acGroup.Grouping)
                .ThenBy(acGroup => acGroup.Prefix)
                .ToList();

            foreach (var autoCodeGroup in autoCodeGroups)
            {
                if (autoCodeGroup.MaxProvidedCode != null)
                {
                    string sql = string.Format("EXEC Common.UpdateAutoCodeCache {0}, {1}, {2}, {3}, {4}, {5}",
                        Rhetos.Utilities.SqlUtility.QuoteText(entityName),
                        Rhetos.Utilities.SqlUtility.QuoteText(propertyName),
                        Rhetos.Utilities.SqlUtility.QuoteText(autoCodeGroup.Grouping),
                        Rhetos.Utilities.SqlUtility.QuoteText(autoCodeGroup.Prefix),
                        autoCodeGroup.MinDigits,
                        autoCodeGroup.MaxProvidedCode);

                    nHibernateSession.CreateSQLQuery(sql).ExecuteUpdate();
                }

                if (autoCodeGroup.ItemsToGenerateCode.Count > 0)
                {
                    string sql = string.Format("EXEC Common.GetNextAutoCodeCached {0}, {1}, {2}, {3}, {4}, {5}",
                        Rhetos.Utilities.SqlUtility.QuoteText(entityName),
                        Rhetos.Utilities.SqlUtility.QuoteText(propertyName),
                        Rhetos.Utilities.SqlUtility.QuoteText(autoCodeGroup.Grouping),
                        Rhetos.Utilities.SqlUtility.QuoteText(autoCodeGroup.Prefix),
                        autoCodeGroup.MinDigits,
                        autoCodeGroup.ItemsToGenerateCode.Count);

                    object[] generatedCodeInfo = nHibernateSession.CreateSQLQuery(sql).List<object[]>().Single();
                    int minDigits = (int)generatedCodeInfo[0];
                    int lastCode = (int)generatedCodeInfo[1];

                    for (int i = 0; i < autoCodeGroup.ItemsToGenerateCode.Count; i++)
                    {
                        string codeSuffix = (lastCode - autoCodeGroup.ItemsToGenerateCode.Count + i + 1).ToString();
                        if (codeSuffix.Length < minDigits)
                            codeSuffix = new string('0', minDigits - codeSuffix.Length) + codeSuffix;

                        setCode(autoCodeGroup.ItemsToGenerateCode[i], autoCodeGroup.Prefix + codeSuffix);
                    }
                }
            }
        }

        public static int GetNumberOfPluses(string codeFormat, Guid itemId, string entityName, string propertyName)
        {
            int numberOfPluses = 0;

            foreach (char c in codeFormat)
                if (c == '+')
                    numberOfPluses++;
                else
                    if (numberOfPluses > 0)
                        throw new Rhetos.UserException(
                            "Invalid code format is entered: The value must end with one or more \"+\" characters at the end of the code (AutoCode). No \"+\" are allowed before those at the end.",
                            "DataStructure:" + entityName + ",Property:" + propertyName + ",ID:" + itemId);

            return numberOfPluses;
        }

        private static int GetSuffixDigitsCount(string providedCode)
        {
            int suffixDigitsCount = 0;

            foreach (char c in providedCode)
                if (char.IsDigit(c))
                    suffixDigitsCount++;
                else
                    suffixDigitsCount = 0;

            return suffixDigitsCount;
        }
    }
}
