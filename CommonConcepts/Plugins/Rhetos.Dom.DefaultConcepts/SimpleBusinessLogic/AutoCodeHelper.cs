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
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;

namespace Rhetos.Dom.DefaultConcepts
{
    public class AutoCodeItem<TEntity, TProperty>
        where TEntity : IEntity
    {
        public TEntity Item;
        public TProperty Code;
        public string GroupValue;
    }

    /// <summary>A helper for automatic detection of TEntity and TProperty parameters.</summary>
    public static class AutoCodeItem
    {
        public static AutoCodeItem<TEntity, TProperty> Create<TEntity, TProperty>(TEntity item, TProperty code, string groupValue = null)
            where TEntity : IEntity
        {
            return new AutoCodeItem<TEntity, TProperty>
            {
                Item = item,
                Code = code,
                GroupValue = groupValue
            };
        }
    }

    public static class AutoCodeHelper
    {
        public static void UpdateCodesWithoutCache<TEntity>(
            ISqlExecuter sqlExecuter,
            string entityName,
            string propertyName,
            IList<AutoCodeItem<TEntity, int?>> autoCodeItems,
            Action<TEntity, int?> setCode,
            string groupColumnName = null,
            bool groupTypeQuoted = false)
            where TEntity : IEntity
        {
            foreach (var autoCodeItem in autoCodeItems)
                if (autoCodeItem.Code == 0) // Both 0 and null are requests to generate autocode.
                    autoCodeItem.Code = null;

            var autoCodeGroups = autoCodeItems
                .GroupBy(acItem => acItem.GroupValue)
                .Select(g => new
                {
                    GroupValue = g.Key,
                    ItemsToGenerateCode = g.Where(acItem => acItem.Code == null).Select(acItem => acItem.Item).ToList(),
                    MaxProvidedCode = g.Max(acItem => acItem.Code)
                })
                .OrderBy(acGroup => acGroup.GroupValue)
                .ToList();

            // UpdateCodesWithoutCache does not need to update cache, so it is interested only in the items that require a generated code.
            autoCodeGroups = autoCodeGroups.Where(acg => acg.ItemsToGenerateCode.Count > 0).ToList();

            foreach (var autoCodeGroup in autoCodeGroups)
            {
                Lock(sqlExecuter, entityName, groupColumnName, autoCodeGroup.GroupValue);

                string groupFilter;
                if (groupColumnName == null)
                    groupFilter = "";
                else
                    groupFilter = "WHERE " + GetGroupFilter(groupColumnName, autoCodeGroup.GroupValue, groupTypeQuoted);

                string sql =
                    $@"SELECT ISNULL(MAX({propertyName}), 0)
                        FROM {entityName}
                        {groupFilter}";

                int maxNumber = 0;
                sqlExecuter.ExecuteReader(sql, reader =>
                {
                    maxNumber = reader.GetInt32(0);
                });

                // If there are newly inserted records greater then the existing records:
                if (autoCodeGroup.MaxProvidedCode != null && autoCodeGroup.MaxProvidedCode > maxNumber)
                {
                    maxNumber = autoCodeGroup.MaxProvidedCode.Value;
                }

                SetNewCodes(autoCodeGroup.ItemsToGenerateCode,
                    maxNumber + autoCodeGroup.ItemsToGenerateCode.Count, setCode);
            }
        }

        private static string GetGroupFilter(string groupColumnName, string groupValue, bool groupTypeQuoted)
        {
            if (groupValue != null)
                return groupColumnName + " = " + (groupTypeQuoted ? SqlUtility.QuoteText(groupValue) : groupValue);
            else
                return groupColumnName + " IS NULL";
        }

