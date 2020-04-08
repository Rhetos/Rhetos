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

using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Rhetos
{
    public class ConfigurationProvider : IConfiguration
    {
        public static readonly string ConfigurationPathSeparator = ":";
        private readonly Dictionary<string, (object Value, string BaseFolder)> _configurationValues;

        public ConfigurationProvider(IDictionary<string, (object Value, string BaseFolder)> configurationValues)
        {
            _configurationValues = configurationValues
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.InvariantCultureIgnoreCase);
        }

        public T GetOptions<T>(string configurationPath = "", bool requireAllMembers = false) where T : class
        {
            var optionsType = typeof(T);
            var optionsInstance = Activator.CreateInstance(optionsType);
            var optionsAttribute = optionsType.GetCustomAttribute<OptionsAttribute>();
            if (string.IsNullOrEmpty(configurationPath))
                configurationPath = optionsAttribute?.ConfigurationPath ?? "";

            var props = optionsType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.CanWrite);
            var fields = optionsType.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(field => !field.IsInitOnly);
            var members = props.Cast<MemberInfo>().Concat(fields.Cast<MemberInfo>()).ToList();

            var membersBound = new List<MemberInfo>();
            foreach (var member in members)
            {
                bool convertRelativePath = member.GetCustomAttribute<OptionsPathAttribute>() != null;
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
        private const string _memberMappingSeparatorDot = ".";

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
                if (TryGetConfigurationValue(memberName.Replace(_memberMappingSeparator, ConfigurationPathSeparator), out var memberNameColon, configurationPath, convertRelativePath))
                {
                    value = memberNameColon;
                    matchCount++;
                }
                if (TryGetConfigurationValue(memberName.Replace(_memberMappingSeparator, _memberMappingSeparatorDot), out var memberNameDot, configurationPath, convertRelativePath))
                {
                    value = memberNameDot;
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

        public IEnumerable<string> AllKeys => _configurationValues.Keys;

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
                configurationKey = $"{configurationPath}{ConfigurationPathSeparator}{configurationKey}";

            if (_configurationValues.TryGetValue(configurationKey, out var entry))
            {
                if (convertRelativePath && !string.IsNullOrEmpty(entry.BaseFolder) && !string.IsNullOrEmpty(entry.Value as string))
                    result = Path.Combine(entry.BaseFolder, (string)entry.Value);
                else
                    result = entry.Value;
                return true;
            }
            else
            {
                result = null;
                return false;
            }
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
                var valueString = value as string;

                if (targetType == typeof(int))
                    return int.Parse(valueString);
                else if (targetType == typeof(double))
                    return double.Parse(NormalizeDecimalSeparator(valueString));
                else if (targetType == typeof(bool))
                    return bool.Parse(valueString);
                else if (targetType.IsEnum)
                    return ParseEnumVerbose(targetType, valueString, forKeyDebugInfo);
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
