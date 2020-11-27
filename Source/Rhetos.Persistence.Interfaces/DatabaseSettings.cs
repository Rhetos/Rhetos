using System;
using System.Collections.Generic;
using System.Text;

namespace Rhetos.Persistence
{
    public class DatabaseSettings
    {
        public bool UseLegacyMsSqlDateTime { get; }
    
        public DatabaseSettings(bool useLegacyMsSqlDateTime)
        {
            UseLegacyMsSqlDateTime = useLegacyMsSqlDateTime;
        }
    }
}
