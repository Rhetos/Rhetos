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
using System.Linq;
using System.Text;

namespace Rhetos.Dsl
{
    public class ConceptMetadata
    {
        Dictionary<string, Dictionary<Guid, object>> _metadata;

        public ConceptMetadata()
        {
            _metadata = new Dictionary<string, Dictionary<Guid, object>>();
        }

        public void Set<T>(IConceptInfo conceptInfo, ConceptMetadataType<T> metadataType, T value)
        {
            Dictionary<Guid, object> conceptMetadataByType;
            string conceptKey = conceptInfo.GetKey();
            if (!_metadata.TryGetValue(conceptKey, out conceptMetadataByType))
            {
                conceptMetadataByType = new Dictionary<Guid, object>();
                _metadata.Add(conceptKey, conceptMetadataByType);
            }

            if (conceptMetadataByType.ContainsKey(metadataType.Id))
                throw new FrameworkException(string.Format(
                    "The metadata is already set for concept {0}. Metadata type {1}.",
                    conceptInfo.GetUserDescription(),
                    metadataType));

            conceptMetadataByType.Add(metadataType.Id, value);
        }

        public T Get<T>(IConceptInfo conceptInfo, ConceptMetadataType<T> metadataType)
        {
            Func<string> missingMetadataMessage = () => "There is no metadata of requested type for concept " + conceptInfo.GetUserDescription() + ". Metadata type is " + metadataType + ".";
            var conceptMetadataByType = _metadata.GetValue(conceptInfo.GetKey(), missingMetadataMessage);
            var metadataValue = conceptMetadataByType.GetValue(metadataType.Id, missingMetadataMessage);
            return (T)metadataValue;
        }
    }

    public class ConceptMetadataType<T>
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }

        /// <param name="name">Name (optional) is used only for debugging.</param>
        public ConceptMetadataType(string name = null)
        {
            Id = Guid.NewGuid();
            Name = name;
        }

        public static implicit operator ConceptMetadataType<T>(string name)
        {
            return new ConceptMetadataType<T>(name);
        }

        public override string ToString()
        {
            return Name != null
                ? Name + ", " + Id.ToString()
                : Id.ToString();
        }
    }
}
