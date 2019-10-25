using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public class RhetosAppOptions
    {
        public string RootPath { get; set; }
        public bool BuiltinAdminOverride { get; set; } = false;
        public int SqlCommandTimeout { get;set; } = 30;
        public double AuthorizationCacheExpirationSeconds { get; set; } = 30;
        public bool AuthorizationAddUnregisteredPrincipals { get; set; } = false;
        public bool Security__LookupClientHostname { get; set; } = false;
        public string Security__AllClaimsForUsers { get; set; } = "";
        public bool EntityFramework__UseDatabaseNullSemantics { get; set; } = false;
    }
}
