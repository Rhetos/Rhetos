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

using Rhetos.Extensibility;
using Rhetos.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Rhetos.Dom.DefaultConcepts.Persistence
{
    /// <summary>
    /// The generated EntityFrameworkContext will work with or without these metadata files,
    /// but context initialization is faster when loading metadata from the pregenerated files.
    /// </summary>
    [Export(typeof(IServerInitializer))]
    public class EntityFrameworkGenerateMetadataFiles : IServerInitializer
    {
        private readonly Lazy<DbContext> _dbContext;
        private readonly Lazy<EntityFrameworkMetadata> _metadata;

        public EntityFrameworkGenerateMetadataFiles(Lazy<DbContext> dbContext, Lazy<EntityFrameworkMetadata> metadata)
        {
            _dbContext = dbContext;
            _metadata = metadata;
        }

        public void Initialize()
        {
            _metadata.Value.SaveMetadata(_dbContext.Value);
        }

        public IEnumerable<string> Dependencies
        {
            get { return null; }
        }
    }
}
