using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities
{
    public enum SqlVersion
    {
        /// <summary>
        ///     SQL Server 8 (2000).
        /// </summary>
        Sql8 = 80,

        /// <summary>
        ///     SQL Server 9 (2005).
        /// </summary>
        Sql9 = 90,

        /// <summary>
        ///     SQL Server 10 (2008).
        /// </summary>
        Sql10 = 100,

        /// <summary>
        ///     SQL Server 11 (2012).
        /// </summary>
        Sql11 = 110,

        // Higher versions go here
    }
}
