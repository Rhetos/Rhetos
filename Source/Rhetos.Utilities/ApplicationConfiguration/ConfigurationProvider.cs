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

using Rhetos.Logging;
using Rhetos.Utilities;
using Rhetos.Utilities.ApplicationConfiguration;
using Rhetos.Utilities.ApplicationConfiguration.ConfigurationSources;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Rhetos
{
    public class ConfigurationProvider : IConfiguration
    {
        private readonly Dictionary<string, ConfigurationValue> _configurationValues;
        private readonly ConfigurationProviderOptions _configurationProviderOptions;
        private readonly ILogger _logger;

        public IEnumerable<string> AllKeys => _configurationValues.Keys;

        public ConfigurationProvider(IDictionary<string, ConfigurationValue> configurationValues, ILogProvider logProvider)
        {
            _configurationValues = configurationValues
                .ToDictionary(pair => pair.Key, pair => pair.Value, new ConfigurationKeyComparer());
            _configurationProviderOptions = GetOptions<ConfigurationProviderOptions>();
            _logger = logProvider.GetLogger(GetType().Name);

            if (_configurationProviderOptions.LegacyKeysSupport == LegacyKeysSupport.Error)
            {
                var legacyKeysReport = ReportLegacyKeys();
                foreach (var message in legacyKeysReport)
                    _logger.Error(message);
                if (legacyKeysReport.Any())
                    throw new FrameworkException(legacyKeysReport.First());
            }
            else if (_configurationProviderOptions.LegacyKeysWarning)
            {
                foreach (var message in ReportLegacyKeys())
                    _logger.Warning(message);
            }
        }

        private List<string> ReportLegacyKeys()
        {
            var newKeysByOld = ConfigurationProviderOptions.LegacyKeysMapping.ToMultiDictionary(mapping => mapping.Value, mapping => mapping.Key);
            return _configurationValues
                .Select(entry => new
                {
                    OldKey = entry.Key,
                    entry.Value.ConfigurationSource,
                    NewKeys = newKeysByOld.TryGetValue(entry.Key, out var value) ? value : null
                })
                .Where(entry => entry.NewKeys != null)
                .Select(entry => $"Please update the obsolete configuration key in {entry.ConfigurationSource}. Change '{entry.OldKey}' to '{string.Join(" or ", entry.NewKeys)}'.")
                .ToList();
        }

        public T GetOptions<T>(string configurationPath = "", bool requireAllMembers = false) where T : class
        {
            var optionsType = typeof(T);
            var optionsInstance = Activator.CreateInstance(optionsType);
            if (string.IsNullOrEmpty(configurationPath))
                configurationPath = OptionsAttribute.GetConfigurationPath<T>();

            var props = optionsType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanWrite);
            var fields = optionsType.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(field => !field.IsInitOnly);
            var members = props.Cast<MemberInfo>().Concat(fields.Cast<MemberInfo>()).ToList();

            var membersBound = new List<MemberInfo>();
            foreach (var member in members)
            {
                bool convertRelativePath = member.GetCustomAttribute<AbsolutePathOptionAttribute>() != null;
                if (TryGetConfigurationValueForMemberName(member.Name, out var memberValue, configurationPath, convertRelativePath))
                {
                    SetMemberValue(optionsInstance, member, memberValue);

                    if (requireAllMembers)
                        membersBound.Add(member);
                }
            }

            if (requireAllMembers && membersBound.Count != members.Count)
            {
                var missing = members.Where(member => !membersBound.Contains(member)).Select(member => member.Name);
                throw new FrameworkException($"Binding requires all members to be present in configuration, but some are missing: {string.Join(",", missing)}.");
            }

            return (T)optionsInstance;
        }

        private const string _memberMappingSeparator = "__";

        private bool TryGetConfigurationValueForMemberName(string memberName, out object value, string configurationPath, bool convertRelativePath)
        {
            value = null;
            var matchCount = 0;
            
            if (TryGetConfigurationValue(memberName, out var memberNameLiteral, configurationPath, convertRelativePath))
            {
                value = memberNameLiteral;
                matchCount++;
            }

            if (memberName.Contains(_memberMappingSeparator))
            {
                if (TryGetConfigurationValue(memberName.Replace(_memberMappingSeparator, ConfigurationProviderOptions.ConfigurationPathSeparator), out var memberNameColon, configurationPath, convertRelativePath))
                {
                    value = memberNameColon;
                    matchCount++;
                }
            }

            if (matchCount == 0)
                return false;

            if (matchCount > 1)
                throw new FrameworkException($"Found multiple matches while binding configuration value to member '{memberName}'.");

            return true;
        }


        public T GetValue<T>(string configurationKey, T defaultValue = default(T), string configurationPath = "")
        {
            if (!TryGetConfigurationValue(configurationKey, out var value, configurationPath, convertRelativePath: false))
                return defaultValue;

            return Convert<T>(value, configurationKey);
        }

        private void SetMemberValue(object instance, MemberInfo member, object value)
        {
            if (member is PropertyInfo propertyInfo)
                propertyInfo.SetValue(instance, Convert(propertyInfo.PropertyType, value, member.Name));
            else if (member is FieldInfo fieldInfo)
                fieldInfo.SetValue(instance, Convert(fieldInfo.FieldType, value, member.Name));
            else
                throw new FrameworkException($"Unhandled member type {member.GetType()}.");
        }

        private bool TryGetConfigurationValue(string configurationKey, out object result, string configurationPath, bool convertRelativePath)
        {
            if (!string.IsNullOrEmpty(configurationPath))
                configurationKey = $"{configurationPath}{ConfigurationProviderOptions.ConfigurationPathSeparator}{configurationKey}";

            if (_configurationValues.TryGetValue(configurationKey, out var entry))
            {
                result = GetConfigurationEntryValue(convertRelativePath, entry);
                return true;
            }
            else if (_configurationProviderOptions?.LegacyKeysSupport == LegacyKeysSupport.Convert
                && ConfigurationProviderOptions.LegacyKeysMapping.ContainsKey(configurationKey)
                && _configurationValues.TryGetValue(ConfigurationProviderOptions.LegacyKeysMapping[configurationKey], out var legacyKeyEntry))
            {
                result = GetConfigurationEntryValue(convertRelativePath, legacyKeyEntry);
                return true;
            }
            else if (_configurationValues.ContainsKey($"{configurationKey}:0"))
            {
                var entries = new List<string>();
                int index = 0;
                while (TryGetConfigurationValue(index.ToString(), out var arrayElement, configurationKey, convertRelativePath))
                {
                    entries.Add((string)arrayElement);
                    index++;
                }
                result = entries.ToArray();
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        private static object GetConfigurationEntryValue(bool convertRelativePath, ConfigurationValue entry)
        {
            if (convertRelativePath && entry.Value is string stringValue)
            {
                string baseFolder= (entry.ConfigurationSource as IConfigurationSourceFolder)?.SourceFolder
                    ?? AppDomain.CurrentDomain.BaseDirectory;
                return Path.GetFullPath(Path.Combine(baseFolder, stringValue));
            }
            else
                return entry.Value;
        }

        private T Convert<T>(object value, string forKeyDebugInfo)
        {
            return (T)Convert(typeof(T), value, forKeyDebugInfo);
        }

        private object Convert(Type targetType, object value, string forKeyDebugInfo)
        {
            if (value == null) return null;
            if (targetType.IsInstanceOfType(value)) return value;

            if (!(value is string))
                throw new FrameworkException($"Can't convert configuration value from type {value.GetType()} to {targetType}. Configuration values can only be fetched in their original type or parsed from string. Key '{forKeyDebugInfo}'.");

            try
            {
                string valueString = (string)value;

                if (targetType == typeof(int))
                    return int.Parse(valueString);
                else if (targetType == typeof(int?))
                    return (int?)int.Parse(valueString);
                else if (targetType == typeof(double))
                    return double.Parse(NormalizeDecimalSeparator(valueString));
                else if (targetType == typeof(double?))
                    return (double?)double.Parse(NormalizeDecimalSeparator(valueString));
                else if (targetType == typeof(bool))
                    return bool.Parse(valueString);
                else if (targetType == typeof(bool?))
                    return (bool?)bool.Parse(valueString);
                else if (targetType.IsEnum)
                    return ParseEnumVerbose(targetType, valueString, forKeyDebugInfo);
                else if (Nullable.GetUnderlyingType(targetType)?.IsEnum == true)
                    return ParseEnumVerbose(Nullable.GetUnderlyingType(targetType), valueString, forKeyDebugInfo);
            }
            catch (Exception e) when (!(e is FrameworkException))
            {
                throw new FrameworkException($"Type conversion failed for configuration key '{forKeyDebugInfo}' while converting value '{value}' to type '{targetType}'.", e);
            }

            throw new FrameworkException($"Configuration type {targetType} is not supported. Key '{forKeyDebugInfo}'.");
        }

        private object ParseEnumVerbose(Type enumType, string valueString, string forKeyDebugInfo)
        {
            try
            {
                return Enum.Parse(enumType, valueString);
            }
            catch (Exception e)
            {
                throw new FrameworkException(
                    $"Type conversion failed for configuration key '{forKeyDebugInfo}' while converting value '{valueString}' to type '{enumType}'. "
                    + $"Allowed values for {enumType.Name} are: {string.Join(", ", Enum.GetNames(enumType))}.",
                    e);
            }
        }

        private string NormalizeDecimalSeparator(string value)
        {
            return value
                .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        }

        /// <summary>
        /// Returns the expected configuration key with full path, for the given option.
        /// </summary>
        public static string GetKey<TOptions, TProperty>(Expression<Func<TOptions, TProperty>> propertySelector)
        {
            string path = OptionsAttribute.GetConfigurationPath<TOptions>();
            string propertyKey = ((MemberExpression)propertySelector.Body).Member.Name;
            if (string.IsNullOrEmpty(path))
                return propertyKey;
            else
                return path + ConfigurationProviderOptions.ConfigurationPathSeparator + propertyKey;
        }

        [Obsolete("Use GetValue or GetOptions instead")]
        public Lazy<string> GetString(string key, string defaultValue) => new Lazy<string>(() => GetValue(key, defaultValue));

        [Obsolete("Use GetValue or GetOptions instead")]
        public Lazy<int> GetInt(string key, int defaultValue) => new Lazy<int>(() => GetValue(key, defaultValue));

        [Obsolete("Use GetValue or GetOptions instead")]
        public Lazy<bool> GetBool(string key, bool defaultValue) => new Lazy<bool>(() => GetValue(key, defaultValue));

        [Obsolete("Use GetValue or GetOptions instead")]
        public Lazy<T> GetEnum<T>(string key, T defaultValue) where T : struct => new Lazy<T>(() => GetValue(key, defaultValue));
    }
}
