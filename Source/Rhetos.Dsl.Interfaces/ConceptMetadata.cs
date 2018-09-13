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

namespace Rhetos.Dsl
{
    public class ConceptMetadata
    {
        Dictionary<string, Dictionary<Guid, object>> _metadata;

        public ConceptMetadata()
        {
            _metadata = new Dictionary<string, Dictionary<Guid, object>>();
        }

        public void Set<T>(IConceptInfo conceptInfo, ConceptMetadataKey<T> metadataKey, T value)
        {
            Dictionary<Guid, object> conceptInfoMetadata;
            string conceptKey = conceptInfo.GetKey();
            if (!_metadata.TryGetValue(conceptKey, out conceptInfoMetadata))
            {
                conceptInfoMetadata = new Dictionary<Guid, object>();
                _metadata.Add(conceptKey, conceptInfoMetadata);
            }

            object oldValue;
            if (!conceptInfoMetadata.TryGetValue(metadataKey.Id, out oldValue))
                conceptInfoMetadata.Add(metadataKey.Id, value);
            else
            {
                if (!SameValue(value, oldValue))
                    throw new FrameworkException(
                        $"Different metadata value is already set for concept {conceptInfo.GetUserDescription()}, key {metadataKey}." +
                        $" Previous value '{oldValue}', new value '{value}'.");
            }
        }

        private static bool SameValue<T>(T value, object oldValue)
        {
            return oldValue == null ? (value == null) : oldValue.Equals(value);
        }

        public T Get<T>(IConceptInfo conceptInfo, ConceptMetadataKey<T> metadataKey)
        {
            Func<string> missingMetadataMessage = () => "There is no requested metadata for concept " + conceptInfo.GetUserDescription() + ". Metadata key is " + metadataKey + ".";
            var conceptInfoMetadata = _metadata.GetValue(conceptInfo.GetKey(), missingMetadataMessage);
            var metadataValue = conceptInfoMetadata.GetValue(metadataKey.Id, missingMetadataMessage);
            return (T)metadataValue;
        }

        public T GetOrDefault<T>(IConceptInfo conceptInfo, ConceptMetadataKey<T> metadataKey, T defaultValue)
        {
            return Contains(conceptInfo, metadataKey)
                ? Get(conceptInfo, metadataKey)
                : defaultValue;
        }

        public bool Contains<T>(IConceptInfo conceptInfo, ConceptMetadataKey<T> metadataKey)
        {
            Dictionary<Guid, object> conceptInfoMetadata;
            if (!_metadata.TryGetValue(conceptInfo.GetKey(), out conceptInfoMetadata))
                return false;
            return conceptInfoMetadata.ContainsKey(metadataKey.Id);
        }
    }
}
