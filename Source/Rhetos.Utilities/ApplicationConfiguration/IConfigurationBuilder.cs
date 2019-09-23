using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Utilities.ApplicationConfiguration
{
    public interface IConfigurationBuilder
    {
        void Add(IConfigurationSource source);
        IConfigurationProvider Build();
    }
}
