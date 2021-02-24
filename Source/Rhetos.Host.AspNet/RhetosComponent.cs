using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;

namespace Rhetos.Extensions.AspNetCore
{
    internal sealed class RhetosComponent<T> : IRhetosComponent<T>
    {
        public T Value { get; }

        public RhetosComponent(RhetosScopeServiceProvider rhetosScopeServiceProvider)
        {
            Value = rhetosScopeServiceProvider.Resolve<T>();
        }
    }
}