        public static void UpdateCodesWithoutCache<TEntity>(
            ISqlExecuter sqlExecuter,
            string entityName,
            string propertyName,
            IList<AutoCodeItem<TEntity, string>> autoCodeItems,
            Action<TEntity, string> setCode,
            string groupColumnName = null,
            bool groupTypeQuoted = false)
            where TEntity : IEntity
        {
            foreach (var autoCodeItem in autoCodeItems)
                if (autoCodeItem.Code == null)
                    autoCodeItem.Code = "+";

            var autoCodeGroups = OrganizeToAutoCodeGroups(autoCodeItems, entityName, propertyName);

            // UpdateCodesWithoutCache does not need to update cache, so it is interested only in the items that require a generated code.
            autoCodeGroups = autoCodeGroups.Where(acg => acg.ItemsToGenerateCode.Count > 0).ToList();

            foreach (var autoCodeGroup in autoCodeGroups)
            {
                Lock(sqlExecuter, entityName, groupColumnName, autoCodeGroup.GroupValue);

                string groupFilter;
                if (groupColumnName == null)
                    groupFilter = "";
                else
                    groupFilter = "AND " + GetGroupFilter(groupColumnName, autoCodeGroup.GroupValue, groupTypeQuoted);

                string quotedPrefix = SqlUtility.QuoteText(autoCodeGroup.Prefix);
                int prefixLength = autoCodeGroup.Prefix.Length;

                string sql =
                    $@"SELECT TOP 1
                        MaxSuffixNumber = CONVERT(INT, SUBSTRING({propertyName}, {prefixLength} + 1, 256 )),
                        MaxSuffixLength = LEN({propertyName}) - {prefixLength}
                    FROM
                        {entityName}
                    WHERE
                        {propertyName} LIKE {quotedPrefix} + '%'
                        AND ISNUMERIC(SUBSTRING({propertyName}, {prefixLength} + 1, 256)) = 1 
                        AND CHARINDEX('.', SUBSTRING({propertyName}, {prefixLength} + 1, 256)) = 0
                        AND CHARINDEX('e', SUBSTRING({propertyName}, {prefixLength} + 1, 256)) = 0
                        {groupFilter}
                    ORDER BY
                        -- Find maximal numeric suffix:
                        CONVERT(INT, SUBSTRING({propertyName}, {prefixLength} + 1, 256)) DESC,
                        -- If there are more than one suffixes with same value, take the longest code:
                        LEN({propertyName}) - {prefixLength} DESC";

                int maxSuffixNumber = 0; // Default, if there are no matching records.
                int maxSuffixLength = 1;
                sqlExecuter.ExecuteReader(sql, reader =>
                {
                    maxSuffixNumber = reader.GetInt32(0);
                    maxSuffixLength = reader.GetInt32(1);
                });

                // If there are newly inserted records greater then the existing records:
                if (autoCodeGroup.MaxProvidedCode != null && autoCodeGroup.MaxProvidedCode > maxSuffixNumber)
                {
                    maxSuffixNumber = autoCodeGroup.MaxProvidedCode.Value;
                    maxSuffixLength = autoCodeGroup.MinDigits;
                }

                SetNewCodes(autoCodeGroup, maxSuffixNumber + autoCodeGroup.ItemsToGenerateCode.Count,
                    Math.Max(maxSuffixLength, autoCodeGroup.MinDigits), setCode);
            }
        }

        public static void UpdateCodesWithCache<TEntity>(
            ISqlExecuter sqlExecuter,
            string entityName,
            string propertyName,
            IList<AutoCodeItem<TEntity, string>> autoCodeItems,
            Action<TEntity, string> setCode)
            where TEntity : IEntity
        {
            foreach (var autoCodeItem in autoCodeItems)
                if (autoCodeItem.Code == null)
                    autoCodeItem.Code = "+";

            var autoCodeGroups = OrganizeToAutoCodeGroups(autoCodeItems, entityName, propertyName);

            foreach (var autoCodeGroup in autoCodeGroups)
            {
                if (autoCodeGroup.MaxProvidedCode != null)
                {
                    string sql = string.Format("EXEC Common.AutoCodeCacheUpdate {0}, {1}, {2}, {3}, {4}, {5}",
                        SqlUtility.QuoteText(entityName),
                        SqlUtility.QuoteText(propertyName),
                        SqlUtility.QuoteText(autoCodeGroup.GroupValue),
                        SqlUtility.QuoteText(autoCodeGroup.Prefix),
                        autoCodeGroup.MinDigits,
                        autoCodeGroup.MaxProvidedCode);

                    sqlExecuter.ExecuteSql(sql);
                }

                if (autoCodeGroup.ItemsToGenerateCode.Count > 0)
                {
                    string sql = string.Format("EXEC Common.AutoCodeCacheGetNext {0}, {1}, {2}, {3}, {4}, {5}",
                        SqlUtility.QuoteText(entityName),
                        SqlUtility.QuoteText(propertyName),
                        SqlUtility.QuoteText(autoCodeGroup.GroupValue),
                        SqlUtility.QuoteText(autoCodeGroup.Prefix),
                        autoCodeGroup.MinDigits,
                        autoCodeGroup.ItemsToGenerateCode.Count);

                    int minDigits = 1;
                    int lastCode = autoCodeGroup.ItemsToGenerateCode.Count;
                    sqlExecuter.ExecuteReader(sql, reader =>
                    {
                        minDigits = reader.GetInt32(0);
                        lastCode = reader.GetInt32(1);
                    });

                    SetNewCodes(autoCodeGroup, lastCode, minDigits, setCode);
                }
            }
        }

