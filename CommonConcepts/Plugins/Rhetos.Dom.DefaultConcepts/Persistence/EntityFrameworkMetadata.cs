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
using Rhetos.Persistence;
using System.Data.Entity.Core.Metadata.Edm;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    public class EntityFrameworkMetadata
    {
        private readonly IMetadataWorkspaceFileProvider _metadataWorkspaceFileProvider;
        private readonly EfMappingViewCacheFactory _efMappingViewCacheFactory;

        private readonly Lazy<MetadataWorkspace> _initializedMetadataWorkspace;

        public MetadataWorkspace MetadataWorkspace => _initializedMetadataWorkspace.Value;

        public EntityFrameworkMetadata(IMetadataWorkspaceFileProvider metadataWorkspaceFileProvider, EfMappingViewCacheFactory efMappingViewCacheFactory)
        {
            _metadataWorkspaceFileProvider = metadataWorkspaceFileProvider;
            _efMappingViewCacheFactory = efMappingViewCacheFactory;
            _initializedMetadataWorkspace = new Lazy<MetadataWorkspace>(CreateAndInitializeMetadataWorkspace);
        }

        private MetadataWorkspace CreateAndInitializeMetadataWorkspace()
        {
            var metadataWorkspace = _metadataWorkspaceFileProvider.MetadataWorkspace;
            _efMappingViewCacheFactory.RegisterFactoryForWorkspace(metadataWorkspace);

            return metadataWorkspace;
        }
    }
}
