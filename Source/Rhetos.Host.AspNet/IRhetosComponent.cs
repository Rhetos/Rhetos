using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rhetos.Extensions.AspNetCore
{
    public interface IRhetosComponent<out T>
    {
        T Value { get; }
    }
}