        private static List<AutoCodeGroup<TEntity>> OrganizeToAutoCodeGroups<TEntity>(
            IList<AutoCodeItem<TEntity, string>> autoCodeItems,
            string entityName,
            string propertyName)
            where TEntity : IEntity
        {
            var parsedAutoCode = autoCodeItems
                .Where(acItem => !string.IsNullOrEmpty(acItem.Code))
                .Select(acItem =>
                {
                    int numberOfPluses = GetNumberOfPluses(acItem.Code, acItem.Item.ID, entityName, propertyName);
                    if (numberOfPluses > 0)
                        return new
                        {
                            acItem.Item,
                            acItem.GroupValue,
                            Prefix = acItem.Code.Substring(0, acItem.Code.Length - numberOfPluses),
                            MinDigits = numberOfPluses,
                            ProvidedCodeValue = (int?)null
                        };

                    int suffixDigitsCount = GetSuffixDigitsCount(acItem.Code);
                    if (suffixDigitsCount > 0)
                        return new
                        {
                            acItem.Item,
                            acItem.GroupValue,
                            Prefix = acItem.Code.Substring(0, acItem.Code.Length - suffixDigitsCount),
                            MinDigits = suffixDigitsCount,
                            ProvidedCodeValue = (int?)int.Parse(acItem.Code.Substring(acItem.Code.Length - suffixDigitsCount))
                        };

                    return null;
                })
                .Where(acItem => acItem != null)
                .ToList();

            return parsedAutoCode
                .GroupBy(acItem => new { acItem.GroupValue, acItem.Prefix })
                .Select(g => new AutoCodeGroup<TEntity>
                {
                    GroupValue = g.Key.GroupValue,
                    Prefix = g.Key.Prefix,
                    MinDigits = g.Max(acItem => acItem.MinDigits),
                    ItemsToGenerateCode = g.Where(acItem => acItem.ProvidedCodeValue == null).Select(acItem => acItem.Item).ToList(),
                    MaxProvidedCode = g.Max(acItem => acItem.ProvidedCodeValue)
                })
                .OrderBy(acGroup => acGroup.GroupValue)
                .ThenBy(acGroup => acGroup.Prefix)
                .ToList();
        }

        private class AutoCodeGroup<TEntity> where TEntity : IEntity
        {
            public string GroupValue;
            public string Prefix;
            public int MinDigits;
            public List<TEntity> ItemsToGenerateCode;
            public int? MaxProvidedCode;
        }

        private static void SetNewCodes<TEntity>(AutoCodeGroup<TEntity> autoCodeGroup, int lastCode, int minDigits, Action<TEntity, string> setCode) where TEntity : IEntity
        {
            for (int i = 0; i < autoCodeGroup.ItemsToGenerateCode.Count; i++)
            {
                string codeSuffix = (lastCode - autoCodeGroup.ItemsToGenerateCode.Count + i + 1).ToString();
                if (codeSuffix.Length < minDigits)
                    codeSuffix = new string('0', minDigits - codeSuffix.Length) + codeSuffix;

                setCode(autoCodeGroup.ItemsToGenerateCode[i], autoCodeGroup.Prefix + codeSuffix);
            }
        }

        private static void SetNewCodes<TEntity>(List<TEntity> itemsToGenerateCode, int lastCode, Action<TEntity, int?> setCode) where TEntity : IEntity
        {
            for (int i = 0; i < itemsToGenerateCode.Count; i++)
            {
                int code = lastCode - itemsToGenerateCode.Count + i + 1;
                setCode(itemsToGenerateCode[i], code);
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

        /// <summary>
        /// The manual database locking is used here in order to:
        /// 1. allow other users to read the existing records(no exclusive locks), and
        /// 2. avoid deadlocks(no shared locks that will be upgraded to exclusive locks).
        /// </summary>
        private static void Lock(ISqlExecuter sqlExecuter, string entityName, string groupColumnName, string groupValue)
        {
            string key = $"AutoCode {entityName}{groupColumnName ?? ""}{groupValue ?? ""}";
            key = key.Limit(200);

            try
            {
                sqlExecuter.ExecuteSql(
                    $@"DECLARE @lockResult int;
                    EXEC @lockResult = sp_getapplock {SqlUtility.QuoteText(key)}, 'Exclusive';
                    IF @lockResult < 0
                    BEGIN
                        RAISERROR('AutoCode lock.', 16, 10);
                        ROLLBACK;
                        RETURN;
                    END");
            }
            catch (FrameworkException ex)
            {
                if (ex.Message.TrimEnd().EndsWith("AutoCode lock."))
                    throw new UserException(
                        "Cannot insert the record in {0} because another user's insert command is still running.",
                        new object[] { entityName },
                        null,
                        ex);
                else
                    throw;
            }
        }
    }
}
