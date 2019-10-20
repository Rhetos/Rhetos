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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public class ConfigurationProvider : IConfigurationProvider
    {
        public static readonly string ConfigurationPathSeparator = ":";
        private readonly Dictionary<string, object> _configurationValues;

        public ConfigurationProvider(Dictionary<string, object> configurationValues)
        {
            _configurationValues = configurationValues;
        }

        public T GetOptions<T>(string configurationPath = "", bool requireAllMembers = false) where T : class
        {
            var optionsType = typeof(T);
            var optionsInstance = Activator.CreateInstance(optionsType);

            var props = optionsType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(a => a.CanWrite);
            var fields = optionsType.GetFields(BindingFlags.Public | BindingFlags.Instance).Where(a => !a.IsInitOnly);
            var members = props.Cast<MemberInfo>().Concat(fields.Cast<MemberInfo>()).ToList();

            var membersBound = new List<MemberInfo>();
            foreach (var member in members)
            {
                var effectiveName = member.Name.Replace("__", ConfigurationPathSeparator); // allow binding to property designating a full path to configuration value
                if (TryGetConfigurationValue(effectiveName, out var memberValue, configurationPath))
                {
                    SetMemberValue(optionsInstance, member, memberValue);

                    if (requireAllMembers)
                        membersBound.Add(member);
                }
            }

            if (requireAllMembers && membersBound.Count != members.Count)
            {
                var missing = members.Where(a => !membersBound.Contains(a)).Select(a => a.Name);
                throw new FrameworkException($"Binding requires all members to be present in configuration, but some are missing: {string.Join(",", missing)}.");
            }

            return (T)optionsInstance;
        }

        public T GetValue<T>(string configurationKey, T defaultValue = default, string configurationPath = "")
        {
            if (!TryGetConfigurationValue(configurationKey, out var value, configurationPath))
                return defaultValue;

            return Convert<T>(value);
        }

        public string[] AllKeys => _configurationValues.Keys.ToArray();

        private void SetMemberValue(object instance, MemberInfo member, object value)
        {
            if (member is PropertyInfo propertyInfo)
                propertyInfo.SetValue(instance, Convert(propertyInfo.PropertyType, value));
            else if (member is FieldInfo fieldInfo)
                fieldInfo.SetValue(instance, Convert(fieldInfo.FieldType, value));
            else
                throw new FrameworkException($"Unhandled member type {member.GetType()}.");
        }

        private bool TryGetConfigurationValue(string configurationKey, out object result, string configurationPath = "")
        {
            configurationKey = configurationKey.ToLowerInvariant();
            if (!string.IsNullOrEmpty(configurationPath))
                configurationKey = $"{configurationPath.ToLowerInvariant()}{ConfigurationPathSeparator}{configurationKey}";

            return _configurationValues.TryGetValue(configurationKey, out result);
        }


        private T Convert<T>(object value)
        {
            return (T)Convert(typeof(T), value);
        }

        private object Convert(Type targetType, object value)
        {
            if (value == null) return null;
            if (targetType == value.GetType() || targetType == typeof(object)) return value;

            if (!(value is string))
                throw new FrameworkException($"Can't convert configuration value from type {value.GetType()} to {targetType}. Configuration values can only be fetched in their original type or parsed from string.");

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
                    return Enum.Parse(targetType, valueString);
            }
            catch (Exception e)
            {
                throw new FrameworkException($"Type conversion failed converting '{value}' to {targetType}.", e);
            }

            throw new FrameworkException($"Configuration type {targetType} is not supported.");
        }

        private string NormalizeDecimalSeparator(string value)
        {
            return value
                .Replace(".", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)
                .Replace(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
        }
    }
}
