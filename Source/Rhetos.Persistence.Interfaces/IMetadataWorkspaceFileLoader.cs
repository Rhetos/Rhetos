using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Text;

namespace Rhetos.Persistence
{
    public interface IMetadataWorkspaceFileLoader
    {
        MetadataWorkspace LoadFromFiles();
    }
}
